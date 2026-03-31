namespace MF.Radius.Core.Enums;

/// <summary>
/// Cisco Vendor-Specific Attribute Sub-types (Vendor-ID 9).
/// These are sub-attributes carried within RADIUS Attribute 26.
/// </summary>
public enum RadiusCiscoAttributeType
    : byte
{
    /// <summary>Cisco AV-Pair (Sub-type 1, string/text).</summary>
    AvPair = 1,
    
    /// <summary>NAS-Port for Cisco (Sub-type 2, string/text).</summary>
    NasPort = 2,
    
    /// <summary>Multilink Endpoint Discriminator (Sub-type 11, binary).</summary>
    MultilinkEndpoint = 11,

    /// <summary>Inbound ACL name (Sub-type 19, string/text).</summary>
    PreAuthAcl = 19,
    
    /// <summary>Output packets (Sub-type 20, integer).</summary>
    OutputPackets = 20,
    
    /// <summary>Input packets (Sub-type 21, integer).</summary>
    InputPackets = 21,
    
    /// <summary>Output octets (Sub-type 22, integer).</summary>
    OutputOctets = 22,
    
    /// <summary>Input octets (Sub-type 23, integer).</summary>
    InputOctets = 23,
    
    /// <summary>Maximum session time (Sub-type 24, integer).</summary>
    MaximumTime = 24,
    
    /// <summary>Maximum data throughput (Sub-type 25, integer).</summary>
    MaximumData = 25,

    /// <summary>Cisco Disconnect Cause (Sub-type 195, integer).</summary>
    DisconnectCause = 195,

    /// <summary>Service Info (Sub-type 250, string/text).</summary>
    ServiceInfo = 250,

    /// <summary>Command Code (Sub-type 251, binary).</summary>
    CommandCode = 251,

    /// <summary>Control Info (Sub-type 252, string/text).</summary>
    ControlInfo = 252,

    /// <summary>Account Info (Sub-type 253, string/text).</summary>
    AccountInfo = 253,

    /// <summary>Service Name (Sub-type 254, string/text).</summary>
    ServiceName = 254,

    /// <summary>Multicast Info (Sub-type 255, string/text).</summary>
    MulticastInfo = 255
    
}