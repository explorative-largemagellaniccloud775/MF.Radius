
namespace MF.Radius.Core.Enums;

/// <summary>
/// Represents the RADIUS packet type codes and their respective values.
/// These codes are used to identify the type of RADIUS packets being sent or received.
/// The enumeration contains the following categories of codes:
/// - Authentication and Authorization codes (e.g., AccessRequest, AccessAccept, AccessReject).
/// - Accounting codes (e.g., AccountingRequest, AccountingResponse, AccountingStatus).
/// - Experimental codes as per RFC 3575 (e.g., PasswordRequest, PasswordAck, PasswordReject).
/// - Dynamic Authorization codes as defined in RFC 5176 (e.g., DisconnectRequest, CoARequest).
/// - NAS reboot codes (e.g., NasRebootRequest, NasRebootResponse).
/// - Special purpose codes (e.g., ProtocolError, Reserved).
/// </summary>
public enum RadiusCode
    : byte
{
    /// <summary>
    /// Access-Request (RFC 2865)
    /// </summary>
    AccessRequest = 1,

    /// <summary>
    /// Access-Accept (RFC 2865)
    /// </summary>
    AccessAccept = 2,

    /// <summary>
    /// Access-Reject (RFC 2865)
    /// </summary>
    AccessReject = 3,

    /// <summary>
    /// Accounting-Request (RFC 2866)
    /// </summary>
    AccountingRequest = 4,

    /// <summary>
    /// Accounting-Response (RFC 2866)
    /// </summary>
    AccountingResponse = 5,
    
    /// <summary>
    /// Accounting-Status (RFC 3575) - Experimental
    /// </summary>
    AccountingStatus = 6,
    
    /// <summary>
    /// Password-Request (RFC 3575) - Experimental
    /// </summary>
    PasswordRequest = 7,
    
    /// <summary>
    /// Password-Ack (RFC 3575) - Experimental
    /// </summary>
    PasswordAck = 8,
    
    /// <summary>
    /// Password-Reject (RFC 3575) - Experimental
    /// </summary>
    PasswordReject = 9,
    
    /// <summary>
    /// Accounting-Message (RFC 3575) - Experimental
    /// </summary>
    AccountingMessage = 10,
    
    /// <summary>
    /// Access-Challenge (RFC 2865)
    /// </summary>
    AccessChallenge = 11,
    
    /// <summary>
    /// Status-Server (RFC 2865, RFC 5997)
    /// </summary>
    StatusServer = 12,
    
    /// <summary>
    /// Status-Client (RFC 2865, RFC 5997)
    /// </summary>
    StatusClient = 13,
    
    /// <summary>
    /// Resource-Free-Request (RFC 3575) - Experimental
    /// </summary>
    ResourceFreeRequest = 21,
    
    /// <summary>
    /// Resource-Free-Response (RFC 3575) - Experimental
    /// </summary>
    ResourceFreeResponse = 22,
    
    /// <summary>
    /// Resource-Query-Request (RFC 3575) - Experimental
    /// </summary>
    ResourceQueryRequest = 23,
    
    /// <summary>
    /// Resource-Query-Response (RFC 3575) - Experimental
    /// </summary>
    ResourceQueryResponse = 24,
    
    /// <summary>
    /// Alternate-Resource-Reclaim-Request (RFC 3575) - Experimental
    /// </summary>
    AlternateResourceReclaimRequest = 25,
    
    /// <summary>
    /// NAS-Reboot-Request (RFC 5176)
    /// </summary>
    NasRebootRequest = 26,
    
    /// <summary>
    /// NAS-Reboot-Response (RFC 5176)
    /// </summary>
    NasRebootResponse = 27,
    
    // Reserved: 28
    
    /// <summary>
    /// Next-Passcode (RFC 3575) - Experimental
    /// </summary>
    NextPasscode = 29,
    
    /// <summary>
    /// New-Pin (RFC 3575) - Experimental
    /// </summary>
    NewPin = 30,
    
    /// <summary>
    /// Terminate-Session (RFC 3575) - Experimental
    /// </summary>
    TerminateSession = 31,
    
    /// <summary>
    /// Password-Expired (RFC 3575) - Experimental
    /// </summary>
    PasswordExpired = 32,
    
    /// <summary>
    /// Event-Request (RFC 3575) - Experimental
    /// </summary>
    EventRequest = 33,
    
    /// <summary>
    /// Event-Response (RFC 3575) - Experimental
    /// </summary>
    EventResponse = 34,
    
    // Reserved: 35-39
    
    /// <summary>
    /// Disconnect-Request (RFC 5176, RFC 3575)
    /// </summary>
    DisconnectRequest = 40,
    
    /// <summary>
    /// Disconnect-ACK (RFC 5176, RFC 3575)
    /// </summary>
    DisconnectAck = 41,
    
    /// <summary>
    /// Disconnect-NAK (RFC 5176, RFC 3575)
    /// </summary>
    DisconnectNak = 42,
    
    /// <summary>
    /// CoA-Request (Change of Authorization Request) (RFC 5176, RFC 3575)
    /// </summary>
    CoARequest = 43,
    
    /// <summary>
    /// CoA-ACK (Change of Authorization Acknowledgement) (RFC 5176, RFC 3575)
    /// </summary>
    CoAAck = 44,
    
    /// <summary>
    /// CoA-NAK (Change of Authorization Negative Acknowledgement) (RFC 5176, RFC 3575)
    /// </summary>
    CoANak = 45,
    
    // Reserved: 46-49
    
    /// <summary>
    /// IP-Address-Allocate (RFC 3575) - Experimental
    /// </summary>
    IpAddressAllocate = 50,
    
    /// <summary>
    /// IP-Address-Release (RFC 3575) - Experimental
    /// </summary>
    IpAddressRelease = 51,
    
    /// <summary>
    /// Protocol-Error (RFC 5176)
    /// </summary>
    ProtocolError = 52,
    
    // Reserved: 53-249
    
    // Reserved for Experimental Use: 250-253 (RFC 3575)
    
    /// <summary>
    /// Reserved (RFC 2865)
    /// </summary>
    Reserved = 255
    
}