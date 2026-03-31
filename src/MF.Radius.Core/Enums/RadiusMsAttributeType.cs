namespace MF.Radius.Core.Enums;

/// <summary>
/// Microsoft Vendor-Specific Attributes (VSA) for RADIUS, as defined in RFC 2548.
/// These attributes are encapsulated within the Vendor-Specific attribute (Type 26) with Vendor-ID 311.
/// </summary>
public enum RadiusMsAttributeType
    : byte
{
    /// <summary>Used in MS-CHAPv1 to carry the challenge response (Type 1).</summary>
    MsChapResponse = 1,

    /// <summary>Carries error information in MS-CHAP responses (Type 2).</summary>
    MsChapError = 2,

    /// <summary>Used to change the user's password in MS-CHAPv1 (Type 3).</summary>
    MsChapCpw1 = 3,

    /// <summary>Used to change the user's password in MS-CHAPv1 (Type 4).</summary>
    MsChapCpw2 = 4,

    /// <summary>Carries the LM encryption key for MPPE (Type 5).</summary>
    MsChapLmEncKey = 5,

    /// <summary>Carries the NT encryption key for MPPE (Type 6).</summary>
    MsChapNtEncKey = 6,

    /// <summary>Defines if encryption is required, allowed, or prohibited (Type 7).</summary>
    MsMppeEncryptionPolicy = 7,

    /// <summary>Defines supported encryption types (e.g., 40-bit, 128-bit) (Type 8).</summary>
    MsMppeEncryptionTypes = 8,

    /// <summary>The challenge string used in MS-CHAPv1 or MS-CHAPv2 (Type 11).</summary>
    MsChapChallenge = 11,

    /// <summary>Used in MS-CHAPv1 to pass encryption keys (Type 12).</summary>
    MsChapMppeKeys = 12,

    /// <summary>The MPPE Send Key used to encrypt data from the NAS to the client (Type 16).</summary>
    MsMppeSendKey = 16,

    /// <summary>The MPPE Receive Key used to decrypt data from the client to the NAS (Type 17).</summary>
    MsMppeRecvKey = 17,

    /// <summary>The primary DNS server to be assigned to the client (Type 21).</summary>
    MsPrimaryDnsServer = 21,

    /// <summary>The secondary DNS server to be assigned to the client (Type 22).</summary>
    MsSecondaryDnsServer = 22,

    /// <summary>Carries the MS-CHAPv2 peer challenge and NT-response (Type 25).</summary>
    MsChap2Response = 25,

    /// <summary>Carries the authenticator response for a successful MS-CHAPv2 login (Type 26).</summary>
    MsChap2Success = 26,

    /// <summary>Used to change the user's password in MS-CHAPv2 (Type 27).</summary>
    MsChap2Cpw = 27
    
}