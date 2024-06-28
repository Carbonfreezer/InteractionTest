using UnityEngine;

/// <summary>
/// Adds everything to the game object to get remotely controlled. This involves the patched Network Object component,
/// the remote sphere pointer controller and network transformation in rigid mode.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RemoteControllerComponent : MonoBehaviour, IDisconnectable
{
    /// <summary>
    /// The spaitial manipulation controller.
    /// </summary>
    private RemoteSpatialManipulationController m_pointerController;

    /// <summary>
    /// Get access for the spatial manipulation controller.
    /// </summary>
    public RemoteSpatialManipulationController Controller => m_pointerController;

    /// <summary>
    /// Constructs all the sub components.
    /// </summary>
    public void Awake()
    {
        m_pointerController = gameObject.AddComponent<RemoteSpatialManipulationController>();
    }

    #region Implementation of IDisconnectable

    public void InformClientDisconnection(ulong clientId)
    {
        m_pointerController.StopInteractionReception(clientId);
    }

    #endregion
}