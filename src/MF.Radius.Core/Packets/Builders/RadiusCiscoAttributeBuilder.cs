using System.Buffers.Binary;
using System.Net;
using System.Text;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;

namespace MF.Radius.Core.Packets.Builders;

/// <summary>
/// A high-performance builder for Cisco Vendor-Specific Attributes (Vendor-ID 9).
/// </summary>
public ref struct RadiusCiscoAttributeBuilder
{
    private readonly Span<byte> _buffer;
    private int _offset;

    /// <summary>
    /// A high-performance builder for Cisco Vendor-Specific Attributes (Vendor-ID 9).
    /// Provides methods to construct and manipulate Cisco-specific AV-Pairs
    /// and Sub-Attributes within a given buffer.
    /// </summary>
    public RadiusCiscoAttributeBuilder(Span<byte> buffer)
    {
        if (buffer.Length < RadiusConstants.VsaHeaderSize) throw new ArgumentException("Buffer too small for Cisco VSA header");
        
        _buffer = buffer;
        _buffer[0] = RadiusConstants.VendorSpecificType;
        // _buffer[1] (Length) will be set in Complete()
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[2..6], RadiusConstants.CiscoVendorId);
        
        _offset = RadiusConstants.VsaHeaderSize;
    }

    /// <summary>
    /// Adds a Cisco AV-Pair (Sub-type 1) in "key=value" format without extra allocations.
    /// </summary>
    public void AddAvPair(string key, string value)
    {
        // Calculate total sub-attribute length: sub-type(1) + sub-len(1) + key + '=' + value
        var keyBytes = Encoding.UTF8.GetByteCount(key);
        var valBytes = Encoding.UTF8.GetByteCount(value);
        var totalSubLen = 1 + 1 + keyBytes + 1 + valBytes;

        if (_offset + totalSubLen > _buffer.Length)
            throw new InvalidOperationException("Cisco VSA buffer overflow while adding AV-Pair");

        _buffer[_offset] = 1;  // Sub-type: Cisco-AVPair
        _buffer[_offset + 1] = (byte)totalSubLen;  // calculated
        
        var current = _offset + 2;
        current += Encoding.UTF8.GetBytes(key, _buffer[current..]);
        _buffer[current++] = (byte)'=';
        current += Encoding.UTF8.GetBytes(value, _buffer[current..]);

        _offset += totalSubLen;
    }
    
    /// <summary>
    /// Adds a Cisco string sub-attribute.
    /// </summary>
    public void AddString(RadiusCiscoAttributeType subType, string value)
    {
        var bytesCount = Encoding.UTF8.GetByteCount(value);
        var totalSubLen = 1 + 1 + bytesCount;

        if (_offset + totalSubLen > _buffer.Length)
            throw new InvalidOperationException($"Cisco VSA overflow for {subType}");

        _buffer[_offset] = (byte)subType;
        _buffer[_offset + 1] = (byte)totalSubLen;
        Encoding.UTF8.GetBytes(value, _buffer.Slice(_offset + 2, bytesCount));

        _offset += totalSubLen;
    }

    /// <summary>
    /// Adds a Cisco 32-bit integer sub-attribute.
    /// </summary>
    public void AddInt32(RadiusCiscoAttributeType subType, uint value)
    {
        const byte subLen = 6; 
        if (_offset + subLen > _buffer.Length) 
            throw new InvalidOperationException($"Cisco VSA overflow for {subType}");

        _buffer[_offset] = (byte)subType;
        _buffer[_offset + 1] = subLen;
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[(_offset + 2)..], value);
        
        _offset += subLen;
    }

    /// <summary>
    /// Adds a Cisco IPv4 address sub-attribute.
    /// </summary>
    public void AddIpV4(RadiusCiscoAttributeType subType, IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new ArgumentException("Only IPv4 addresses are supported in this Cisco VSA method");
        
        const byte subLen = 6;
        if (_offset + subLen > _buffer.Length) 
            throw new InvalidOperationException($"Cisco VSA overflow for {subType}");

        _buffer[_offset] = (byte)subType;
        _buffer[_offset + 1] = subLen;
        
        if (!address.TryWriteBytes(_buffer[(_offset + 2)..], out _))
            throw new ArgumentException($"Invalid IPv4 for {subType}");

        _offset += subLen;
    }
    
    /// <summary>
    /// Adds a raw byte sub-attribute into the Cisco VSA container.
    /// </summary>
    public void AddBytes(RadiusCiscoAttributeType subType, ReadOnlySpan<byte> value)
    {
        var totalSubLen = 1 + 1 + value.Length;
        if (_offset + totalSubLen > _buffer.Length)
            throw new InvalidOperationException($"Cisco VSA overflow for {subType}");

        _buffer[_offset] = (byte)subType;
        _buffer[_offset + 1] = (byte)totalSubLen;
        value.CopyTo(_buffer[(_offset + 2)..]);

        _offset += totalSubLen;
    }

    /// <summary>
    /// Finalizes the Cisco VSA container by writing the total length.
    /// Returns the total number of bytes written to the buffer.
    /// </summary>
    public int Complete()
    {
        if (_offset > 255) throw new InvalidOperationException("Cisco VSA total length exceeds 255 bytes");
        _buffer[1] = (byte)_offset;
        return _offset;
    }
    
}
