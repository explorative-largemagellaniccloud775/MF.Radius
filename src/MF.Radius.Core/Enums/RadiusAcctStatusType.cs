namespace MF.Radius.Core.Enums;

/// <summary>
/// Acct-Status-Type attribute values (RFC 2866).
/// </summary>
public enum RadiusAcctStatusType
    : uint
{
    Start = 1,
    Stop = 2,
    InterimUpdate = 3,
    AccountingOn = 7,
    AccountingOff = 8,
    Failed = 15
}
