using System.Buffers.Binary;
using System.Text;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;

namespace MF.Radius.Core.Extensions;

/// <summary>
/// Helpers for extracting NAS rejection details from CoA/Disconnect NAK responses.
/// </summary>
public static class RadiusPacketNasExtensions
{
    
    /// <summary>
    /// Returns the best available rejection description from a NAK packet.
    /// Priority: Error-Cause textual payload (non-standard) -> Reply-Message -> Error-Cause enum value.
    /// </summary>
    public static string? GetNasErrorDescription(this RadiusPacket packet, out RadiusErrorCause? errorCause)
    {
        errorCause = null;
        string? errorCauseText = null;
        string? replyMessageText = null;

        foreach (var attr in packet.GetAttributes())
        {
            if (attr.Type == RadiusAttributeType.ErrorCause)
            {
                if (attr.Value.Length == 4)
                    errorCause = (RadiusErrorCause)BinaryPrimitives.ReadUInt32BigEndian(attr.Value.Span);
                else
                {
                    // Some NAS implementations send a textual Error-Cause payload.
                    errorCauseText ??= DecodeText(attr.Value.Span);
                }
                continue;
            }

            if (attr.Type == RadiusAttributeType.ReplyMessage)
                replyMessageText ??= DecodeText(attr.Value.Span);
            
        }

        return !string.IsNullOrWhiteSpace(errorCauseText)
            ? errorCauseText
            : !string.IsNullOrWhiteSpace(replyMessageText)
                ? replyMessageText
            : errorCause?.ToString();
        
    }

    private static string? DecodeText(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
            return null;

        var text = Encoding.UTF8.GetString(value).TrimEnd('\0').Trim();
        return !string.IsNullOrWhiteSpace(text) 
            ? text 
            : $"0x{Convert.ToHexString(value)}";
        
    }
    
}

