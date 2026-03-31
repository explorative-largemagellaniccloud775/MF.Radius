using MF.Radius.Core.Enums;

namespace MF.Radius.SampleServer.Application.Features.Nas.Models;

/// <summary>
/// Represents the result of executing a command on a NAS (Network Access Server).
/// </summary>
public record NasCommandResult
{
    public required NasCommandStatus Status { get; init; }

    /// <summary>
    /// Protocol-level response code from NAS (if a response was received).
    /// </summary>
    public RadiusCode? RadiusResponseCode { get; init; }

    /// <summary>
    /// RADIUS packet identifier (if a response was received).
    /// </summary>
    public byte? RadiusIdentifier { get; init; }

    /// <summary>
    /// Application-level error classification.
    /// </summary>
    public NasCommandFailureReason FailureReason { get; init; } = NasCommandFailureReason.None;

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => Status == NasCommandStatus.Success;

    public static NasCommandResult Success(RadiusCode code, byte identifier) => new()
    {
        Status = NasCommandStatus.Success,
        RadiusResponseCode = code,
        RadiusIdentifier = identifier,
    };

    public static NasCommandResult Rejected(RadiusCode code, byte identifier, string? message = null) => new()
    {
        Status = NasCommandStatus.Rejected,
        RadiusResponseCode = code,
        RadiusIdentifier = identifier,
        ErrorMessage = message,
    };

    public static NasCommandResult Timeout() => new()
    {
        Status = NasCommandStatus.Timeout,
        FailureReason = NasCommandFailureReason.NasNotResponding,
        ErrorMessage = "NAS did not respond within the configured timeout."
    };

    public static NasCommandResult InvalidInput(string message) => new()
    {
        Status = NasCommandStatus.InvalidInput,
        FailureReason = NasCommandFailureReason.ValidationError,
        ErrorMessage = message,
    };

    public static NasCommandResult Failed(
        NasCommandFailureReason reason,
        string message,
        RadiusCode? code = null,
        byte? identifier = null
    ) => new()
    {
        Status = NasCommandStatus.Failed,
        FailureReason = reason,
        ErrorMessage = message,
        RadiusResponseCode = code,
        RadiusIdentifier = identifier,
    };
    
}
