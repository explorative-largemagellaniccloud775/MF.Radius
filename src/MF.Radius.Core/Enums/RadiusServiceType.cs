namespace MF.Radius.Core.Enums;

/// <summary>
/// Service-Type attribute values (RFC 2865).
/// </summary>
public enum RadiusServiceType
    : uint
{
    Login = 1,
    Framed = 2,
    CallbackLogin = 3,
    CallbackFramed = 4,
    Outbound = 5,
    Administrative = 6,
    NasPrompt = 7,
    AuthenticateOnly = 8,
    CallbackNasPrompt = 9,
    CallCheck = 10,
    CallbackAdministrative = 11
}
