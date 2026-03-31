using System.Buffers.Binary;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Packets.Parsing;

namespace MF.Radius.Core.Models;

/// <summary>
/// A READ-ONLY structure for parsing a RADIUS packet over Memory{byte}.
/// Provides safe access to header fields and attributes without allocations.
/// </summary>
public readonly struct RadiusPacket
{
    private readonly ReadOnlyMemory<byte> _buffer;
    private const int HeaderSize = 20;

    /// <summary>
    /// Initializes a RadiusPacket for reading from an existing buffer.
    /// </summary>
    /// <param name="data">The raw packet data.</param>
    public RadiusPacket(ReadOnlyMemory<byte> data)
    {
        if (data.Length < HeaderSize) throw new ArgumentException("Packet too short");
        
        _buffer = data; 
        
        // Ensure the declared length in the header matches available data
        if (Length > data.Length) throw new ArgumentException("Packet length mismatch (header vs buffer)");
        
    }

    /// <summary>
    /// Provides access to the raw byte representation of the RADIUS packet buffer.
    /// </summary>
    public ReadOnlyMemory<byte> Raw => _buffer;

    /// <summary>
    /// Represents the RADIUS code indicating the type of the RADIUS packet.
    /// </summary>
    public RadiusCode Code => (RadiusCode)_buffer.Span[0];

    /// <summary>
    /// It is used to match request and reply packets
    /// </summary>
    public byte Identifier => _buffer.Span[1];

    /// <summary>
    /// It indicates the total length of all fields,
    /// namely, the Code, Identifier, Length, Authenticator, and Attributes fields
    /// </summary>
    public ushort Length => BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span[2..4]);

    /// <summary>
    /// The 16-byte Request/Response Authenticator. 
    /// </summary>
    public ReadOnlyMemory<byte> Authenticator => _buffer[4..20];
    
    /// <summary>
    /// The attributes section of the packet.
    /// </summary>
    public ReadOnlyMemory<byte> Attributes => _buffer[HeaderSize..Length];

    /// <summary>
    /// Iterates over attributes in the incoming packet.
    /// </summary>
    public RadiusAttributeEnumerator GetAttributes() => new(Attributes);
    
    /// <summary>
    /// Gets a value indicating whether the packet is a CoA/Disconnect ACK.
    /// </summary>
    public bool IsSuccessResponse => Code
        is RadiusCode.AccessAccept 
        or RadiusCode.AccountingResponse 
        or RadiusCode.CoAAck
        or RadiusCode.DisconnectAck
    ;
    
    /// <summary>
    /// Gets a value indicating whether the packet is a CoA/Disconnect NAK or Access-Reject.
    /// </summary>
    public bool IsErrorResponse => Code
        is RadiusCode.AccessReject 
        or RadiusCode.CoANak 
        or RadiusCode.DisconnectNak
    ;
    
}
