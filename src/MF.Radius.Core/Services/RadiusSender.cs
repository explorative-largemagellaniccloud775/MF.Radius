using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using MF.Radius.Core.Cryptography;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Extensions;
using MF.Radius.Core.Interfaces;
using MF.Radius.Core.Models;
using MF.Radius.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MF.Radius.Core.Services;

/// <summary>
/// A service for sending RADIUS requests (CoA/DM).
/// Should be used (and registered in DI) as a Singleton.
/// </summary>
public sealed partial class RadiusSender
    : IDisposable
    , IRadiusSender
{
    private readonly ILogger<RadiusSender> _logger;
    private readonly RadiusSenderOptions _options;
    private readonly Socket _socket;
    private readonly CancellationTokenSource _mainCts = new();
    private readonly IPEndPoint _anyEp = new(IPAddress.Any, 0);

    /// <summary>
    /// Represents a unique key used to track pending RADIUS requests.
    /// The key is composed of an IP address, a port, and an identifier.
    /// </summary>
    private readonly record struct PendingKey(IPAddress Address, int Port, byte Identifier)
    {
        public static PendingKey From(IPEndPoint ep, byte identifier) => new(ep.Address, ep.Port, identifier);
    }
    
    private readonly record struct PendingRequest(
        TaskCompletionSource<RadiusPacket> Completion,
        ReadOnlyMemory<byte> RequestAuthenticator,
        string SharedSecret
    );
        
    private readonly ConcurrentDictionary<PendingKey, PendingRequest> _pendingRequests = new ();

    /// <summary>
    /// A service responsible for sending and receiving RADIUS requests, such as CoA (Change of Authorization)
    /// and DM (Disconnect Message). This class interacts with RADIUS clients via UDP.
    /// </summary>
    /// <remarks>
    /// This service should be registered as a singleton within the dependency injection (DI) container.
    /// It initializes a socket for sending outgoing UDP requests, binds to a specified IP address and port,
    /// and manages a background listener loop for receiving RADIUS responses.
    /// </remarks>
    /// <example>
    /// Configuration for this sender is controlled through <see cref="RadiusSenderOptions"/>.
    /// </example>
    public RadiusSender(
        ILogger<RadiusSender> logger,
        IOptions<RadiusSenderOptions> options
    )
    {
        _logger = logger;
        _options = options.Value;

        // Инициализируем сокет для исходящих UDP-запросов.
        // Биндимся на указанный адрес, ОС выделит случайный исходящий порт.
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var ipEp = new IPEndPoint(IPAddress.Parse(_options.BindAddress), _options.Port);
        _socket.Bind(ipEp);

        LogInitialized(ipEp.Address, ipEp.Port);

        // Запускаем фоновый поток прослушивания
        _ = Task.Run(ListenLoop);
    }
    
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Initialized on {IpAddress}:{Port}")]
    private partial void LogInitialized(IPAddress ipAddress, int port);
    
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Waiting for RADIUS packets...")]
    private partial void LogWaitingPackets();
    
    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Received packet from unsupported endpoint type {Type}")]
    private partial void LogUnsupportedEndpointType(string type);
    
    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Received RADIUS packet from {EndPoint}: Code={Code}, ID={Id}, Length={Len}")]
    private partial void LogPacketReceived(EndPoint endPoint, RadiusCode code, byte id, int len);
    
    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Invalid Response Authenticator for ID {Id} from {EndPoint}")]
    private partial void LogInvalidResponseAuthenticator(byte id, EndPoint endPoint);
    
    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning, Message = "Received packet ID {Id} from {EndPoint}, but no matching request is waiting.")]
    private partial void LogNoPendingRequest(byte id, EndPoint endPoint);
    
    [LoggerMessage(EventId = 1006, Level = LogLevel.Error, Message = "Error in RADIUS ListenLoop")]
    private partial void LogListenLoopError(Exception ex);
    
    [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Sending request {Id} to {EndPoint}")]
    private partial void LogSendingRequest(byte id, EndPoint endPoint);
    
    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Success response for ID {Id} from {Ep}")]
    private partial void LogSuccessResponse(byte id, EndPoint ep);
    
    [LoggerMessage(EventId = 1009, Level = LogLevel.Warning, Message = "NAS rejected request ID {Id} from {Ep}. " +
                                                                       "Check request attributes. " +
                                                                       "Error-Cause: {ErrorCause}. " +
                                                                       "Reason: {Reason}")]
    private partial void LogNasRejected(byte id, EndPoint ep, RadiusErrorCause? errorCause, string? reason);
    
    [LoggerMessage(EventId = 1010, Level = LogLevel.Warning, Message = "Unexpected RADIUS code {Code} received for ID {Id}")]
    private partial void LogUnexpectedCode(RadiusCode code, byte id);
    
    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning, Message = "Request {Id} timed out or was cancelled.")]
    private partial void LogRequestCanceled(byte id);
    
    [LoggerMessage(EventId = 1012, Level = LogLevel.Error, Message = "Failed to send request {Id}")]
    private partial void LogSendFailed(Exception ex, byte id);
    

    private async Task ListenLoop()
    {
        LogWaitingPackets();
        while (!_mainCts.IsCancellationRequested)
        {
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);
            try
            {
                var result = await _socket.ReceiveFromAsync(rentedBuffer, SocketFlags.None, _anyEp);
                if (result.RemoteEndPoint is not IPEndPoint responseEp)
                {
                    LogUnsupportedEndpointType(result.RemoteEndPoint.GetType().FullName ?? "<null>");
                    continue;
                }

                // TODO(perf): Avoid per-packet heap allocation here.
                // We currently copy to a stable array because RadiusPacket is returned to callers
                // and must not reference an ArrayPool-backed buffer that will be returned in finally.
                // Revisit after introducing an owned-response contract (IMemoryOwner<byte> + length).
                var dataCopy = rentedBuffer.AsSpan(0, result.ReceivedBytes).ToArray();
                
                var packet = new RadiusPacket(dataCopy);
                LogPacketReceived(responseEp, packet.Code, packet.Identifier, result.ReceivedBytes);

                // Log incoming response packet
                packet.LogPacketAttributes(_logger, result.RemoteEndPoint);

                // Ищем того, кто ждет этот Identifier
                var pendingKey = PendingKey.From(responseEp, packet.Identifier);
                if (_pendingRequests.TryRemove(pendingKey, out var pending))
                {
                    if (!IsValidResponseAuthenticator(packet, pending.RequestAuthenticator, pending.SharedSecret))
                    {
                        LogInvalidResponseAuthenticator(packet.Identifier, responseEp);
                        pending.Completion.TrySetException(new CryptographicException("Invalid RADIUS Response Authenticator."));
                        continue;
                    }

                    // Получили ответ
                    pending.Completion.TrySetResult(packet);
                    
                }
                else
                {
                    LogNoPendingRequest(packet.Identifier, responseEp);
                }
            }
            catch (Exception ex)
            {
                LogListenLoopError(ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    /// <summary>
    /// Sends a RADIUS request and waits for a response from the specified remote endpoint.
    /// </summary>
    /// <param name="requestDataOwner">
    /// An <see cref="IMemoryOwner{T}"/> instance that owns the memory containing the request data.
    /// </param>
    /// <param name="remoteEp">
    /// The <see cref="IPEndPoint"/> representing the remote endpoint to which the request will be sent.
    /// </param>
    /// <param name="sharedSecret">
    /// A string containing the shared secret used for RADIUS message signing and verification.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="RadiusPacket"/>
    /// instance representing the response, or <c>null</c> if no response was received.
    /// </returns>
    public async Task<RadiusPacket?> SendAndReceiveAsync(
        IMemoryOwner<byte> requestDataOwner,
        IPEndPoint remoteEp,
        string sharedSecret, 
        CancellationToken ct
    )
    {
        var requestPacket = new RadiusPacket(requestDataOwner.Memory);
        LogSendingRequest(requestPacket.Identifier, remoteEp);

        // Log outgoing response
        requestPacket.LogPacketAttributes(_logger, remoteEp);
        
        using (requestDataOwner)
        {
            // Создаем TCS "обещания" ответа с флагом асинхронного продолжения.
            // RunContinuationsAsynchronously - не выполнять продолжения (бизнес-логику) в текущем потоке.
            // TODO: Heap allocation
            var tcs = new TaskCompletionSource<RadiusPacket>(TaskCreationOptions.RunContinuationsAsynchronously);

            var pendingKey = PendingKey.From(remoteEp, requestPacket.Identifier);
            var pendingRequest = new PendingRequest(
                tcs,
                requestPacket.Authenticator, // 16 bytes
                sharedSecret
            );

            if (!_pendingRequests.TryAdd(pendingKey, pendingRequest))
                throw new InvalidOperationException($"MF-RADIUS Sender: Request with ID {requestPacket.Identifier} for {remoteEp} is already in progress.");

            try
            {
                await _socket.SendToAsync(requestDataOwner.Memory[..requestPacket.Length], SocketFlags.None, remoteEp, ct);

                // Связываем наш таймаут и внешний токен отмены.
                // Ждем либо ответа от ListenLoop, либо таймаута.
                using var timeoutCt = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCt.CancelAfter(_options.DefaultTimeout);

                // Подписываемся на отмену, чтобы завершить TaskCompletionSource
                await using (timeoutCt.Token.Register(() => tcs.TrySetCanceled()))
                {
                    var responsePacket = await tcs.Task;
                    
                    if (responsePacket.IsSuccessResponse)
                        LogSuccessResponse(responsePacket.Identifier, remoteEp);
                    
                    else if (responsePacket.IsErrorResponse)
                    {
                        var reason = responsePacket.GetNasErrorDescription(out var errorCause);
                        LogNasRejected(responsePacket.Identifier, remoteEp, errorCause, reason);
                    }
                    
                    else
                        LogUnexpectedCode(responsePacket.Code, responsePacket.Identifier);
                    
                    return responsePacket;
                }
            }
            catch (OperationCanceledException)
            {
                LogRequestCanceled(requestPacket.Identifier);
                return null;
            }
            catch (Exception ex)
            {
                LogSendFailed(ex, requestPacket.Identifier);
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(pendingKey, out var _);
            }
        }
    }

    /// <summary>
    /// Validates the Response Authenticator of a RADIUS packet by recalculating its value and comparing it
    /// against the provided RADIUS response packet's authenticator field.
    /// </summary>
    /// <param name="responsePacket">The received RADIUS response packet to validate.</param>
    /// <param name="requestAuthenticator">The authenticator from the corresponding RADIUS request.</param>
    /// <param name="sharedSecret">The shared secret used for communication with the RADIUS server.</param>
    /// <returns>True if the response authenticator is valid; otherwise, false.</returns>
    private static bool IsValidResponseAuthenticator(
        RadiusPacket responsePacket,
        ReadOnlyMemory<byte> requestAuthenticator,
        string sharedSecret
    )
    {
        Span<byte> computed = stackalloc byte[16];
        RadiusCrypto.GenerateResponseAuthenticator(
            (byte)responsePacket.Code,
            responsePacket.Identifier,
            responsePacket.Length,
            requestAuthenticator.Span,
            responsePacket.Attributes.Span,
            sharedSecret,
            computed
        );
        return CryptographicOperations.FixedTimeEquals(responsePacket.Authenticator.Span, computed);
    }
    

    public void Dispose()
    {
        // Сигнализируем об остановке
        _mainCts.Cancel();
        
        // Закрываем сокет (вызовет исключение в ListenLoop и прервет ожидания)
        _socket.Dispose();
        
        // Очищаем все ожидающие запросы, чтобы вызывающие потоки не "зависли"
        foreach (var pending in _pendingRequests.Values)
            pending.Completion.TrySetCanceled();
        
        _pendingRequests.Clear();

        // Утилизируем токен
        _mainCts.Dispose();
        
    }
    
}
