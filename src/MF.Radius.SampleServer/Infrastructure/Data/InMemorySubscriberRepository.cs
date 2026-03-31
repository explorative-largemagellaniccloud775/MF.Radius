using System.Collections.Concurrent;
using System.Net;
using MF.Radius.SampleServer.Application.Features.Subscribers.Interfaces;
using MF.Radius.SampleServer.Domain.Entities;

namespace MF.Radius.SampleServer.Infrastructure.Data;

/// <summary>
/// A high-performance, thread-safe in-memory storage for subscriber profiles.
/// </summary>
/// <remarks>
/// This implementation serves as a primary data source for the sample server.
/// In production, this should be replaced by a repository that interacts with a persistent database.
/// </remarks>
public class InMemorySubscriberRepository
    : ISubscriberRepository
{
    private readonly ConcurrentDictionary<string, Subscriber> _subscribers = new()
    {
        ["test-subscriber"] = new Subscriber
        {
            UserName = "test-subscriber",
            StoredPassword = "Secret123",
            Status = SubscriberStatus.Active,
            StaticIp = IPAddress.Parse("100.64.0.101"),
            BaseRateLimit = 25_000,
        },
    };

    /// <summary>
    /// Initializes the repository and populates it with default test subscribers.
    /// </summary>
    public InMemorySubscriberRepository()
    {
        
    }

    /// <summary>
    /// Retrieves a subscriber by their username.
    /// </summary>
    /// <param name="userName">The identifier used in RADIUS Access-Request.</param>
    /// <returns>A <see cref="ValueTask"/> with the subscriber data or null.</returns>
    public ValueTask<Subscriber?> GetByUserNameAsync(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return ValueTask.FromResult<Subscriber?>(null);
        _subscribers.TryGetValue(userName, out var subscriber);
        return ValueTask.FromResult(subscriber);
    }

    /// <summary>
    /// Stores or updates a subscriber profile in the internal dictionary.
    /// </summary>
    /// <param name="subscriber">The subscriber entity to persist.</param>
    /// <remarks>
    /// Using 'Store' provides a clean, storage-agnostic API name.
    /// </remarks>
    public void Store(Subscriber subscriber)
    {
        if (subscriber == null)
            throw new ArgumentNullException(nameof(subscriber));
        _subscribers[subscriber.UserName] = subscriber;
    }
    
}
