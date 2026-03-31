namespace MF.Radius.SampleServer.Domain.Entities;

/// <summary>
/// Represents the possible lifecycle states of a network subscriber.
/// </summary>
public enum SubscriberStatus
{
    /// <summary>
    /// Fully authenticated and authorized with standard access.
    /// </summary>
    Active,

    /// <summary>
    /// Limited access (e.g., walled garden) due to low balance or other restrictions.
    /// </summary>
    Restricted,

    /// <summary>
    /// Access completely denied; session should be terminated.
    /// </summary>
    Disabled
    
}
