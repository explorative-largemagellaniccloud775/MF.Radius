using System.Buffers.Binary;
using System.Net;
using System.Text;
using MF.Radius.Core.Cryptography;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;
using MF.Radius.Core.Models.Auth;

namespace MF.Radius.Core.Processors;

/// <summary>
/// Provides functionality to transform a raw <see cref="RadiusPacket"/>
/// into a specialized <see cref="RadiusAuthRequestBase"/>.
/// This processor handles protocol detection, attribute extraction using zero-copy slicing, and PAP decryption.
/// </summary>
public static class RadiusAuthRequestProcessor
{

    /// <summary>Internal struct to collect data during attribute iteration without multiple refs.</summary>
    private ref struct Context
    {
        public ReadOnlyMemory<byte> UserName { get; set; }
        public ReadOnlyMemory<byte> UserPassword { get; set; }
        public ReadOnlyMemory<byte> ChapChallenge { get; set; }
        public ReadOnlyMemory<byte> ChapPassword { get; set; }
        public ReadOnlyMemory<byte> MsChapChallenge { get; set; }
        public ReadOnlyMemory<byte> MsChap2Response { get; set; }
    }

    /// <summary>
    /// Processes the incoming RADIUS packet and returns a specialized authentication request object.
    /// </summary>
    /// <param name="packet">The raw RADIUS Access-Request packet.</param>
    /// <param name="remoteEndPoint">The remote endpoint where the packet originated.</param>
    /// <param name="sharedSecret">The shared secret used for PAP password decryption.</param>
    /// <returns>A specialized instance of <see cref="RadiusAuthRequestBase"/> (Pap, Chap, or MsChapV2).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the User-Name attribute is missing.</exception>
    /// <exception cref="NotSupportedException">Thrown when the authentication protocol cannot be determined.</exception>
    public static RadiusAuthRequestBase Process(RadiusPacket packet, EndPoint remoteEndPoint, string sharedSecret)
    {
        var context = new Context();

        // One-pass attribute extraction (O(N))
        foreach (var attr in packet.GetAttributes())
        {
            switch (attr.Type)
            {
                case RadiusAttributeType.UserName:
                    context.UserName = attr.Value;
                    break;

                case RadiusAttributeType.UserPassword:
                    context.UserPassword = attr.Value;
                    break;

                case RadiusAttributeType.ChapPassword:
                    context.ChapPassword = attr.Value;
                    break;

                case RadiusAttributeType.ChapChallenge:
                    context.ChapChallenge = attr.Value;
                    break;

                case RadiusAttributeType.VendorSpecific:
                    ProcessVendorSpecific(attr.Value, ref context);
                    break;
            }
        }

        if (context.UserName.IsEmpty)
            throw new InvalidOperationException("User-Name attribute is missing in the RADIUS packet.");

        // TODO: heap allocation!!! FIX!
        var userName = Encoding.UTF8.GetString(context.UserName.Span);

        // Protocol Detection Logic (Prioritized)
        
        // 1. MS-CHAP v2
        if (!context.MsChap2Response.IsEmpty)
        {
            return new RadiusAuthMsChapV2Request
            {
                UserName = userName,
                RawPacket = packet,
                RemoteEndPoint = remoteEndPoint,
                AuthenticatorChallenge = context.MsChapChallenge.IsEmpty
                    ? packet.Authenticator
                    : context.MsChapChallenge,
                MsChap2Response = context.MsChap2Response,
            };
        }
        
        // 2. Standard CHAP
        if (!context.ChapPassword.IsEmpty)
        {
            return new RadiusAuthChapRequest
            {
                UserName = userName,
                RawPacket = packet,
                RemoteEndPoint = remoteEndPoint,
                ChapPassword = context.ChapPassword,
                // RFC 2865: Fallback to Request Authenticator if CHAP-Challenge is missing
                Challenge = !context.ChapChallenge.IsEmpty
                    ? context.ChapChallenge
                    : packet.Authenticator,
            };
        }

        // 3. PAP
        if (!context.UserPassword.IsEmpty)
        {
            return new RadiusAuthPapRequest
            {
                UserName = userName,
                RawPacket = packet,
                RemoteEndPoint = remoteEndPoint,
                Password = RadiusCrypto.DecryptPapPassword(
                    context.UserPassword.Span, 
                    packet.Authenticator.Span,
                    sharedSecret
                )
            };
        }

        throw new NotSupportedException("Unable to determine authentication protocol from packet attributes.");
        
    }

    /// <summary>
    /// Dispatches Vendor-Specific Attributes (VSA) to their respective vendor parsers.
    /// </summary>
    private static void ProcessVendorSpecific(ReadOnlyMemory<byte> vsaData, ref Context context)
    {
        var span = vsaData.Span;
        if (span.Length < 4) return;
        
        var vendor = (RadiusVendor)BinaryPrimitives.ReadUInt32BigEndian(span[..4]);

        switch (vendor)
        {
            case RadiusVendor.Microsoft:
                ParseMicrosoftAttributes(vsaData[4..], ref context);
                break;

            // In the future, you can easily add:
            // case RadiusVendor.Cisco:
            //     ParseCiscoAttributes(vsaData.Slice(4), ...);
            //     break;
            
        }
    }

    /// <summary>
    /// Parses Microsoft-specific sub-attributes for MS-CHAP v2.
    /// </summary>
    private static void ParseMicrosoftAttributes(ReadOnlyMemory<byte> data, ref Context context)
    {
        var offset = 0;
        while (offset + 2 <= data.Length)
        {
            var span = data.Span;
            var type = (RadiusMsAttributeType)span[offset];
            var len = span[offset + 1];

            if (len < 2 || offset + len > data.Length) break;

            var value = data.Slice(offset + 2, len - 2);

            switch (type)
            {
                case RadiusMsAttributeType.MsChapChallenge:
                    context.MsChapChallenge = value;
                    break;
                
                case RadiusMsAttributeType.MsChap2Response:
                    context.MsChap2Response = value;
                    break;
            }
            
            offset += len;
        }
    }
    
}
