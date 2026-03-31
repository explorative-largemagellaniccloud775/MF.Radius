namespace MF.Radius.Core.Enums;

/// <summary>
/// Standard Vendor IDs (SMI Network Management Private Enterprise Codes) 
/// as maintained by IANA. These are used in Vendor-Specific Attributes (Type 26).
/// </summary>
public enum RadiusVendor
    : uint
{
    /// <summary>IETF standard (not usually used in VSA Header, but for reference).</summary>
    Ietf = 0,
    
    /// <summary>Cisco Systems, Inc.</summary>
    Cisco = 9,
    
    /// <summary>Microsoft Corporation. Used for MS-CHAP v2 and MPPE keys.</summary>
    Microsoft = 311,
    
    /// <summary>FreeRADIUS Project.</summary>
    FreeRadius = 11344,
    
    /// <summary>MikroTik.</summary>
    MikroTik = 14988,
    
    /// <summary>Ubiquiti Networks.</summary>
    Ubiquiti = 41112,
    
    /// <summary>Huawei Technologies Co., Ltd.</summary>
    Huawei = 2011,
    
    /// <summary>Juniper Networks.</summary>
    Juniper = 2636,
    
    /// <summary>Nokia (formerly Alcatel-Lucent).</summary>
    Nokia = 2849
    
}