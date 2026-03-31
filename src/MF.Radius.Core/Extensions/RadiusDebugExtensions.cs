using System.Buffers.Binary;
using System.Net;
using System.Text;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;
using Microsoft.Extensions.Logging;

namespace MF.Radius.Core.Extensions;

/// <summary>
/// Provides advanced debugging visualization for RADIUS packets.
/// Interprets standard attributes, VSAs (Microsoft, Cisco), and complex protocols like MS-CHAP v2.
/// </summary>
public static class RadiusDebugExtensions
{
    private const string Indent = "      ";

    /// <summary>
    /// Performs a high-quality deep dive log of the RADIUS packet.
    /// This method is zero-cost if LogLevel.Debug is disabled.
    /// </summary>
    public static void LogPacketAttributes(this RadiusPacket packet, ILogger logger, EndPoint remoteEndPoint)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;

        var sb = new StringBuilder();
        sb.AppendLine(); 
    
        try 
        {
            sb.AppendLine($"{Indent}╔════════ RADIUS PACKET DEBUG [Id: {packet.Identifier}] ════════");
            sb.AppendLine($"{Indent}║ EndPoint:      {remoteEndPoint}");
            sb.AppendLine($"{Indent}║ Code:          {packet.Code} ({(int)packet.Code})");
            sb.AppendLine($"{Indent}║ Identifier:    {packet.Identifier}");
            sb.AppendLine($"{Indent}║ Length:        {packet.Length} bytes (Buffer: {packet.Raw.Length})");
            sb.AppendLine($"{Indent}║ Authenticator: 0x{Convert.ToHexString(packet.Authenticator.Span)}");
            sb.AppendLine($"{Indent}╟──────── Attributes ────────");

            foreach (var attr in packet.GetAttributes())
            {
                try 
                {
                    FormatAttribute(sb, attr, $"{Indent}║");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{Indent}║ [ERR] {attr.Type}: {ex.Message}");
                }
            }

            sb.AppendLine($"{Indent}╚══════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{Indent}║ [FATAL ERROR] {ex.Message}");
            sb.AppendLine($"{Indent}║ Raw HEX: {Convert.ToHexString(packet.Raw.Span)}");
            sb.AppendLine($"{Indent}╚══════════════════════════════════════════════");
        }

        logger.LogDebug("{RadiusDebugInfo}", sb.ToString());
    }

    private static void FormatAttribute(StringBuilder sb, RadiusAttribute attr, string prefix)
    {
        var typeId = (int)attr.Type;
        var value = attr.Value;
        var typeName = Enum.IsDefined(typeof(RadiusAttributeType), (byte)typeId) ? attr.Type.ToString() : "Unknown";

        if (typeId == 26 && value.Length >= 6)
        {
            FormatVsa(sb, value.Span, prefix);
        }
        else
        {
            var dataType = GetRadiusDataType(typeId, value.Span);
            string decodedValue = DecodeValue(typeId, value.Span);

            // Format: ID  Name [DataType] TotLen: X (Pay: Y) | Value
            sb.Append($"{prefix}   {typeId,-3} {typeName,-25} [{dataType,-12}] ");
            sb.Append($"TotLen: {attr.Length,-3} (Pay: {attr.Length - 2,-3}) | ");
            sb.AppendLine(decodedValue);
        }
    }

    private static void FormatVsa(StringBuilder sb, ReadOnlySpan<byte> vsaData, string prefix)
    {
        var vendorId = BinaryPrimitives.ReadUInt32BigEndian(vsaData[..4]);
        var vendorName = vendorId switch { 311 => "MS", 9 => "Cisco", _ => vendorId.ToString() };
    
        var subData = vsaData[4..];
        int offset = 0;
        bool first = true;

        while (offset + 2 <= subData.Length)
        {
            byte subType = subData[offset];
            byte subLen = subData[offset + 1];
        
            if (subLen < 2 || offset + subLen > subData.Length) break;

            var subValue = subData.Slice(offset + 2, subLen - 2);
            string subName = GetSubAttributeName(vendorId, subType, vendorName);
            string displayValue = GetSubAttributeValue(vendorId, subType, subValue);

            // Detailed tag for VSA: "Sub: TypeID"
            string subTypeTag = $"Sub-{subType}";

            string attrIdStr = first ? "26 " : "   ";
            sb.Append($"{prefix}   {attrIdStr,-3} {subName,-25} [{subTypeTag,-12}] ");
            sb.Append($"TotLen: {vsaData.Length,-3} SubLen: {subLen,-3} (Pay: {subLen - 2,-3}) | ");
            sb.AppendLine(displayValue);
        
            offset += subLen;
            first = false;
        }
    }

    private static RadiusDataType GetRadiusDataType(int type, ReadOnlySpan<byte> data)
    {
        return type switch
        {
            1 or 11 or 18 or 19 or 22 or 23 or 34 or 35 or 60 or 76 => RadiusDataType.Text,
            4 or 8 or 9 or 16 => RadiusDataType.IpV4Addr,
            6 or 7 or 61 or 40 => RadiusDataType.Enum,
            5 or 12 or 13 or 15 or 42 or 43 or 46 or 47 or 48 => RadiusDataType.Integer,
            2 or 3 or 80 => RadiusDataType.String,
            _ => IsPrintable(data) ? RadiusDataType.Text : RadiusDataType.String
        };
    }

    private static string DecodeValue(int type, ReadOnlySpan<byte> value)
    {
        return type switch
        {
            var t when (t is 1 or 4 or 8 or 9 or 16) && value.Length == 4 => new IPAddress(value).ToString(),
            6  => DecodeEnum<RadiusServiceType>(value),
            7  => DecodeEnum<RadiusFramedProtocol>(value),
            61 => DecodeEnum<RadiusNasPortType>(value),
            40 => DecodeEnum<RadiusAcctStatusType>(value),
            var t when (t is 5 or 12 or 13 or 15 or 42 or 43 or 46 or 47 or 48) && value.Length == 4 
                => BinaryPrimitives.ReadUInt32BigEndian(value).ToString(),
            2 or 3 or 80 => $"0x{Convert.ToHexString(value)}",
            _ => IsPrintable(value) ? Encoding.UTF8.GetString(value).TrimEnd('\0') : $"0x{Convert.ToHexString(value)}"
        };
    }

    private static string GetSubAttributeName(uint vendorId, byte subType, string vendorName)
    {
        string name = vendorId switch {
            311 => Enum.IsDefined(typeof(RadiusMsAttributeType), subType) ? ((RadiusMsAttributeType)subType).ToString() : "Unknown",
            9   => Enum.IsDefined(typeof(RadiusCiscoAttributeType), subType) ? ((RadiusCiscoAttributeType)subType).ToString() : "Unknown",
            _   => "SubAttr"
        };
        return $"[{vendorName}] {name}";
    }

    private static string GetSubAttributeValue(uint vendorId, byte subType, ReadOnlySpan<byte> data)
    {
        if (vendorId == 311)
        {
            if (subType == 25 && data.Length >= 50)
            {
                var peer = Convert.ToHexString(data.Slice(2, 16));
                var nt = Convert.ToHexString(data.Slice(26, 24));
                return $"ID:{data[0]}, Flags:{data[1]}, Peer:{peer}, NT:{nt}";
            }
            if (subType == 26) return $"\"{Encoding.ASCII.GetString(data).TrimEnd('\0')}\"";
            if (subType == 16 || subType == 17) return $"0x{Convert.ToHexString(data)}";
            if ((subType == 7 || subType == 8) && data.Length == 4) return BinaryPrimitives.ReadUInt32BigEndian(data).ToString();
        }

        if (IsPrintable(data)) return Encoding.UTF8.GetString(data).TrimEnd('\0');
        return $"0x{TruncateHex(data, 128)}";
    }

    private static string DecodeEnum<T>(ReadOnlySpan<byte> value) where T : struct, Enum
    {
        if (value.Length != 4) return $"0x{Convert.ToHexString(value)}";
        var val = BinaryPrimitives.ReadUInt32BigEndian(value);
        return Enum.IsDefined(typeof(T), val) ? $"{val} ({Enum.GetName(typeof(T), val)})" : val.ToString();
    }

    private static bool IsPrintable(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return false;
        foreach (var b in data) if (b < 32 || b > 126) return false;
        return true;
    }

    private static string TruncateHex(ReadOnlySpan<byte> data, int maxLen = 128)
    {
        if (data.IsEmpty) return string.Empty;
        var hex = Convert.ToHexString(data);
        return hex.Length <= maxLen ? hex : hex[..maxLen] + "...";
    }
}