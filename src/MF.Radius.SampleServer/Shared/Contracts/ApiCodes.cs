namespace MF.Radius.SampleServer.Shared.Contracts;

public static class ApiCodes
{
    public const string Ok = "OK";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotFound = "NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";

    public static class Subscribers
    {
        public const string Found = "SUBSCRIBER_FOUND";
        public const string NotFoundByUserName = "SUBSCRIBER_NOT_FOUND";
    }

    public static class Sessions
    {
        public const string ListReturned = "SESSIONS_RETURNED";
        public const string Found = "SESSION_FOUND";
        public const string NotFoundByNasAndId = "SESSION_NOT_FOUND";
        public const string InvalidNasIp = "INVALID_NAS_IP";
    }

    public static class Nas
    {
        public const string InvalidEndpoint = "NAS_INVALID_ENDPOINT";
        public const string InvalidInput = "NAS_COMMAND_INVALID_INPUT";
        public const string Success = "NAS_COMMAND_SUCCESS";
        public const string Rejected = "NAS_COMMAND_REJECTED";
        public const string Timeout = "NAS_COMMAND_TIMEOUT";
        public const string TransportFailure = "NAS_COMMAND_TRANSPORT_FAILURE";
    }
    
}