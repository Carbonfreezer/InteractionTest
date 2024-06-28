using UnityEngine;

/// <summary>
/// Adds everything to the game object to be pokable. This involves the patched Network Object component,
/// the remote sphere pointer controller and network transformation in rigid mode.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RemotePokerComponent : MonoBehaviour
{
    /// <summary>
    /// The spaitial manipulation controller.
    /// </summary>
    private RemotePokeController m_pokeController;

    /// <summary>
    /// Get access for the spatial manipulation controller.
    /// </summary>
    public RemotePokeController Controller => m_pokeController;

    /// <summary>
    /// Constructs all the sub components.
    /// </summary>
    public void Awake()
    {
        m_pokeController = gameObject.AddComponent<RemotePokeController>();
    }
}