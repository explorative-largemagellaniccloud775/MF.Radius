namespace MF.Radius.SampleServer.Shared.Contracts;

/// <summary>
/// Represents a standardized envelope for API responses.
/// </summary>
/// <typeparam name="T">The type of the data payload contained in the response.</typeparam>
/// <remarks>
/// The envelope includes metadata such as the success status, HTTP status code, response code,
/// an optional data payload, a list of errors (if any), and a unique trace identifier for troubleshooting.
/// </remarks>
public record ApiEnvelope<T>(
    bool IsSuccess,
    int HttpStatus,
    string Code,
    T? Data,
    IReadOnlyList<ApiError> Errors,
    string TraceId
);
