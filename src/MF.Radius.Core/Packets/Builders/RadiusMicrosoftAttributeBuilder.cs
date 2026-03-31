using System.Buffers.Binary;
using System.Text;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;

namespace MF.Radius.Core.Packets.Builders;

/// <summary>
/// A high-performance builder for Microsoft Vendor-Specific Attributes (Vendor-ID 311).
/// </summary>
public ref struct RadiusMicrosoftAttributeBuilder
{
    private readonly Span<byte> _buffer;
    private int _offset;

    /// <summary>
    /// A high-performance builder for constructing Microsoft Vendor-Specific Attributes (Vendor-ID 311)
    /// in RADIUS packets. This builder processes attributes directly into a buffer to minimize allocations.
    /// </summary>
    public RadiusMicrosoftAttributeBuilder(Span<byte> buffer)
    {
        if (buffer.Length < RadiusConstants.VsaHeaderSize) 
            throw new ArgumentException("Buffer too small for Microsoft VSA header");
        
        _buffer = buffer;
        _buffer[0] = RadiusConstants.VendorSpecificType;
        // _buffer[1] (Length) will be set in Complete()
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[2..6], RadiusConstants.MicrosoftVendorId);
        
        _offset = RadiusConstants.VsaHeaderSize;
    }

    /// <summary>
    /// Adds a Microsoft string sub-attribute (e.g., MS-CHAP2-Success).
    /// </summary>
    public void AddText(RadiusMsAttributeType subType, string value)
    {
        var bytesCount = Encoding.ASCII.GetByteCount(value);
        var totalSubLen = 1 + 1 + bytesCount; // Type + Len + Data

        if (_offset + totalSubLen > _buffer.Length)
            throw new InvalidOperationException($"Microsoft VSA overflow for {subType}");

        _buffer[_offset] = (byte)subType;
        _buffer[_offset + 1] = (byte)totalSubLen;
        Encoding.ASCII.GetBytes(value, _buffer.Slice(_offset + 2, bytesCount));

        _offset += totalSubLen;
    }

    /// <summary>
    /// Adds a Microsoft raw byte sub-attribute (e.g., MS-CHAP2-Response parts).
    /// </summary>
    public void AddBytes(RadiusMsAttributeType subType, ReadOnlySpan<byte> value)
    {
        var totalSubLen = 1 + 1 + value.Length;
        if (_offset + totalSubLen > _buffer.Length)
            throw new InvalidOperationException($"Microsoft VSA overflow for {subType}");

        _buffer[_offset] = (byte)subType;
        _buffer[_offset + 1] = (byte)totalSubLen;
        value.CopyTo(_buffer[(_offset + 2)..]);

        _offset += totalSubLen;
    }
    
    /// <summary>
    /// Adds a Microsoft 32-bit integer sub-attribute (e.g., MPPE-Encryption-Policy).
    /// </summary>
    public void AddInt32(RadiusMsAttributeType subType, uint value)
    {
        const byte subLen = 6; 
        if (_offset + subLen > _buffer.Length) 
            throw new InvalidOperationException($"Microsoft VSA overflow for {subType}");

        _buffer[_offset] = (byte)subType;
        _buffer[_offset + 1] = subLen;
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[(_offset + 2)..], value);
        
        _offset += subLen;
    }

    /// <summary>
    /// Finalizes the Microsoft VSA container by writing the total length.
    /// Returns the total number of bytes written to the buffer.
    /// </summary>
    public int Complete()
    {
        if (_offset > 255) 
            throw new InvalidOperationException("Microsoft VSA total length exceeds 255 bytes");
        
        _buffer[1] = (byte)_offset;
        return _offset;
    }
    
}