using MF.Radius.Core.Enums;

namespace MF.Radius.Core.Models.Auth;

/// <summary>
/// Represents a RADIUS authentication request using the CHAP protocol. The CHAP protocol
/// performs a challenge-response mechanism for authentication, relying on the encoded
/// challenge and response data for validation.
/// </summary>
/// <seealso cref="RadiusAuthRequestBase" />
public record RadiusAuthChapRequest
    : RadiusAuthRequestBase
{
    /// <summary>
    /// The NAS-side challenge.
    /// Can be taken from CHAP-Challenge attribute or Request Authenticator.
    /// </summary>
    public ReadOnlyMemory<byte> Challenge { get; init; }

    /// <summary>
    /// The raw binary data from the CHAP-Password attribute.
    /// Byte 0: CHAP Identifier.
    /// Bytes 1-16: MD5 Response.
    /// </summary>
    public ReadOnlyMemory<byte> ChapPassword { get; init; }

    /// <summary>
    /// The identifier used in the CHAP authentication process (first byte of CHAP-Password).
    /// </summary>
    public byte ChapId => ChapPassword.Span[0];
    
    /// <summary>
    /// The MD5 hash response calculated by the client (bytes 1-16 of CHAP-Password).
    /// </summary>
    public ReadOnlyMemory<byte> Response => ChapPassword[1..];

    public override RadiusAuthProtocol AuthProtocol => RadiusAuthProtocol.Chap;
    
}
