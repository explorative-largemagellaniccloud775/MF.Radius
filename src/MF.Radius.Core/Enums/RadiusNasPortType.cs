namespace MF.Radius.Core.Enums;

/// <summary>
/// NAS-Port-Type attribute values (RFC 2865).
/// </summary>
public enum RadiusNasPortType
    : uint
{
    Async = 0,
    Sync = 1,
    IsdnSync = 2,
    IsdnAsyncV120 = 3,
    IsdnAsyncV110 = 4,
    Virtual = 5,
    Piagt = 6,
    Others = 7,
    X25 = 8,
    X75 = 9,
    G3Fax = 10,
    Sdsl = 11,
    AdslCap = 12,
    AdslDmt = 13,
    Idsl = 14,
    Ethernet = 15,
    Xdsl = 16,
    Cable = 17,
    WirelessOther = 18,
    Wireless80211 = 19
}