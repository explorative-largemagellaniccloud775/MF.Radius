using System.Buffers.Binary;
using System.Net;
using System.Text;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;
using MF.Radius.Core.Models.Acct;

namespace MF.Radius.Core.Processors;

/// <summary>
/// Provides functionality to transform a raw <see cref="RadiusPacket"/>
/// into a specialized <see cref="RadiusAcctRequest"/>.
/// This processor handles attribute extraction using zero-copy slicing for accounting-specific fields.
/// </summary>
public static class RadiusAcctRequestProcessor
{
    
    /// <summary>Internal struct to collect data during attribute iteration without multiple refs.</summary>
    private ref struct Context
    {
        public ReadOnlyMemory<byte> UserName { get; set; }
        public ReadOnlyMemory<byte> AcctSessionId { get; set; }
        public uint StatusType { get; set; }
    }

    /// <summary>
    /// Processes the incoming RADIUS packet and returns a specialized accounting request object.
    /// </summary>
    /// <param name="packet">The raw RADIUS Accounting-Request packet.</param>
    /// <param name="remoteEndPoint">The remote endpoint where the packet originated.</param>
    /// <returns>A specialized instance of <see cref="RadiusAcctRequest"/>.</returns>
    public static RadiusAcctRequest Process(RadiusPacket packet, EndPoint remoteEndPoint)
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

                case RadiusAttributeType.AcctSessionId:
                    context.AcctSessionId = attr.Value;
                    break;

                case RadiusAttributeType.AcctStatusType:
                    if (attr.Value.Length >= 4)
                        context.StatusType = BinaryPrimitives.ReadUInt32BigEndian(attr.Value.Span);
                    break;
            }
        }

        // TODO: Encoding.UTF8.GetString - heap allocation!!! FIX!
        return new RadiusAcctRequest
        {
            UserName = context.UserName.IsEmpty 
                ? string.Empty 
                : Encoding.UTF8.GetString(context.UserName.Span),
                
            SessionId = context.AcctSessionId.IsEmpty
                ? string.Empty
                : Encoding.UTF8.GetString(context.AcctSessionId.Span),
            StatusType = (RadiusAcctStatusType)context.StatusType,
            RawPacket = packet,
            RemoteEndPoint = remoteEndPoint,
        };
        
    }
    
}