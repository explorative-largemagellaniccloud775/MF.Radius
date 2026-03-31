using MF.Radius.Core.Models;

namespace MF.Radius.Core.Packets.Parsing;

/// <summary>
/// Отвечает за десериализацию (из байтов в объект)
/// и сериализацию (из объекта в байты).
/// </summary>
public static class PacketParser
{

    public static RadiusPacket Parse(ReadOnlyMemory<byte> data)
    {
        return new RadiusPacket(data);
    }
    
}
