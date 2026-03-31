namespace MF.Radius.Core.Models;

public static class RadiusConstants
{
    public const int MaxPacketSize = 4096;
    public const int HeaderSize = 20;
    public const byte VendorSpecificType = 26;
    public const uint CiscoVendorId = 9;
    public const uint MicrosoftVendorId = 311;
    public const int VsaHeaderSize = 6; // Type(1) + Len(1) + VendorId(4)
}
