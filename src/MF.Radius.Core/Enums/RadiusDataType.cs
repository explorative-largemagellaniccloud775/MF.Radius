
namespace MF.Radius.Core.Enums;

/// <summary>
/// Defines the RADIUS data types as specified in RFC 8044.
/// These types determine how attribute values are serialized and interpreted within the protocol.
/// See: https://www.iana.org/assignments/radius-types/radius-types.xhtml#radius-types-27
/// See2: https://datatracker.ietf.org/doc/html/rfc8044#page-10
/// </summary>
public enum RadiusDataType
{
    /// <summary>
    /// A 32-bit unsigned integer or an enumerated value.
    /// (RFC 8044 Types 1 and 2)
    /// </summary>
    Integer, Enum,

    /// <summary>
    /// A 32-bit unsigned integer representing seconds since the Unix Epoch (1970-01-01 00:00:00 UTC).
    /// (RFC 8044 Type 3)
    /// </summary>
    Time,

    /// <summary>
    /// A UTF-8 encoded string.
    /// (RFC 8044 Type 4)
    /// </summary>
    Text,

    /// <summary>
    /// A sequence of binary octets.
    /// (RFC 8044 Types 5 and 6)
    /// </summary>
    String,

    /// <summary>
    /// An 8-octet IPv6 Interface Identifier.
    /// (RFC 8044 Type 7)
    /// </summary>
    InterfaceId,

    /// <summary>
    /// A 4-octet IPv4 address in network byte order.
    /// (RFC 8044 Type 8)
    /// </summary>
    IpV4Addr,

    /// <summary>
    /// A 16-octet IPv6 address in network byte order.
    /// (RFC 8044 Type 9)
    /// </summary>
    IpV6Addr,

    /// <summary>
    /// An IPv6 prefix consisting of a length octet and the prefix data.
    /// (RFC 8044 Type 10)
    /// </summary>
    IpV6Prefix,

    /// <summary>
    /// An IPv4 prefix consisting of a length octet and the prefix data.
    /// (RFC 8044 Type 11)
    /// </summary>
    IpV4Prefix,

    /// <summary>
    /// A 64-bit unsigned integer in network byte order.
    /// (RFC 8044 Type 12)
    /// </summary>
    Integer64,

    /// <summary>
    /// A Type-Length-Value structure for nested attributes.
    /// (RFC 8044 Type 13)
    /// </summary>
    Tlv,

    /// <summary>
    /// A Vendor-Specific Attribute structure.
    /// (RFC 8044 Type 14)
    /// </summary>
    Vsa,

    /// <summary>
    /// An extended attribute format for larger data sizes.
    /// (RFC 8044 Types 15 and 16)
    /// </summary>
    Extended,

    /// <summary>
    /// An Extended Vendor-Specific Attribute structure.
    /// (RFC 8044 Type 17)
    /// </summary>
    Evs,

    /// <summary>
    /// A specialized Vendor-Specific Attribute format for Cisco AV-Pairs (e.g., "key=value").
    /// </summary>
    CiscoVsa
    
}
