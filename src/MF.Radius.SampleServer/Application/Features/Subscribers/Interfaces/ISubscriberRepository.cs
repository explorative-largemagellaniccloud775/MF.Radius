using MF.Radius.SampleServer.Domain.Entities;

namespace MF.Radius.SampleServer.Application.Features.Subscribers.Interfaces;

/// <summary>
/// Defines a contract for accessing subscriber identity and authorization data.
/// This repository is used to validate credentials and retrieve business-level settings.
/// </summary>
public interface ISubscriberRepository
{
    
    /// <summary>
    /// Retrieves a subscriber profile based on their unique RADIUS User-Name.
    /// </summary>
    /// <param name="userName">The User-Name (login) provided in the RADIUS packet.</param>
    /// <returns>
    /// A task representing the asynchronous operation. 
    /// The task result contains the <see cref="Subscriber"/> if found; otherwise, <see langword="null"/>.
    /// </returns>
    ValueTask<Subscriber?> GetByUserNameAsync(string userName);
    
}
