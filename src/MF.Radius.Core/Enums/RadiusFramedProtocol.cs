namespace MF.Radius.Core.Enums;

/// <summary>
/// Framed-Protocol attribute values (RFC 2865).
/// </summary>
public enum RadiusFramedProtocol
    : uint
{
    PPP = 1,
    SLIP = 2,
    ARAP = 3,
    Gandalf = 4,
    Xylogics = 5,
    X75 = 6
}