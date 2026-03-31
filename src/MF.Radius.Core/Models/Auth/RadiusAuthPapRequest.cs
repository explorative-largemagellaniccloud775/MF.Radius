using MF.Radius.Core.Enums;

namespace MF.Radius.Core.Models.Auth;

/// <summary>
/// Represents a RADIUS authentication request that uses the PAP (Password Authentication Protocol).
/// This class encapsulates information specific to PAP authentication, including the user's password and
/// relevant RADIUS protocol details.
/// </summary>
/// <remarks>
/// In the RADIUS authentication flow, the PAP protocol transmits the user's credentials (username and
/// plaintext password) to the server. This class provides the necessary structure to handle and process
/// such requests.
/// </remarks>
public record RadiusAuthPapRequest
    : RadiusAuthRequestBase
{
    /// <summary>The decrypted password sent by the client.</summary>
    public required string Password { get; init; }

    public override RadiusAuthProtocol AuthProtocol => RadiusAuthProtocol.Pap;
    
}
