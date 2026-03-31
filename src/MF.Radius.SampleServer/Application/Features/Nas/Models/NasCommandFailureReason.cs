namespace MF.Radius.SampleServer.Application.Features.Nas.Models;

public enum NasCommandFailureReason
{
    None = 0,
    ValidationError,
    NasNotResponding,
    SharedSecretNotFound,
    TransportError,
    UnexpectedResponseCode,
}
