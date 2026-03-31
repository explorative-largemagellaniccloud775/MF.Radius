namespace MF.Radius.SampleServer.Shared.Contracts;

/// <summary>
/// Represents an error response within an API context.
/// </summary>
/// <remarks>
/// This record is used to encapsulate error information in a structured way,
/// including an error code, an error message, and an optional target that
/// provides additional context about the error.
/// </remarks>
/// <param name="Code">
/// A string representing the specific error code associated with the error.
/// This code is often used for categorizing or identifying the type of error that occurred.
/// </param>
/// <param name="Message">
/// A string that describes the error in detail. This is intended to provide
/// a human-readable explanation of what caused the error.
/// </param>
/// <param name="Target">
/// An optional string that identifies the specific target of the error,
/// such as a parameter name, resource identifier, or process stage. This
/// parameter can be null if no specific target is applicable.
/// </param>
public record ApiError(
    string Code,
    string Message,
    string? Target = null
);
