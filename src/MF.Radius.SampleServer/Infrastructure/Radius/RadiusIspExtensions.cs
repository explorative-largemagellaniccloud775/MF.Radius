using System.Net;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Packets.Builders;
using MF.Radius.SampleServer.Application.Options;

namespace MF.Radius.SampleServer.Infrastructure.Radius;

/// <summary>
/// Provides extension methods for adding custom ISP-related attributes to RADIUS packets.
/// </summary>
public static class RadiusIspExtensions
{
    
    /// <summary>
    /// Adds standard ISP session attributes (RFC 2865, 2866).
    /// Optimized for PPPoE/IPoE scenarios with Zero Allocation.
    /// </summary>
    public static void ApplyStandardIspAttributes(
        this ref RadiusPacketBuilder builder, 
        RadiusIspOptions options, 
        IPAddress? framedIpAddress
    )
    {
        var attrs = builder.GetAttributeBuilder();
        
        // Service-Type: Framed (2) tells NAS to encapsulate network traffic
        attrs.AddInt32(RadiusAttributeType.ServiceType, 2);  // Framed-User
        attrs.AddInt32(RadiusAttributeType.FramedProtocol, 1); // PPP
        attrs.AddInt32(RadiusAttributeType.FramedMtu, options.FramedMtu);
        
        // Network addressing
        if (framedIpAddress != null)
        {
            attrs.AddIpV4(RadiusAttributeType.FramedIpAddress, framedIpAddress);
            attrs.AddIpV4(RadiusAttributeType.FramedIpNetmask, IPAddress.Broadcast);
        }
        
        // Session management
        attrs.AddInt32(RadiusAttributeType.SessionTimeout, options.SessionTimeout);  
        attrs.AddInt32(RadiusAttributeType.IdleTimeout, options.IdleTimeout);  
        
        // Termination-Action: 1 (Re-authorize) - NAS will re-auth user instead of just kicking
        attrs.AddInt32(RadiusAttributeType.TerminationAction, 1); // RADIUS-Request
        
        // Accounting interval (Interim-Update) - 300s is the best practice for billing
        attrs.AddInt32(RadiusAttributeType.AcctInterimInterval, options.AcctInterimInterval);  
        
        // 
        attrs.AddText(RadiusAttributeType.ReplyMessage, options.ReplyMessage);
        
        // Apply attributes to the packet buffer
        builder.Apply(attrs);
        
    }

    public static void ApplyCiscoRateLimit(this ref RadiusPacketBuilder builder, uint rate)
    {
        // WARNING: Try don't stack multiple attributes in one builder.Apply(),
        // got bugs with accel-ppp.
        // CORRECT: One VSA container per attribute
        
        // Service-command
        var cmdBuilder = builder.GetCiscoAttributeBuilder();
        cmdBuilder.AddAvPair("subscriber:command", "account-logon");
        builder.Apply(cmdBuilder);
        
        // Rate-limit
        var rateBuilder = builder.GetCiscoAttributeBuilder();
        rateBuilder.AddAvPair("subscriber:rate-limit", rate.ToString());
        builder.Apply(rateBuilder);
        
        // Optional: add service-profile if needed
        // var profileBuilder = builder.GetCiscoAttributeBuilder();
        // profileBuilder.AddAvPair("subscriber:service-name", "internet");
        // builder.Apply(profileBuilder);
        
    }
    
    /// <summary>
    /// Restricts user access by applying a named ACL or Policy-Map.
    /// Includes User-Name for better session matching and logging on NAS.
    /// </summary>
    public static void ApplyCiscoRestriction(
        this ref RadiusPacketBuilder builder, 
        string sessionId, 
        string? userName, 
        string aclName
    )
    {
        builder.ApplySessionIdentification(sessionId, userName);
        
        var cmdBuilder = builder.GetCiscoAttributeBuilder();
        cmdBuilder.AddAvPair("subscriber:command", "account-logon");
        builder.Apply(cmdBuilder);

        var filterBuilder = builder.GetCiscoAttributeBuilder();
        filterBuilder.AddAvPair("subscriber:filter-spec", aclName);
        builder.Apply(filterBuilder);
        
    }
    
    /// <summary>
    /// Applies attributes for Disconnect-Request (DM).
    /// Adding User-Name increases compatibility with many NAS types (e.g., accel-ppp).
    /// </summary>
    public static void ApplyDisconnect(
        this ref RadiusPacketBuilder builder, 
        string sessionId, 
        string? userName = null
    )
    {
        builder.ApplySessionIdentification(sessionId, userName);
    }
    
    /// <summary>
    /// Adds mandatory attributes to identify a session in CoA or Disconnect-Request.
    /// </summary>
    private static void ApplySessionIdentification(this ref RadiusPacketBuilder builder, string sessionId, string? userName)
    {
        var attrs = builder.GetAttributeBuilder();
        attrs.AddText(RadiusAttributeType.AcctSessionId, sessionId);
        if (!string.IsNullOrEmpty(userName))
            attrs.AddText(RadiusAttributeType.UserName, userName);
        builder.Apply(attrs);
    }
    
}
