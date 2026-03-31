using System.Buffers.Binary;
using System.Net;
using System.Text;
using MF.Radius.Core.Enums;

namespace MF.Radius.Core.Packets.Builders;

/// <summary>
/// A high-performance builder for all RADIUS attribute types defined in RFC 8044.
/// </summary>
public ref struct RadiusAttributeBuilder(Span<byte> buffer)
{
    private readonly Span<byte> _buffer = buffer;
    private int _offset = 0;

    /// <summary>
    /// Adds a 32-bit unsigned integer (RFC 8044 Type 1, 2, 3).
    /// </summary>
    public void AddInt32(RadiusAttributeType type, uint value)
    {
        const byte attrSize = 6;
        EnsureCapacity(attrSize);
        _buffer[_offset] = (byte)type;
        _buffer[_offset + 1] = attrSize;
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[(_offset + 2)..], value);
        _offset += attrSize;
    }

    /// <summary>
    /// Adds a 64-bit unsigned integer (RFC 8044 Type 12).
    /// </summary>
    public void AddInt64(RadiusAttributeType type, ulong value)
    {
        const byte attrSize = 10;
        EnsureCapacity(attrSize);
        _buffer[_offset] = (byte)type;
        _buffer[_offset + 1] = attrSize;
        BinaryPrimitives.WriteUInt64BigEndian(_buffer[(_offset + 2)..], value);
        _offset += attrSize;
    }

    /// <summary>
    /// Adds a UTF-8 encoded text attribute (RFC 8044 Type 4).
    /// </summary>
    public void AddText(RadiusAttributeType type, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        var totalLen = 2 + byteCount;
        if (totalLen > 255) throw new ArgumentException("Text attribute exceeds 255 bytes");

        EnsureCapacity(totalLen);
        _buffer[_offset] = (byte)type;
        _buffer[_offset + 1] = (byte)totalLen;
        Encoding.UTF8.GetBytes(value, _buffer[(_offset + 2)..]);
        _offset += totalLen;
    }

    public void AddIpV4(RadiusAttributeType type, IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new ArgumentException("Address must be IPv4");

        const byte totalLen = 6;
        EnsureCapacity(totalLen);
        _buffer[_offset] = (byte)type;
        _buffer[_offset + 1] = totalLen;
        address.TryWriteBytes(_buffer.Slice(_offset + 2, 4), out _);
        _offset += totalLen;
    }

    public void AddIpV6(RadiusAttributeType type, IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
            throw new ArgumentException("Address must be IPv6");

        const byte totalLen = 18;
        EnsureCapacity(totalLen);
        _buffer[_offset] = (byte)type;
        _buffer[_offset + 1] = totalLen;
        address.TryWriteBytes(_buffer.Slice(_offset + 2, 16), out _);
        _offset += totalLen;
    }

    /// <summary>
    /// Adds a prefix attribute (IPv4 or IPv6) following the RADIUS format.
    /// Format: [Type][Length][Reserved(0)][PrefixLength][AddressBytes]
    /// </summary>
    public void AddPrefix(RadiusAttributeType type, byte prefixLen, ReadOnlySpan<byte> addressBytes)
    {
        var totalLen = 2 + 2 + addressBytes.Length; // type + len + reserved + prefixLen + bytes
        EnsureCapacity(totalLen);
        _buffer[_offset] = (byte)type;
        _buffer[_offset + 1] = (byte)totalLen;
        _buffer[_offset + 2] = 0; // Reserved
        _buffer[_offset + 3] = prefixLen;
        addressBytes.CopyTo(_buffer[(_offset + 4)..]);
        _offset += totalLen;
    }

    /// <summary>
    /// Adds a raw octet string attribute (RFC 8044 Type 5).
    /// </summary>
    public void AddBytes(RadiusAttributeType type, ReadOnlySpan<byte> value)
    {
        var totalLen = 2 + value.Length;
        if (totalLen > 255) throw new ArgumentException("Attribute length exceeds 255 bytes");

        EnsureCapacity(totalLen);
        _buffer[_offset] = (byte)type;
        _buffer[_offset + 1] = (byte)totalLen;
        value.CopyTo(_buffer[(_offset + 2)..]);
        _offset += totalLen;
    }
    
    /// <summary>
    /// Adds a Vendor-Specific Attribute (Type 26).
    /// </summary>
    public void AddVendorSpecific(uint vendorId, byte vendorType, ReadOnlySpan<byte> value)
    {
        var vendorAttributeLen = (byte)(value.Length + 2);
        var totalLen = 6 + vendorAttributeLen;
        if (totalLen > 255) throw new ArgumentException("VSA attribute exceeds 255 bytes");

        EnsureCapacity(totalLen);
        _buffer[_offset] = 26; // Vendor-Specific
        _buffer[_offset + 1] = (byte)totalLen;
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[(_offset + 2)..], vendorId);
            
        _buffer[_offset + 6] = vendorType;
        _buffer[_offset + 7] = vendorAttributeLen;
        value.CopyTo(_buffer[(_offset + 8)..]);
            
        _offset += totalLen;
    }

    private void EnsureCapacity(int size)
    {
        if (_offset + size > _buffer.Length)
            throw new InvalidOperationException("Attribute buffer overflow");
    }

    /// <summary>
    /// Returns the total number of bytes written by this builder.
    /// </summary>
    public int Complete() => _offset;
    
}
