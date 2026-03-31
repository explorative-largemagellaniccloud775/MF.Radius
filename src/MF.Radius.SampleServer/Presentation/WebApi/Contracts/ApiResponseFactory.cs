using MF.Radius.SampleServer.Shared.Contracts;

namespace MF.Radius.SampleServer.Presentation.WebApi.Contracts;

/// <summary>
/// Builds unified HTTP JSON responses for the public API.
/// </summary>
/// <remarks>
/// This factory enforces a consistent transport contract based on <see cref="ApiEnvelope{T}"/>:
/// success and failure responses share the same shape and always include a trace identifier.
/// Keep domain/business logic outside of this class; it should only format HTTP responses.
/// </remarks>
public static class ApiResponseFactory
{
    
    /// <summary>
    /// Creates a successful API response envelope and returns it as JSON.
    /// </summary>
    /// <typeparam name="T">Type of payload stored in the <c>data</c> field.</typeparam>
    /// <param name="httpContext">Current HTTP context used to populate <c>traceId</c>.</param>
    /// <param name="data">Response payload returned to the client.</param>
    /// <param name="code">Machine-readable domain code (for example, <c>SUBSCRIBER_FOUND</c>).</param>
    /// <param name="statusCode">HTTP status code. Defaults to <see cref="StatusCodes.Status200OK"/>.</param>
    /// <returns>
    /// An <see cref="IResult"/> containing a JSON body with <see cref="ApiEnvelope{T}"/>
    /// where <c>isSuccess</c> is <see langword="true"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="code"/> is empty or whitespace.
    /// </exception>
    public static IResult Success<T>(
        HttpContext httpContext,
        T data,
        string code,
        int statusCode = StatusCodes.Status200OK
    )
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code must not be empty.", nameof(code));

        var body = new ApiEnvelope<T>(
            IsSuccess: true,
            HttpStatus: statusCode,
            Code: code,
            Data: data,
            Errors: Array.Empty<ApiError>(),
            TraceId: httpContext.TraceIdentifier
        );
        return Results.Json(body, statusCode: statusCode);
    }

    /// <summary>
    /// Creates a failed API response envelope with a single error and returns it as JSON.
    /// </summary>
    /// <typeparam name="T">Expected payload type for the endpoint contract.</typeparam>
    /// <param name="httpContext">Current HTTP context used to populate <c>traceId</c>.</param>
    /// <param name="statusCode">HTTP status code representing the failure.</param>
    /// <param name="code">Machine-readable top-level error code.</param>
    /// <param name="message">Human-readable error description.</param>
    /// <param name="target">Optional error target (for example, field name).</param>
    /// <returns>
    /// An <see cref="IResult"/> containing a JSON body with <see cref="ApiEnvelope{T}"/>
    /// where <c>isSuccess</c> is <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="code"/> or <paramref name="message"/> is empty or whitespace.
    /// </exception>
    public static IResult Fail<T>(
        HttpContext httpContext,
        int statusCode,
        string code,
        string message,
        string? target = null
    )
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must not be empty.", nameof(message));

        return Fail<T>(
            httpContext,
            statusCode,
            code,
            [new ApiError(code, message, target)]
        );
    }

    /// <summary>
    /// Creates a failed API response envelope with multiple errors and returns it as JSON.
    /// </summary>
    /// <typeparam name="T">Expected payload type for the endpoint contract.</typeparam>
    /// <param name="httpContext">Current HTTP context used to populate <c>traceId</c>.</param>
    /// <param name="statusCode">HTTP status code representing the failure.</param>
    /// <param name="code">Machine-readable top-level error code.</param>
    /// <param name="errors">Detailed error list.</param>
    /// <returns>
    /// An <see cref="IResult"/> containing a JSON body with <see cref="ApiEnvelope{T}"/>
    /// where <c>isSuccess</c> is <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> or <paramref name="errors"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="code"/> is empty or whitespace, or when <paramref name="errors"/> is empty.
    /// </exception>
    public static IResult Fail<T>(
        HttpContext httpContext,
        int statusCode,
        string code,
        IReadOnlyList<ApiError> errors
    )
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(errors);
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code must not be empty.", nameof(code));
        if (errors.Count == 0)
            throw new ArgumentException("At least one error is required.", nameof(errors));

        var body = new ApiEnvelope<T>(
            IsSuccess: false,
            HttpStatus: statusCode,
            Code: code,
            Data: default,
            Errors: errors,
            TraceId: httpContext.TraceIdentifier
        );
        return Results.Json(body, statusCode: statusCode);
    }

    /// <summary>
    /// Creates a validation failure response envelope from multiple field errors.
    /// </summary>
    /// <typeparam name="T">Expected payload type for the endpoint contract.</typeparam>
    /// <param name="httpContext">Current HTTP context used to populate <c>traceId</c>.</param>
    /// <param name="code">Validation error code (for example, <c>VALIDATION_FAILED</c>).</param>
    /// <param name="errors">Validation error sequence.</param>
    /// <param name="statusCode">HTTP status code. Defaults to <see cref="StatusCodes.Status400BadRequest"/>.</param>
    /// <returns>A JSON response built via <see cref="Fail{T}(HttpContext,int,string,IReadOnlyList{ApiError})"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> or <paramref name="errors"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="code"/> is empty or whitespace, or when <paramref name="errors"/> yields no items.
    /// </exception>
    public static IResult ValidationFail<T>(
        HttpContext httpContext,
        string code,
        IEnumerable<ApiError> errors,
        int statusCode = StatusCodes.Status400BadRequest
    )
    {
        ArgumentNullException.ThrowIfNull(errors);
        var materialized = errors as IReadOnlyList<ApiError> ?? errors.ToArray();
        return Fail<T>(httpContext, statusCode, code, materialized);
    }
    
}
