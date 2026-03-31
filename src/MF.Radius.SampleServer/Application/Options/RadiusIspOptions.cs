namespace MF.Radius.SampleServer.Application.Options;

public record RadiusIspOptions
{
    public TimeSpan NasTimeout { get; init; } = TimeSpan.FromSeconds(5);
    
    public uint FramedMtu { get; init; } = 1492;  // for PPPoE
    public uint SessionTimeout { get; init; } = 86400;  // 24 hours
    public uint IdleTimeout { get; init; } = 3600;  // 1 hour
    public uint AcctInterimInterval { get; init; } = 300;  // 5 minutes
    public string ReplyMessage { get; init; } = "Welcome to the MoRFaiR Network!";
    
}
