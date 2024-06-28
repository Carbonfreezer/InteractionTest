using UnityEngine;

/// <summary>
/// Generates a Poker and Controller on the same time. In this case the poke controller must be omni directional.
/// </summary>
[RequireComponent(typeof(Collider))]
class RemotePokerControllerComponent : MonoBehaviour, IDisconnectable
{
    /// <summary>
    /// The spaitial manipulation controller.
    /// </summary>
    private RemotePokeController m_pokeController;

    /// <summary>
    /// Get access for the spatial manipulation controller.
    /// </summary>
    public RemotePokeController PokeController => m_pokeController;

    /// <summary>
    /// The spaitial manipulation controller.
    /// </summary>
    private RemoteSpatialManipulationController m_pointerController;

    /// <summary>
    /// Get access for the spatial manipulation controller.
    /// </summary>
    public RemoteSpatialManipulationController ManipulationController => m_pointerController;

    public void Awake()
    {
        m_pointerController = gameObject.AddComponent<RemoteSpatialManipulationController>();
        m_pokeController = gameObject.AddComponent<RemotePokeController>();
        m_pokeController.IsOmni = true;
    }

    #region Implementation of IDisconnectable

    public void InformClientDisconnection(ulong clientId)
    {
        m_pointerController.StopInteractionReception(clientId);
    }

    #endregion
}