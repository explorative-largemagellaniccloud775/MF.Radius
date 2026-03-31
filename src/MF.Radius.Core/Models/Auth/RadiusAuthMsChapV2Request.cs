using MF.Radius.Core.Enums;

namespace MF.Radius.Core.Models.Auth;

/// <summary>
/// Represents an MS-CHAPv2 authentication request for the RADIUS protocol.
/// </summary>
/// <remarks>
/// This class extends the <see cref="RadiusAuthRequestBase"/> and encapsulates
/// the data required to process an MS-CHAPv2 authentication request in RADIUS.
/// It includes the authenticator challenge, MS-CHAPv2 response, as well as
/// methods for accessing key components of the response such as identification,
/// peer challenge, and NT response.
/// </remarks>
public record RadiusAuthMsChapV2Request
    : RadiusAuthRequestBase
{
    /// <summary>
    /// The Authenticator Challenge (16 bytes) provided by the NAS.
    /// Usually from the MS-CHAP-Challenge attribute.
    /// Authenticator - NAS;
    /// Backend Authenticator - RADIUS Server;
    /// </summary>
    public required ReadOnlyMemory<byte> AuthenticatorChallenge { get; init; }
    
    /// <summary>
    /// The raw binary data from the MS-CHAP-Response attribute (50 bytes).
    /// </summary>
    public required ReadOnlyMemory<byte> MsChap2Response { get; init; }

    /// <summary>The Ident byte (first byte of MS-CHAP-Response).</summary>
    public byte Ident => MsChap2Response.Span[0];

    /// <summary>The Peer Challenge (16 bytes) provided by the client (bytes 2-17).</summary>
    public ReadOnlyMemory<byte> PeerChallenge => MsChap2Response[2..18];

    /// <summary>The NT-Response (24 bytes) provided by the client (bytes 26-49).</summary>
    public ReadOnlyMemory<byte> NtResponse => MsChap2Response.Length >= 50 
        ? MsChap2Response[26..50] 
        : ReadOnlyMemory<byte>.Empty
        ;

    /// <summary>
    /// Internal: Stores the correct password after successful validation.
    /// </summary>
    internal string? StoredPassword { get; set; }

    public override RadiusAuthProtocol AuthProtocol => RadiusAuthProtocol.MsChapV2;
    
}
