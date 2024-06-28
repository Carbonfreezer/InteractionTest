using SteeringLogging;
using UnityEngine;

/// <summary>
/// Same as remote controller component but geared for physics interaction, where hand actually apply a force on the object.
/// </summary>
/// <seealso cref="RemoteControllerComponent"/>
[RequireComponent(typeof(Collider))]
public class RemotePhysicsControllerComponent : MonoBehaviour, ISteerable, IDisconnectable
{
    /// <summary>
    /// The physics controller we represent.
    /// </summary>
    private RemotePhysicsController m_controller;

    /// <summary>
    /// Gets the remote physics controller.
    /// </summary>
    public RemotePhysicsController Controller => m_controller;

    /// <summary>
    /// Simply add the controller.
    /// </summary>
    public void Awake()
    {
        m_controller = gameObject.AddComponent<RemotePhysicsController>();
    }

    #region Implementation of ISteerable

    public void StartMission(bool requiresReset)
    {
        m_controller.StartMission();
    }

    public void StartDebrief()
    {
        m_controller.StartDebrief();
    }

    #endregion

    #region Implementation of IDisconnectable

    public void InformClientDisconnection(ulong clientId)
    {
        m_controller.StopPhysicsReception(clientId, true);
        m_controller.StopPhysicsReception(clientId, false);
    }

    #endregion
}