namespace MF.Radius.Core.Enums;

/// <summary>
/// RADIUS Error-Cause Attribute (Type 101) values as defined in RFC 3576 and RFC 5176.
/// Used in Disconnect-NAK and CoA-NAK packets.
/// </summary>
public enum RadiusErrorCause
    : uint
{
    // --- 2xx: Informational (Success/Standard operations) ---
    /// <summary>
    /// The session context was removed for a reason other than those listed below.
    /// </summary>
    ResidualSessionContextRemoved = 201,

    /// <summary>
    /// The session context was removed because the user's session time expired.
    /// </summary>
    InvalidEapPacket = 202,

    // --- 4xx: Protocol Errors (Problem with the request itself) ---
    /// <summary>
    /// The request contained an attribute that the receiver does not support.
    /// </summary>
    UnsupportedExtension = 401,

    /// <summary>
    /// The request was missing one or more required attributes or contained invalid values.
    /// </summary>
    InvalidRequest = 402,

    /// <summary>
    /// A required attribute (e.g., Service-Type) was missing from the request.
    /// </summary>
    MissingAttribute = 403,

    /// <summary>
    /// The Message-Authenticator attribute was invalid or missing when required.
    /// </summary>
    InvalidAttributeValue = 404,

    /// <summary>
    /// An attribute was present more than once when only one is allowed.
    /// </summary>
    UnsupportedAttribute = 405,

    /// <summary>
    /// The receiver was unable to parse an attribute (e.g., malformed EAP).
    /// </summary>
    InvalidPacketLength = 406,

    /// <summary>
    /// The Proxy or Server was unable to deliver the packet to the next hop.
    /// </summary>
    CommunicationWithNextHopFailed = 407,

    // --- 5xx: Server/NAS Errors (State or Resource issues) ---
    /// <summary>
    /// The session identified by the attributes in the request was not found.
    /// </summary>
    SessionContextNotFound = 503,

    /// <summary>
    /// The receiver is too busy or has insufficient resources to fulfill the request.
    /// </summary>
    ResourcesUnavailable = 506,

    /// <summary>
    /// The administrator has disabled the ability to perform this operation.
    /// </summary>
    RequestInitiatedDisconnectNotSupported = 507,

    /// <summary>
    /// The request was ignored because it would result in a duplicate operation.
    /// </summary>
    MultipleSessionContextFound = 508,

    /// <summary>
    /// General error indicating the request was not authorized by policy.
    /// </summary>
    AdministrativeDisallow = 601
}