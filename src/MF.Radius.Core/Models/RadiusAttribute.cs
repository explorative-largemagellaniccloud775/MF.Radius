using MF.Radius.Core.Enums;

namespace MF.Radius.Core.Models;

/// <summary>
/// Each attribute consists of Type, Length, and Value fields.
/// </summary>
public readonly ref struct RadiusAttribute
{
    
    /// <summary>
    /// It indicates the type of an attribute.
    /// The value ranges from 1 to 255.
    /// </summary>
    public required RadiusAttributeType Type { get; init; }
    
    /// <summary>
    ///  It indicates the length of an attribute
    /// (including the Type, Length, and Attribute fields).
    /// </summary>
    public required byte Length { get; init; }
    
    /// <summary>
    /// It indicates the attribute information.
    /// The format and content are dependent on the Type and Length fields.
    /// </summary>
    public required ReadOnlyMemory<byte> Value { get; init; }
    
}
