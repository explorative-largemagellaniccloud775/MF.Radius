using System.Buffers;
using System.Net;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Interfaces;
using MF.Radius.Core.Models;
using MF.Radius.Core.Models.Acct;
using MF.Radius.Core.Models.Auth;
using MF.Radius.Core.Processors;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Sessions.Events;
using MF.Radius.SampleServer.Application.Features.Sessions.Interfaces;
using MF.Radius.SampleServer.Application.Features.Subscribers.Events;
using MF.Radius.SampleServer.Application.Features.Subscribers.Interfaces;
using MF.Radius.SampleServer.Application.Interfaces.Packets;
using MF.Radius.SampleServer.Application.Models;
using MF.Radius.SampleServer.Application.Options;
using MF.Radius.SampleServer.Domain.Entities;
using Microsoft.Extensions.Options;

namespace MF.Radius.SampleServer.Infrastructure.Radius;

/// <summary>
/// A high-performance ISP RADIUS processor implementing core business logic.
/// Handles authentication and tracks user sessions via accounting.
/// Orchestrator.
/// </summary>
public sealed partial class IspRadiusProcessor(
    ILogger<IspRadiusProcessor> logger,
    IOptions<RadiusIspOptions> options,
    IRadiusSharedSecretResolver secretResolver,
    ISubscriberRepository subscribersRepo,
    ISessionStore sessionsStore,
    IAuthResponsePacketFactory authResponsePacketFactory,
    IAcctResponsePacketFactory acctResponsePacketFactory,
    IApplicationEventPublisher eventPublisher
)
    : RadiusProcessorBase(logger)
{
    private readonly RadiusIspOptions _options = options.Value;
    private const string LogPrefix = "[Id: {id}] ";
    
    [LoggerMessage(LogLevel.Information, Message = LogPrefix + "Auth Request: processing for username '{userName}' (via {authType})")]
    private partial void LogProcessingAuthRequest(byte id, string userName, string authType);
    
    [LoggerMessage(LogLevel.Warning, Message = LogPrefix + "Auth Reject: username '{userName}' not found or disabled")]
    private partial void LogSubscriberError(byte id, string userName);
    
    [LoggerMessage(LogLevel.Warning, Message = LogPrefix + "Auth Reject: invalid credentials for username '{userName}'")]
    private partial void LogInvalidCredentials(byte id, string userName);
    
    [LoggerMessage(LogLevel.Information, Message = LogPrefix + "Auth Success: username {userName} (via {authType})")]
    private partial void LogAuthSuccess(byte id, string userName, string authType);
    
    [LoggerMessage(LogLevel.Debug, Message = LogPrefix + "Acct Request: processing for username '{userName}' (Session-Id: '{sessionId}')")]
    private partial void LogProcessingAcctRequest(byte id, string userName, string sessionId);
    
    [LoggerMessage(LogLevel.Warning, Message = LogPrefix + "Acct Warning: Session-Id is missing/empty for username '{userName}' (status {statusType}); session store update skipped, Accounting-Response will still be sent")]
    private partial void LogMissingSessionId(byte id, string userName, string statusType);
    
    [LoggerMessage(LogLevel.Information, Message = LogPrefix + "Acct Request: statusType {statusType}, username {userName}, sessionId {sessionId}")]
    private partial void LogAcctRequest(byte id, string statusType, string userName, string sessionId);
    
    [LoggerMessage(LogLevel.Information, Message = LogPrefix + "Acct Handled: username {userName} (Session-Id: '{sessionId}')")]
    private partial void LogAcctHandled(byte id, string userName, string sessionId);
    
    /// <summary>
    /// Resolves the shared secret for the NAS using the infrastructure resolver.
    /// </summary>
    protected override async Task<string> GetSharedSecretAsync(EndPoint remoteEndPoint)
    {
        return await secretResolver.GetSharedSecretAsync(remoteEndPoint);
    }

    /// <summary>
    /// Handles Authentication requests (PAP, CHAP, MS-CHAPv2).
    /// </summary>
    protected override async Task<IMemoryOwner<byte>> HandleAuthRequestAsync(
        RadiusAuthRequestBase authRequest,
        string sharedSecret,
        CancellationToken ct
    )
    {
        LogProcessingAuthRequest(authRequest.RawPacket.Identifier, authRequest.UserName, authRequest.AuthProtocol.ToString());
        
        // 1. Get subscriber and validate credentials (this helper handles PAP/CHAP/MS-CHAPv2 automatically)
        var subscriber = await subscribersRepo.GetByUserNameAsync(authRequest.UserName);
        if (subscriber == null || subscriber.Status == SubscriberStatus.Disabled)
        {
            LogSubscriberError(authRequest.RawPacket.Identifier, authRequest.UserName);
            PublishInBackground(new SubscriberAuthRejectedEvent
            {
                UserName = authRequest.UserName,
                AuthProtocol = authRequest.AuthProtocol,
                Reason = "Subscriber not found or disabled"
            });
            return await BuildReject(authRequest.RawPacket, sharedSecret, ct: ct);
        }
        
        if (!ValidateCredentials(authRequest, subscriber.StoredPassword))
        {
            LogInvalidCredentials(authRequest.RawPacket.Identifier, authRequest.UserName);
            PublishInBackground(new SubscriberAuthRejectedEvent
            {
                UserName = authRequest.UserName,
                AuthProtocol = authRequest.AuthProtocol,
                Reason = "Invalid credentials"
            });
            return await BuildReject(authRequest.RawPacket, sharedSecret, ct: ct);
        }

        // 2. Build Access-Accept with authorization attributes
        // Rent a buffer from the shared pool. 
        // NOTE: We return the full IMemoryOwner<byte> to avoid heap allocations via wrappers.
        // The Listener is responsible for determining the actual payload size by reading 
        // the 'Length' field from the RADIUS header and ensuring Dispose() is called after transmission.
        var responseDataOwner = authResponsePacketFactory.BuildAccessAccept(
            authRequest,
            sharedSecret,
            subscriber,
            _options
        );
        
        LogAuthSuccess(authRequest.RawPacket.Identifier, subscriber.UserName, authRequest.AuthProtocol.ToString());
        PublishInBackground(new SubscriberAuthenticatedEvent
        {
            UserName = authRequest.UserName,
            AuthProtocol = authRequest.AuthProtocol,
        });
        
        // Return ownership to the caller (Listener).
        // The Listener will check the packet length and will dispose of it after sending.
        return responseDataOwner;
    }

    /// <summary>
    /// Handles Accounting requests and manages the session store.
    /// </summary>
    protected override async Task<IMemoryOwner<byte>> HandleAcctRequestAsync(
        RadiusAcctRequest acctRequest,
        string sharedSecret,
        CancellationToken ct
    )
    {
        LogProcessingAcctRequest(acctRequest.RawPacket.Identifier, acctRequest.UserName, acctRequest.SessionId);

        if (string.IsNullOrWhiteSpace(acctRequest.SessionId))
            LogMissingSessionId(
                acctRequest.RawPacket.Identifier,
                acctRequest.UserName,
                acctRequest.StatusType.ToString()
            );

        if (!string.IsNullOrWhiteSpace(acctRequest.SessionId))
            switch (acctRequest.StatusType)
            {
                case RadiusAcctStatusType.Start:
                    LogAcctRequest(acctRequest.RawPacket.Identifier, acctRequest.StatusType.ToString(), acctRequest.UserName, acctRequest.SessionId);
                    await sessionsStore.SaveAsync(new Session
                    {
                        SessionId = acctRequest.SessionId,
                        UserName = acctRequest.UserName,
                        NasEndPoint = acctRequest.RemoteEndPoint,
                    });
                    break;

                case RadiusAcctStatusType.Stop:
                    LogAcctRequest(acctRequest.RawPacket.Identifier, acctRequest.StatusType.ToString(), acctRequest.UserName, acctRequest.SessionId);
                    // NOTE: in real production, we should store session duration and billing info here
                    await sessionsStore.RemoveAsync(
                        acctRequest.RemoteEndPoint, 
                        acctRequest.SessionId
                    );
                    break;

                case RadiusAcctStatusType.InterimUpdate:
                    LogAcctRequest(acctRequest.RawPacket.Identifier, acctRequest.StatusType.ToString(), acctRequest.UserName, acctRequest.SessionId);
                    // TODO: Implement interim update logic
                    break;
            }
        
        // Build Accounting-Response with no attributes
        var responseDataOwner = acctResponsePacketFactory.BuildAccountingResponse(
            acctRequest, 
            sharedSecret
        );
        
        LogAcctHandled(acctRequest.RawPacket.Identifier, acctRequest.UserName, acctRequest.SessionId);
        PublishInBackground(new AcctPacketProcessedEvent
        {
            UserName = acctRequest.UserName,
            SessionId = acctRequest.SessionId,
            StatusType = acctRequest.StatusType,
        });
        
        return responseDataOwner;
    }
    
    protected override IMemoryOwner<byte> BuildRejectPacket(RadiusPacket requestPacket, string sharedSecret)
    {
        return authResponsePacketFactory.BuildAccessReject(requestPacket, sharedSecret);
    }
    
    private void PublishInBackground<TEvent>(TEvent evt)
        where TEvent : IApplicationEvent
    {
        // NOTE: This is a simple demo-friendly fire-and-forget approach to keep hot path lean.
        // It may drop ordering/backpressure guarantees under load.
        // For production, prefer bounded Channel + BackgroundService (or external broker/outbox).
        // NOTE: Task.Run introduces extra scheduling and allocation overhead,
        // so this is not a zero-allocation hot-path strategy.
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync(evt, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Background event publish failed for {EventType}", typeof(TEvent).Name);
            }
        });
    }
    
}