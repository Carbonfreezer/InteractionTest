using UnityEngine;

/// <summary>
/// Simple interface
/// a client side RPC to ping without creating a game object.
/// </summary>
public interface ITriggerable
{
    /// <summary>
    /// The method that gets invoked, when the trigger function should get activated.
    /// </summary>
    void Trigger();

    /// <summary>
    /// Returns the belonging game object, used for hash code implementation.
    /// </summary>
    GameObject InternalObject { get; }
}