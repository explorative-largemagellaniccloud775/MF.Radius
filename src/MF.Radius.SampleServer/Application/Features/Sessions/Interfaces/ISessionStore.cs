using System.Net;
using MF.Radius.SampleServer.Application.Models;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Interfaces;

/// <summary>
/// Defines an asynchronous contract for managing active RADIUS sessions.
/// Supports high-performance lookups using ValueTask to minimize allocations.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Persists a new session or updates an existing one.
    /// </summary>
    ValueTask SaveAsync(Session session);

    /// <summary>
    /// Retrieves all active sessions stored in the session store.
    /// </summary>
    /// <returns>A read-only list of all active sessions.</returns>
    ValueTask<IReadOnlyList<Session>> GetAllAsync();
    
    /// <summary>
    /// Retrieves a specific session by its unique NAS and Session-Id pair.
    /// </summary>
    /// <param name="nasEp">The endpoint of the NAS that owns the session.</param>
    /// <param name="sessionId">The Acct-Session-Id from the NAS.</param>
    ValueTask<Session?> GetAsync(IPEndPoint nasEp, string sessionId);

    /// <summary>
    /// Retrieves all active sessions belonging to a specific subscriber.
    /// Useful when a subscriber has multiple concurrent connections.
    /// </summary>
    ValueTask<IReadOnlyList<Session>> GetByUserNameAsync(string userName);

    /// <summary>
    /// Removes a session from the store.
    /// </summary>
    ValueTask RemoveAsync(EndPoint nasEp, string sessionId);
    
}
