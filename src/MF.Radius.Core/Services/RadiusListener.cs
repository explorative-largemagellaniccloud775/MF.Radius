using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Extensions;
using MF.Radius.Core.Interfaces;
using MF.Radius.Core.Models;
using MF.Radius.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MF.Radius.Core.Services;

/// <summary>
/// A hosted background service responsible for listening to incoming RADIUS requests
/// on configured ports and dispatching them to worker tasks for processing.
/// Manages inbound packet buffering, concurrency control, and task execution lifecycle.
/// </summary>
public sealed partial class RadiusListener
    : BackgroundService
{
    private readonly ILogger<RadiusListener> _logger;
    private readonly RadiusListenerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    
    /// Tracks packets currently being processed to avoid duplicate in-flight work.
    private readonly ConcurrentDictionary<ProcessingPacketKey, bool> _processingPackets = new();

    /// <summary>
    /// Represents an inbound packet received by the RADIUS listener.
    /// Encapsulates the raw data buffer, the length of the received data, and the remote endpoint information.
    /// </summary>
    private record struct InboundPacket(
        byte[] Buffer, 
        int Length, 
        EndPoint RemoteEndPoint,
        Socket OriginSocket
    );

    /// <summary>
    /// Represents a value-type key used to detect duplicate RADIUS packets that are currently in-flight.
    /// </summary>
    /// <param name="Address">The source IP address of the NAS.</param>
    /// <param name="Port">The source UDP port of the NAS.</param>
    /// <param name="Identifier">The RADIUS packet Identifier (1 byte).</param>
    /// <param name="AuthenticatorPrefix">The first 8 bytes of the 16-byte Request Authenticator.</param>
    /// <param name="AuthenticatorSuffix">The last 8 bytes of the 16-byte Request Authenticator.</param>
    /// <remarks>
    /// This key intentionally combines endpoint + Identifier + full Authenticator to reduce false duplicate matches.
    /// Using only Identifier is not reliable because it is 8-bit and can be reused frequently.
    /// </remarks>
    private readonly record struct ProcessingPacketKey(
        IPAddress Address,
        int Port,
        byte Identifier,
        ulong AuthenticatorPrefix,
        ulong AuthenticatorSuffix
    );

    private readonly Channel<InboundPacket> _inboundChannel;

    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Starting on {IpAddress}:{Port}...")]
    private partial void LogListenerStarting(IPAddress ipAddress, int port);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Debug, Message = "Received packet on port {Port} from {EndPoint}")]
    private partial void LogPacketReceived(int port, EndPoint endPoint);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Warning, Message = "Inbound queue full. Dropped packet on port {Port} from {EndPoint}")]
    private partial void LogInboundQueueFull(int port, EndPoint endPoint);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Error, Message = "Socket error on port {Port}")]
    private partial void LogSocketError(Exception ex, int port);

    [LoggerMessage(EventId = 1104, Level = LogLevel.Information, Message = "Worker {WorkerId} started")]
    private partial void LogWorkerStarted(int workerId);

    [LoggerMessage(EventId = 1105, Level = LogLevel.Warning, Message = "Dropping malformed packet (length {Length}) from {EndPoint}")]
    private partial void LogMalformedPacket(int length, EndPoint endPoint);

    [LoggerMessage(EventId = 1106, Level = LogLevel.Warning, Message = "Dropping duplicate packet (ID: {Id}) from {EndPoint}")]
    private partial void LogDuplicatePacket(byte id, EndPoint endPoint);

    [LoggerMessage(EventId = 1107, Level = LogLevel.Information, Message = "[port {Port}]: {InCode} -> {RespCode} for {EndPoint} (ID: {Id})")]
    private partial void LogResponseFlow(int port, RadiusCode inCode, RadiusCode respCode, EndPoint endPoint, byte id);

    [LoggerMessage(EventId = 1108, Level = LogLevel.Error, Message = "Error processing packet on port {Port} from {EndPoint}")]
    private partial void LogProcessPacketError(Exception ex, int port, EndPoint endPoint);

    /// <summary>
    /// A hosted background service responsible for listening to incoming RADIUS requests
    /// on configured ports and dispatching them to worker tasks for processing.
    /// Manages inbound packet buffering, concurrency control, and task execution lifecycle.
    /// </summary>
    /// <param name="logger">Logger instance for capturing log messages and diagnostics.</param>
    /// <param name="options">Configuration options for the RADIUS listener, including ports,
    /// bind address, and queue size.</param>
    /// <param name="scopeFactory">A factory for creating service scopes used in processing inbound packets.</param>
    public RadiusListener(
        ILogger<RadiusListener> logger,
        IOptions<RadiusListenerOptions> options,
        IServiceScopeFactory scopeFactory
    )
    {
        _logger = logger;
        _options = options.Value;
        _scopeFactory = scopeFactory;

        _inboundChannel = Channel.CreateBounded<InboundPacket>(
            new BoundedChannelOptions(_options.InboundQueueSize)
            {
                // DropWrite keeps ownership explicit: we can safely return the just-rented buffer in finally.
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = false,
                SingleWriter = true
            }
        );
        
    }

    /// <summary>
    /// Executes the main logic of the RADIUS listener service, including initializing worker tasks,
    /// binding sockets to configured ports, and processing incoming RADIUS requests. Manages the
    /// lifecycle of worker and receive tasks, ensuring proper coordination and termination.
    /// </summary>
    /// <param name="stoppingToken">A token to signal the service to stop execution gracefully.</param>
    /// <returns>A task that represents the asynchronous execution of the listener service.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start workers (shared across all ports, reading from the same queue)
        var workers = Enumerable.Range(0, _options.ConcurrentWorkers)
            .Select(id => Task.Run(() => WorkerAsync(id, stoppingToken), stoppingToken))
            .ToArray();
        
        // Start listening on configured ports
        var receiveTasks = _options.Ports.Select(port => Task.Run(async () => 
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var ipEp = new IPEndPoint(IPAddress.Parse(_options.BindAddress), port);
            socket.Bind(ipEp);
            LogListenerStarting(ipEp.Address, ipEp.Port);
            await ReceiveLoopAsync(socket, stoppingToken);
        }, stoppingToken)).ToList();
        
        await Task.WhenAll(receiveTasks);
        _inboundChannel.Writer.Complete();
        await Task.WhenAll(workers);
    }
    
    private async Task ReceiveLoopAsync(Socket socket, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);
            var handedOffToWorker = false;

            try
            {
                var result = await socket.ReceiveFromAsync(
                    rentedBuffer,
                    SocketFlags.None,
                    new IPEndPoint(IPAddress.Any, 0), // placeholder for data 
                    ct
                );
                var packet = new InboundPacket(rentedBuffer, result.ReceivedBytes, result.RemoteEndPoint, socket);

                // Deliver packet to workers
                handedOffToWorker = _inboundChannel.Writer.TryWrite(packet);
                if (handedOffToWorker)
                {
                    LogPacketReceived(
                        ActualPort(socket),
                        result.RemoteEndPoint
                    );
                }
                else
                {
                    LogInboundQueueFull(
                        ActualPort(socket),
                        result.RemoteEndPoint
                    );
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogSocketError(ex, ActualPort(socket));
                await Task.Delay(100, ct);
            }
            finally
            {
                if (!handedOffToWorker) ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    /// <summary>
    /// Asynchronously processes packets from the inbound channel using a dedicated worker.
    /// Each worker handles packet processing and ensures proper resource cleanup.
    /// </summary>
    /// <param name="workerId">The identifier of the worker responsible for processing packets.</param>
    /// <param name="stoppingToken">A cancellation token to signal cancellation requests to the worker operation.</param>
    /// <returns>A task that represents the asynchronous processing of packets by the worker.</returns>
    private async Task WorkerAsync(int workerId, CancellationToken stoppingToken)
    {
        LogWorkerStarted(workerId);
        
        // WARNING! InboundPacket.Buffer should be returned to the pool after processing!!!
        await foreach (var packet in _inboundChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessPacketAsync(packet.OriginSocket, packet.Buffer.AsMemory(0, packet.Length), packet.RemoteEndPoint, stoppingToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packet.Buffer);
            }
        }
        
    }

    /// <summary>
    /// Processes a single inbound RADIUS packet from the worker queue.
    /// </summary>
    /// <param name="socket">The UDP socket used to send the response.</param>
    /// <param name="data">The raw packet bytes for the current request.</param>
    /// <param name="remoteEp">The remote NAS endpoint that sent the packet.</param>
    /// <param name="ct">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous processing pipeline for one packet.</returns>
    /// <remarks>
    /// Pipeline:
    /// 1) Validate minimum packet length;
    /// 2) Parse the packet;
    /// 3) Apply in-flight deduplication;
    /// 4) Resolve processor and execute business logic;
    /// 5) Serialize and send response.
    /// 
    /// Any malformed packet is dropped without terminating the worker loop.
    /// The deduplication key is always removed in <c>finally</c> if it was added.
    /// </remarks>
    private async Task ProcessPacketAsync(Socket socket, ReadOnlyMemory<byte> data, EndPoint remoteEp, CancellationToken ct)
    {
        var hasPacketKey = false;
        var packetKey = default(ProcessingPacketKey);

        try
        {
            // RADIUS packets must be between 20 and 4096 octets.
            // Packets shorter than 20 octets are invalid as they cannot contain a full header.
            if (data.Length < 20)
            {
                LogMalformedPacket(data.Length, remoteEp);
                return;
            }

            var requestPacket = new RadiusPacket(data);

            // Log incoming request packet before processing
            requestPacket.LogPacketAttributes(_logger, remoteEp);

            // Prevent duplicate in-flight processing of the same packet.
            if (TryCreateProcessingPacketKey(remoteEp, requestPacket, out packetKey))
            {
                hasPacketKey = true;
                if (!_processingPackets.TryAdd(packetKey, true))
                {
                    LogDuplicatePacket(requestPacket.Identifier, remoteEp);
                    return;
                }
            }

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IRadiusProcessor>();

            // Buisines logic:
            using var responseDataOwner = await processor.ProcessAsync(requestPacket, remoteEp, ct);
            if (responseDataOwner == null) return;
            var responsePacket = new RadiusPacket(responseDataOwner.Memory);
            var validResponseData = responseDataOwner.Memory[..responsePacket.Length];

            LogResponseFlow(
                ActualPort(socket),
                requestPacket.Code,
                responsePacket.Code,
                remoteEp,
                requestPacket.Identifier
            );

            // Log outgoing response packet
            responsePacket.LogPacketAttributes(_logger, remoteEp);

            await socket.SendToAsync(
                validResponseData,
                SocketFlags.None,
                remoteEp,
                ct
            );
        }
        catch (ArgumentException)
        {
            // Invalid packet structure (e.g. malformed length/header) should be dropped, not crash a worker.
            LogMalformedPacket(data.Length, remoteEp);
        }
        catch (Exception ex)
        {
            LogProcessPacketError(ex, ActualPort(socket), remoteEp);
        }
        finally
        {
            if (hasPacketKey)
            {
                _processingPackets.TryRemove(packetKey, out _);
            }
        }
    }

    /// <summary>
    /// Tries to build a stable in-flight deduplication key for an incoming RADIUS packet.
    /// </summary>
    /// <param name="remoteEp">The remote endpoint that sent the packet.</param>
    /// <param name="packet">The parsed RADIUS packet.</param>
    /// <param name="key">
    /// The resulting deduplication key composed of:
    /// remote IP, remote port, packet Identifier, and the full 16-byte Authenticator (split into two UInt64 values).
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the endpoint is an <see cref="IPEndPoint"/> and the key was created;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Including the Request Authenticator significantly reduces false duplicate matches compared to using
    /// only (endpoint, identifier), because the Identifier is only 8 bits and can be reused by NAS devices.
    /// The method intentionally skips non-IP endpoints to avoid ambiguous key material.
    /// </remarks>
    private static bool TryCreateProcessingPacketKey(EndPoint remoteEp, RadiusPacket packet, out ProcessingPacketKey key)
    {
        if (remoteEp is IPEndPoint ipEndPoint)
        {
            var auth = packet.Authenticator.Span;
            key = new ProcessingPacketKey(
                ipEndPoint.Address,
                ipEndPoint.Port,
                packet.Identifier,
                BinaryPrimitives.ReadUInt64BigEndian(auth[..8]),
                BinaryPrimitives.ReadUInt64BigEndian(auth[8..16])
            );
            return true;
        }

        key = default;
        return false;
    }

    private static int ActualPort(Socket socket)
    {
        return (socket.LocalEndPoint as IPEndPoint)?.Port ?? 0;
    }
    
}
