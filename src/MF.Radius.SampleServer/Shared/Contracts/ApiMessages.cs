namespace MF.Radius.SampleServer.Shared.Contracts;

public static class ApiMessages
{
    public const string InvalidNasIp = "Invalid NAS IP.";
    public const string InvalidNasEndpoint = "Invalid NAS endpoint. Provide a valid IP address and port in range 0..65535.";
    public const string SubscriberNotFound = "Subscriber not found.";
    public const string SessionNotFound = "Session not found.";
    public const string NasCommandRejected = "NAS rejected the command.";
    public const string NasCommandTimeout = "NAS did not respond within the configured timeout.";
    public const string NasCommandFailed = "NAS command failed.";
}
