using System.Buffers;
using System.Net;
using MF.Radius.Core.Cryptography;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Interfaces;
using MF.Radius.Core.Models;
using MF.Radius.Core.Models.Acct;
using MF.Radius.Core.Models.Auth;
using MF.Radius.Core.Packets.Builders;
using Microsoft.Extensions.Logging;

namespace MF.Radius.Core.Processors;

/// <summary>
/// A high-performance base class for RADIUS processors optimized for .NET 10.
/// Manages zero-allocation packet routing and protocol-specific patching for MS-CHAP v2 and MPPE.
/// </summary>
public abstract partial class RadiusProcessorBase(ILogger logger)
    : IRadiusProcessor
{
    protected readonly ILogger Logger = logger;
    private const string LogPrefix = "[Id: {id}] ";
    
    [LoggerMessage(Level = LogLevel.Debug, Message = LogPrefix + "Received RADIUS {code}")]
    protected partial void LogPacketReceived(byte id, RadiusCode code);
    
    [LoggerMessage(Level = LogLevel.Error, Message = LogPrefix + "Shared secret not found for {endPoint}")]
    protected partial void LogSharedSecretNotFound(byte id, EndPoint endPoint);
    
    [LoggerMessage(Level = LogLevel.Error, Message = LogPrefix + "Security violation! Invalid Message-Authenticator from {endPoint}")]
    protected partial void LogInvalidMessageAuthenticator(byte id, EndPoint endPoint);
    
    [LoggerMessage(Level = LogLevel.Information, Message = LogPrefix + "Processed {code} in {elapsed}ms")]
    protected partial void LogProcessed(byte id, RadiusCode code, double elapsed);
    
    [LoggerMessage(Level = LogLevel.Critical, Message = LogPrefix + "Fatal error from {endPoint}")]
    protected partial void LogFatalError(Exception ex, byte id, EndPoint endPoint);
    
    [LoggerMessage(Level = LogLevel.Debug, Message = LogPrefix + "Reject delay set to {delay}")]
    protected partial void LogRejectDelay(byte id, TimeSpan delay);
    
    [LoggerMessage(Level = LogLevel.Error, Message = LogPrefix + "MS-CHAP v2 patching failed: StoredPassword is missing. " +
        "Make sure to set authRequest.StoredPassword during your credential validation.")]
    protected partial void LogMsChap2PathingFailed(byte id);
    
    /// <summary>
    /// Entry point for processing incoming UDP payloads. Handles security validation and routing.
    /// </summary>
    public async ValueTask<IMemoryOwner<byte>?> ProcessAsync(RadiusPacket requestPacket, EndPoint remoteEndPoint, CancellationToken ct)
    {
        var startTime = System.Diagnostics.Stopwatch.GetTimestamp();
        
        try
        {
            // 1. Identify the NAS and get the secret
            var sharedSecret = await GetSharedSecretAsync(remoteEndPoint);
            if (string.IsNullOrEmpty(sharedSecret))
            {
                LogSharedSecretNotFound(requestPacket.Identifier, remoteEndPoint);
                return null;
            }
            
            // 2. Security Check: Validate Message-Authenticator (RFC 2869)
            if (!RadiusCrypto.ValidateMessageAuthenticator(requestPacket.Raw, sharedSecret))
            {
                LogInvalidMessageAuthenticator(requestPacket.Identifier, remoteEndPoint);
                return null;
            }

            LogPacketReceived(requestPacket.Identifier, requestPacket.Code);

            // 3. Route to a specific handler based on Packet Code
            var responseDataOwner = requestPacket.Code switch
            {
                RadiusCode.AccessRequest => await HandleAuthInternalAsync(requestPacket, remoteEndPoint, sharedSecret, ct),
                RadiusCode.AccountingRequest => await HandleAcctInternalAsync(requestPacket, remoteEndPoint, sharedSecret, ct),
                _ => await HandleUnknownPacketAsync(requestPacket, sharedSecret, ct)
            };
            
            var elapsedTime = System.Diagnostics.Stopwatch.GetElapsedTime(startTime);
            LogProcessed(requestPacket.Identifier, requestPacket.Code, elapsedTime.TotalMilliseconds);
            
            return responseDataOwner;
            
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            LogFatalError(ex, requestPacket.Identifier, remoteEndPoint);
            return null;
        }
        
    }

    /// <summary>
    /// Internal logic for Authentication requests. Orchestrates parsing, business logic, and MS-CHAP v2 patching.
    /// </summary>
    private async Task<IMemoryOwner<byte>> HandleAuthInternalAsync(
        RadiusPacket requestPacket, 
        EndPoint remoteEndPoint,
        string sharedSecret, 
        CancellationToken ct
    )
    {
        // Parse raw packet into specialized request types (PAP, CHAP, MS-CHAP v2)
        var authRequest = RadiusAuthRequestProcessor.Process(requestPacket, remoteEndPoint, sharedSecret);
        
        if (string.IsNullOrWhiteSpace(authRequest.UserName))
            return await BuildReject(requestPacket, sharedSecret, ct: ct);
        
        // Call user-defined business logic
        var responseDataOwner = await HandleAuthRequestAsync(authRequest, sharedSecret, ct);
        
        // Post-processing: If it's MS-CHAP v2, and we accepted, we MUST append Success-Report and MPPE keys
        return authRequest switch
        {
            RadiusAuthMsChapV2Request msChap2 => await TryPatchMsChap2Success(responseDataOwner, msChap2, sharedSecret, ct),
            _ => responseDataOwner
        };
        
    }

    private async Task<IMemoryOwner<byte>> HandleAcctInternalAsync(
        RadiusPacket requestPacket,
        EndPoint remoteEndPoint,
        string sharedSecret,
        CancellationToken ct
    )
    {
        // Parse raw packet into request
        var acctRequest = RadiusAcctRequestProcessor.Process(requestPacket, remoteEndPoint);
        
        // Call user-defined business logic
        var responseDataOwner = await HandleAcctRequestAsync(acctRequest, sharedSecret, ct);
        
        return responseDataOwner;

    }
    
    /// <summary>
    /// Ensures that Access-Accept packets for MS-CHAP v2 contain the mandatory Success-Report and MPPE keys.
    /// </summary>
    private async Task<IMemoryOwner<byte>> TryPatchMsChap2Success(
        IMemoryOwner<byte> responseDataOwner, 
        RadiusAuthMsChapV2Request authRequest, 
        string sharedSecret,
        CancellationToken ct
    )
    {
        var responsePacket = new RadiusPacket(responseDataOwner.Memory);
        
        // Patching only applies to successful Access-Accept packets
        if (responsePacket.Code != RadiusCode.AccessAccept) return responseDataOwner;
        
        // We need the cleartext password to derive MPPE keys and the Success-Report
        if (string.IsNullOrEmpty(authRequest.StoredPassword))
        {
            LogNullStoredPassword(authRequest);
            responseDataOwner.Dispose();
            return await BuildReject(authRequest.RawPacket, sharedSecret, ct: ct);
        }

        PatchMsChap2SuccessInternal(responseDataOwner, authRequest, sharedSecret);
        return responseDataOwner;
    }
    
    /// <summary>
    /// Performs in-place modification of the response buffer to add MS-CHAP v2 specific attributes (RFC 2548).
    /// </summary>
    private void PatchMsChap2SuccessInternal(
        IMemoryOwner<byte> responseDataOwner, 
        RadiusAuthMsChapV2Request authRequest, 
        string sharedSecret
    )
    {
        if (authRequest.StoredPassword == null)
        {
            LogNullStoredPassword(authRequest);
            return;
        }
        
        // Use Builder to append attributes to the existing packet
        var responsePacket = new RadiusPacket(responseDataOwner.Memory);
        var builder = new RadiusPacketBuilder(
            responseDataOwner.Memory.Span, 
            responsePacket, 
            authRequest.RawPacket.Authenticator.Span
        );

        // 1. Calculate ChallengeHash for Authenticator Response
        Span<byte> challengeHash = stackalloc byte[8];
        RadiusMsChapV2Crypto.ComputeChallengeHash(
            authRequest.PeerChallenge.Span, 
            authRequest.AuthenticatorChallenge.Span, 
            authRequest.UserName, 
            challengeHash
        );

        // 2. Generate the S= string
        var rentedMsChap2SuccessBuffer = ArrayPool<byte>.Shared.Rent(43);
        try
        {
            var msChapSuccessBytes = RadiusMsChapV2Crypto.GenerateAuthenticatorResponse(
                authRequest.StoredPassword,
                authRequest.Ident,
                authRequest.NtResponse.Span,
                challengeHash,
                rentedMsChap2SuccessBuffer
            );
            var msBuilder = builder.GetMicrosoftAttributeBuilder();
            msBuilder.AddBytes(
                RadiusMsAttributeType.MsChap2Success,
                rentedMsChap2SuccessBuffer.AsSpan(0, msChapSuccessBytes)
            );
            builder.Apply(msBuilder);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedMsChap2SuccessBuffer);
        }

        // 3. Derive encrypted MPPE keys
        var rentedMppeKeySendBuf = ArrayPool<byte>.Shared.Rent(34);
        var rentedMppeKeyRecvBuf = ArrayPool<byte>.Shared.Rent(34);
        try
        {
            RadiusMsChapV2Crypto.GenerateAndEncryptMppeKeys(
                authRequest.StoredPassword,
                authRequest.NtResponse.Span,
                authRequest.RawPacket.Authenticator.Span,
                sharedSecret,
                rentedMppeKeySendBuf,
                rentedMppeKeyRecvBuf
            );

            var msBuilder = builder.GetMicrosoftAttributeBuilder();
            msBuilder.AddBytes(
                RadiusMsAttributeType.MsMppeSendKey,
                rentedMppeKeySendBuf.AsSpan(0, 34)
            );
            builder.Apply(msBuilder);

            msBuilder = builder.GetMicrosoftAttributeBuilder();
            msBuilder.AddBytes(
                RadiusMsAttributeType.MsMppeRecvKey,
                rentedMppeKeyRecvBuf.AsSpan(0, 34)
            );
            builder.Apply(msBuilder);
            
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedMppeKeySendBuf);
            ArrayPool<byte>.Shared.Return(rentedMppeKeyRecvBuf);
        }
        
        
        // 5. Add other Microsoft Vendor-Specific Attributes (VSA)
        var msBuilderFinal = builder.GetMicrosoftAttributeBuilder();
        msBuilderFinal.AddInt32(RadiusMsAttributeType.MsMppeEncryptionPolicy, 1); // Allowed
        // msBuilderFinal.AddInt32(RadiusMsAttributeType.MsMppeEncryptionPolicy, 2); // Required
        builder.Apply(msBuilderFinal);

        msBuilderFinal = builder.GetMicrosoftAttributeBuilder();
        msBuilderFinal.AddInt32(RadiusMsAttributeType.MsMppeEncryptionTypes, 6); // 128-bit
        builder.Apply(msBuilderFinal);

        // 6. Sign and finalize the packet
        builder.Complete(sharedSecret);
        
    }

    /// <summary>
    /// Helper method for developers to validate credentials. 
    /// Automatically detects authentication type (PAP, CHAP, MS-CHAP v2) and uses the correct crypto.
    /// </summary>
    /// <param name="authRequest">The parsed authentication request.</param>
    /// <param name="storedPassword">The cleartext password retrieved from your database.</param>
    /// <returns>A tuple containing the validation result and the password (for MPPE derivation).</returns>
    protected virtual bool ValidateCredentials(
        RadiusAuthRequestBase authRequest, 
        string storedPassword
    )
    { 
        var isValid = false;
        switch (authRequest)
        {
            case RadiusAuthPapRequest pap:
                isValid = pap.Password == storedPassword;
                break;
            
            case RadiusAuthChapRequest chap:
                isValid = RadiusCrypto.ValidateChapResponse(
                    chap.ChapId,
                    storedPassword,
                    chap.Challenge.Span,
                    chap.Response.Span
                );
                break;
            
            case RadiusAuthMsChapV2Request msChap2:
                msChap2.StoredPassword = storedPassword;
                isValid = RadiusMsChapV2Crypto.ValidateMsChapV2Response(
                    msChap2.UserName,
                    msChap2.StoredPassword,
                    msChap2.AuthenticatorChallenge.Span,
                    msChap2.PeerChallenge.Span,
                    msChap2.NtResponse.Span
                );
                break;
            
        }
        return isValid;
    }
    
    // --- Abstract / Virtual API for External Implementation ---

    /// <summary>
    /// Must return the shared secret (password) associated with the NAS (remoteEndPoint).
    /// </summary>
    protected abstract Task<string> GetSharedSecretAsync(EndPoint remoteEndPoint);
    
    /// <summary>
    /// Business logic handler for Access-Request.
    /// Use <see cref="ValidateCredentials"/> inside to check passwords and set authRequest.StoredPassword inside.
    /// </summary>
    protected abstract Task<IMemoryOwner<byte>> HandleAuthRequestAsync(
        RadiusAuthRequestBase authRequest, 
        string sharedSecret, 
        CancellationToken ct
    );

    /// <summary>
    /// Business logic handler for Accounting-Request.
    /// </summary>
    protected abstract Task<IMemoryOwner<byte>> HandleAcctRequestAsync(
        RadiusAcctRequest acctRequest, 
        string sharedSecret, 
        CancellationToken ct
    );

    /// <summary>
    /// Fallback for unsupported RADIUS codes.
    /// </summary>
    protected virtual Task<IMemoryOwner<byte>?> HandleUnknownPacketAsync(RadiusPacket request, string sharedSecret, CancellationToken ct)
    {
        Logger.LogWarning("Unsupported packet code {Code} from {Id}", request.Code, request.Identifier);
        return Task.FromResult<IMemoryOwner<byte>?>(null);
    }

    /// <summary>
    /// Constructs and returns a RADIUS reject packet in response to a request packet.
    /// Optionally introduces a delay before returning the response to throttle invalid requests.
    /// </summary>
    /// <param name="requestPacket">
    /// The incoming RADIUS packet to which the reject response will correspond.
    /// </param>
    /// <param name="sharedSecret">
    /// The shared secret used to sign the reject response for security validation.
    /// </param>
    /// <param name="rejectDelay">
    /// The optional delay before returning the reject response. Defaults to no delay.
    /// </param>
    /// <param name="ct">
    /// The cancellation token that can be used to cancel the reject response process.
    /// </param>
    /// <returns>
    /// A memory owner containing the serialized RADIUS reject packet ready for transmission.
    /// </returns>
    protected async Task<IMemoryOwner<byte>> BuildReject(
        RadiusPacket requestPacket,
        string sharedSecret,
        TimeSpan rejectDelay = default,
        CancellationToken ct = default
    )
    {
        var responseDataOwner = BuildRejectPacket(requestPacket, sharedSecret);

        if (rejectDelay > TimeSpan.Zero)
        {
            LogRejectDelay(requestPacket.Identifier, rejectDelay);
            try
            {
                await Task.Delay(rejectDelay, ct);
            }
            catch (OperationCanceledException)
            {
                responseDataOwner.Dispose();
                throw;
            }
        }

        return responseDataOwner;
    }
    
    /// <summary>
    /// Builds a standard Access-Reject response.
    /// </summary>
    protected virtual IMemoryOwner<byte> BuildRejectPacket(RadiusPacket requestPacket, string sharedSecret)
    {
        var responseDataOwner = MemoryPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);
        var builder = new RadiusPacketBuilder(
            responseDataOwner.Memory.Span,
            RadiusCode.AccessReject,
            requestPacket.Identifier,
            requestPacket.Authenticator.Span
        );
        builder.Complete(sharedSecret);
        return responseDataOwner;
    }

    private void LogNullStoredPassword(RadiusAuthRequestBase authRequest)
    {
        LogMsChap2PathingFailed(authRequest.RawPacket.Identifier);
    }
    
}
