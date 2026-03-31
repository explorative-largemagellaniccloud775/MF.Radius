namespace MF.Radius.Core.Enums;

/// <summary>
/// RADIUS Attribute Types as defined in RFC 8044 and related specifications.
/// Includes attributes for Authentication (RFC 2865), Accounting (RFC 2866), 
/// Tunneling (RFC 2867/2868), IPv6 (RFC 3162), and Dynamic Authorization (RFC 5176).
/// </summary>
public enum RadiusAttributeType
    : byte
{
    // --- Authentication Attributes (RFC 2865) ---
    UserName = 1,
    UserPassword = 2,
    ChapPassword = 3,
    NasIpAddress = 4,
    NasPort = 5,
    ServiceType = 6,
    FramedProtocol = 7,
    FramedIpAddress = 8,
    FramedIpNetmask = 9,
    FramedRouting = 10,
    FilterId = 11,
    FramedMtu = 12,
    FramedCompression = 13,
    LoginIpHost = 14,
    LoginService = 15,
    LoginTcpPort = 16,
    ReplyMessage = 18,
    CallbackNumber = 19,
    CallbackId = 20,
    FramedRoute = 22,
    FramedIpxNetwork = 23,
    State = 24,
    Class = 25,
    VendorSpecific = 26,
    SessionTimeout = 27,
    IdleTimeout = 28,
    TerminationAction = 29,
    CalledStationId = 30,
    CallingStationId = 31,
    NasIdentifier = 32,
    ProxyState = 33,
    LoginLatService = 34,
    LoginLatNode = 35,
    LoginLatGroup = 36,
    FramedAppleTalkLink = 37,
    FramedAppleTalkNetwork = 38,
    FramedAppleTalkZone = 39,

    // --- Accounting Attributes (RFC 2866) ---
    AcctStatusType = 40,
    AcctDelayTime = 41,
    AcctInputOctets = 42,
    AcctOutputOctets = 43,
    AcctSessionId = 44,
    AcctAuthentic = 45,
    AcctSessionTime = 46,
    AcctInputPackets = 47,
    AcctOutputPackets = 48,
    AcctTerminateCause = 49,
    AcctMultiSessionId = 50,
    AcctLinkCount = 51,
    AcctInputGigawords = 52,
    AcctOutputGigawords = 53,
    EventTimestamp = 55,

    // --- CHAP & MS-CHAP Extensions (RFC 2865, 2548) ---
    ChapChallenge = 60,
    
    // --- Other Common Attributes ---
    NasPortType = 61,
    PortLimit = 62,
    LoginLatPort = 63,

    // --- Tunneling Attributes (RFC 2867, 2868) ---
    TunnelType = 64,
    TunnelMediumType = 65,
    TunnelClientEndpoint = 66,
    TunnelServerEndpoint = 67,
    AcctTunnelConnection = 68,
    TunnelPassword = 69,
    TunnelPrivateGroupId = 81,
    TunnelAssignmentId = 82,
    TunnelPreference = 83,
    TunnelClientAuthId = 90,
    TunnelServerAuthId = 91,

    // --- ARAP & EAP (RFC 2869) ---
    PasswordRetry = 75,
    Prompt = 76,
    ConnectInfo = 77,
    ConfigurationToken = 78,
    EapMessage = 79,
    MessageAuthenticator = 80,
    ARAPPassword = 70,
    ARAPFeatures = 71,
    ARAPZoneAccess = 72,
    ARAPSecurity = 73,
    ARAPSecurityData = 74,

    // --- IPv6 Attributes (RFC 3162, 4818) ---
    NasIpv6Address = 95,
    FramedInterfaceId = 96,
    FramedIpv6Prefix = 97,
    LoginIpv6Host = 98,
    FramedIpv6Route = 99,
    FramedIpv6Pool = 100,
    DelegatedIpv6Prefix = 123,

    // --- Dynamic Authorization (RFC 5176) ---
    ErrorCause = 101,

    // --- Common Extensions ---
    AcctInterimInterval = 85,
    NasPortId = 87,
    FramedPool = 88,
    ChargeableUserIdentity = 89,
    NasFilterRule = 92
    
}