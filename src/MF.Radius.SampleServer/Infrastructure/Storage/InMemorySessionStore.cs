using System.Collections.Concurrent;
using System.Net;
using MF.Radius.SampleServer.Application.Features.Sessions.Interfaces;
using MF.Radius.SampleServer.Application.Models;

namespace MF.Radius.SampleServer.Infrastructure.Storage;

/// <summary>
/// High-performance, thread-safe in-memory implementation of <see cref="ISessionStore"/>.
/// Follows Redis-like patterns using string keys and secondary indexing.
/// </summary>
public class InMemorySessionStore
    : ISessionStore
{
    // Primary storage: Key is "session:{IP:Port}:{SessionId}"
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    // Secondary index: Key is "user:sessions:{UserName}", 
    // Value is a dictionary acting as a Set of primary session keys.
    // We use ConcurrentDictionary as a Thread-Safe Set.
    // The 'byte' value is a dummy 1-byte constant because we only care about the keys (Session IDs).
    // This provides O(1) lookup, addition, and removal across multiple threads.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userIndex = new();

    /// <inheritdoc />
    public ValueTask SaveAsync(Session session)
    {
        var sessionKey = GetSessionKey(session.NasEndPoint, session.SessionId);
        var userKey = GetUserIndexKey(session.UserName);

        // 1. Update primary record
        _sessions[sessionKey] = session;

        // 2. Update secondary index (SetAdd pattern)
        var userSessions = _userIndex.GetOrAdd(userKey, _ => new ConcurrentDictionary<string, byte>());
        userSessions.TryAdd(sessionKey, 0);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<Session>> GetAllAsync()
    {
        var result = _sessions.Values.ToList();
        return ValueTask.FromResult<IReadOnlyList<Session>>(result.AsReadOnly());
    }

    /// <inheritdoc />
    public ValueTask<Session?> GetAsync(IPEndPoint nasEp, string sessionId)
    {
        var key = GetSessionKey(nasEp, sessionId);
        _sessions.TryGetValue(key, out var session);
        return ValueTask.FromResult(session);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<Session>> GetByUserNameAsync(string userName)
    {
        var userKey = GetUserIndexKey(userName);
        
        if (!_userIndex.TryGetValue(userKey, out var sessionKeys))
            return ValueTask.FromResult<IReadOnlyList<Session>>([]);

        var result = new List<Session>();
        // Iterate over the keys in the user's "Set"
        foreach (var key in sessionKeys.Keys)
        {
            if (_sessions.TryGetValue(key, out var session))
                result.Add(session);
        }

        return ValueTask.FromResult<IReadOnlyList<Session>>(result.AsReadOnly());
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(EndPoint nasEp, string sessionId)
    {
        var sessionKey = GetSessionKey(nasEp, sessionId);

        // Remove from primary storage
        if (_sessions.TryRemove(sessionKey, out var session))
        {
            var userKey = GetUserIndexKey(session.UserName);
            if (_userIndex.TryGetValue(userKey, out var keys))
            {
                // Remove from secondary index
                keys.TryRemove(sessionKey, out _);
                
                // Optional: Cleanup the index entry itself if no sessions left for this user
                if (keys.IsEmpty)
                {
                    _userIndex.TryRemove(userKey, out _);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    private static string GetSessionKey(EndPoint nasEndPoint, string sessionId) 
        => $"session:{nasEndPoint}:{sessionId}";

    private static string GetUserIndexKey(string userName) 
        => $"user:sessions:{userName.ToLowerInvariant()}";
    
}
