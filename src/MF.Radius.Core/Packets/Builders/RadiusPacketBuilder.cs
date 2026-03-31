using System.Buffers.Binary;
using MF.Radius.Core.Cryptography;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;

namespace MF.Radius.Core.Packets.Builders;

/// <summary>
/// A high-performance stack-allocated structure for building RADIUS packets.
/// Supports both:
/// - response packets (Access-Accept/Reject/Challenge, Accounting-Response),
/// - server-initiated requests (CoA-Request / Disconnect-Request).
/// Builds RADIUS packets directly into a caller-provided buffer with minimal allocations.
/// </summary>
public ref struct RadiusPacketBuilder
{
    /// <summary>
    /// Destination buffer that stores the full packet (header + attributes).
    /// </summary>
    private readonly Span<byte> _buffer;
    
    /// <summary>
    /// Request Authenticator from the inbound request packet.
    /// </summary>
    /// <remarks>
    /// Used when building response packets (e.g., Access-Accept/Reject, Accounting-Response),
    /// where Response Authenticator depends on the original request authenticator.
    /// </remarks>
    private readonly ReadOnlySpan<byte> _requestAuthenticator;
    
    /// <summary>
    /// Current write offset in <see cref="_buffer"/>.
    /// Starts at RADIUS header size and grows as attributes are appended.
    /// </summary>
    private int _offset;

    /// <summary>
    /// Indicates whether this builder is creating a server-initiated request
    /// (e.g., CoA-Request/Disconnect-Request) rather than a direct response.
    /// </summary>
    private readonly bool _isServerInitiatedRequest;

    /// <summary>
    /// Initializes a builder for server-initiated RADIUS requests (CoA/Disconnect).
    /// </summary>
    /// <param name="destination">Writable destination buffer.</param>
    /// <param name="code">Request code, typically CoA-Request or Disconnect-Request.</param>
    /// <param name="identifier">RADIUS packet identifier.</param>
    /// <exception cref="ArgumentException">Thrown when destination is too small for a RADIUS header.</exception>
    public RadiusPacketBuilder(
        Span<byte> destination,
        RadiusCode code,
        byte identifier
    )
    {
        if (destination.Length < RadiusConstants.HeaderSize)
            throw new ArgumentException("Buffer too small for RADIUS header");

        _buffer = destination;
        _requestAuthenticator = default;
        _isServerInitiatedRequest = true;

        _buffer[0] = (byte)code;
        _buffer[1] = identifier;

        // Length and Authenticator are finalized in Complete().
        _offset = RadiusConstants.HeaderSize;
    }

    /// <summary>
    /// Initializes a builder for response packets that depend on request authenticator.
    /// </summary>
    /// <param name="destination">The destination buffer where the RADIUS packet will be constructed.</param>
    /// <param name="code">The response RADIUS packet code (e.g., Access-Accept).</param>
    /// <param name="identifier">The identifier matching the original request.</param>
    /// <param name="requestAuthenticator">The 16-byte authenticator from the request packet.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when destination is too small or requestAuthenticator length is not 16 bytes.
    /// </exception>
    public RadiusPacketBuilder(
        Span<byte> destination,
        RadiusCode code,
        byte identifier,
        ReadOnlySpan<byte> requestAuthenticator
    )
    {
        if (destination.Length < RadiusConstants.HeaderSize)
            throw new ArgumentException("Buffer too small for RADIUS header");
        if (requestAuthenticator.Length != 16)
            throw new ArgumentException("Request authenticator must be exactly 16 bytes");

        _buffer = destination;
        _requestAuthenticator = requestAuthenticator;
        _isServerInitiatedRequest = false;

        _buffer[0] = (byte)code;
        _buffer[1] = identifier;

        // Length and Authenticator are finalized in Complete().
        _offset = RadiusConstants.HeaderSize;
    }

    /// <summary>
    /// Initializes a <see cref="RadiusPacketBuilder"/> from an existing packet to append attributes in-place.
    /// </summary>
    /// <param name="destination">Writable destination buffer (often the same memory as <c>existingPacket.Raw</c>).</param>
    /// <param name="existingPacket">Existing packet used as the base payload.</param>
    /// <param name="requestAuthenticator">The 16-byte authenticator from the original request.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when destination buffer is smaller than <paramref name="existingPacket"/> length.
    /// </exception>
    public RadiusPacketBuilder(
        Span<byte> destination,
        RadiusPacket existingPacket,
        ReadOnlySpan<byte> requestAuthenticator
    )
    {
        // Ensure the destination buffer is large enough for the existing packet data.
        if (destination.Length < existingPacket.Length)
            throw new ArgumentException("Destination buffer is smaller than the existing packet length.");

        // Copy only when source and destination do not overlap.
        // For in-place patching, copying is intentionally skipped.
        if (!existingPacket.Raw.Span.Overlaps(destination))
            existingPacket.Raw.Span.CopyTo(destination);

        _buffer = destination;
        _requestAuthenticator = requestAuthenticator;

        // Start appending attributes after existing packet payload.
        _offset = existingPacket.Length;

    }

    /// <summary>
    /// Creates a builder for standard RADIUS attributes.
    /// </summary>
    /// <returns>A <see cref="RadiusAttributeBuilder"/> writing into the remaining packet space.</returns>
    public RadiusAttributeBuilder GetAttributeBuilder() => new(_buffer[_offset..]);

    /// <summary>
    /// Creates a builder for Microsoft Vendor-Specific Attributes (Vendor-ID 311).
    /// </summary>
    /// <returns>A <see cref="RadiusMicrosoftAttributeBuilder"/> writing into the remaining packet space.</returns>
    public RadiusMicrosoftAttributeBuilder GetMicrosoftAttributeBuilder() => new(_buffer[_offset..]);

    /// <summary>
    /// Creates a builder for Cisco Vendor-Specific Attributes.
    /// </summary>
    /// <returns>A <see cref="RadiusCiscoAttributeBuilder"/> writing into the remaining packet space.</returns>
    public RadiusCiscoAttributeBuilder GetCiscoAttributeBuilder() => new(_buffer[_offset..]);

    /// <summary>
    /// Commits bytes written by a standard attribute builder and advances packet offset.
    /// </summary>
    /// <param name="builder">Completed attribute builder instance.</param>
    public void Apply(RadiusAttributeBuilder builder) => _offset += builder.Complete();

    /// <summary>
    /// Commits bytes written by a Microsoft VSA builder and advances packet offset.
    /// </summary>
    /// <param name="builder">Completed Microsoft VSA builder instance.</param>
    public void Apply(RadiusMicrosoftAttributeBuilder builder) => _offset += builder.Complete();

    /// <summary>
    /// Commits bytes written by a Cisco VSA builder and advances packet offset.
    /// </summary>
    /// <param name="builder">Completed Cisco VSA builder instance.</param>
    public void Apply(RadiusCiscoAttributeBuilder builder) => _offset += builder.Complete();

    /// <summary>
    /// Finalizes the packet: writes total length and computes authenticator bytes.
    /// </summary>
    /// <param name="sharedSecret">Shared secret used for packet signing.</param>
    /// <returns>A span containing the complete serialized packet.</returns>
    /// <remarks>
    /// Current implementation computes a Response Authenticator using
    /// <see cref="RadiusCrypto.GenerateResponseAuthenticator"/>.
    /// </remarks>
    public ReadOnlySpan<byte> Complete(string sharedSecret)
    {
        var finalLength = (ushort)_offset;
        BinaryPrimitives.WriteUInt16BigEndian(_buffer[2..4], finalLength);

        var authenticatorDestination = _buffer.Slice(4, 16);

        if (_isServerInitiatedRequest)
        {
            RadiusCrypto.GenerateRequestAuthenticator(
                _buffer[0],
                _buffer[1],
                finalLength,
                _buffer[RadiusConstants.HeaderSize.._offset],
                sharedSecret,
                authenticatorDestination
            );
        }
        else
        {
            RadiusCrypto.GenerateResponseAuthenticator(
                _buffer[0],
                _buffer[1],
                finalLength,
                _requestAuthenticator,
                _buffer[RadiusConstants.HeaderSize.._offset],
                sharedSecret,
                authenticatorDestination
            );
        }

        return _buffer[..finalLength];
    }
}
