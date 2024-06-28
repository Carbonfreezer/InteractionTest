using System.Collections.Generic;
using UIElements;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Helper system to avoid having to many objects with network capabilitty.
/// </summary>
[RequireComponent(typeof(NetworkObjectWrapper))]
public class RPCChanneler : NetworkBehaviour
{
    public static RPCChanneler Singleton { get; private set; }

    private Dictionary<uint, RemotePokeController> m_pokerControllerMap;
    private Dictionary<RemotePokeController, uint> m_pokerControllerReverse;
    private Dictionary<uint, RemoteSpatialManipulationController> m_spatialControllerMap;
    private Dictionary<RemoteSpatialManipulationController, uint> m_spatialControllerReverse;
    private Dictionary<uint, RemotePhysicsController> m_physicsControllerMap;
    private Dictionary<RemotePhysicsController, uint> m_physicsControllerReverse;
    private Dictionary<uint, ITriggerable> m_triggerMap;
    private Dictionary<ITriggerable, uint> m_triggerReverse;
    private Dictionary<uint, RemoteFocusInteractioController> m_focusControllerMap;
    private Dictionary<RemoteFocusInteractioController, uint> m_focusControllerReverse;
    private Dictionary<uint, LampController> m_lampControllerMap;
    private Dictionary<LampController, uint> m_lampControllerReverse;
    private List<IDisconnectable> m_disconnectionList;

    public void Awake()
    {
        Singleton = this;
        m_pokerControllerMap = new Dictionary<uint, RemotePokeController>();
        m_pokerControllerReverse = new Dictionary<RemotePokeController, uint>();
        m_spatialControllerMap = new Dictionary<uint, RemoteSpatialManipulationController>();
        m_spatialControllerReverse = new Dictionary<RemoteSpatialManipulationController, uint>();
        m_triggerMap = new Dictionary<uint, ITriggerable>();
        m_triggerReverse = new Dictionary<ITriggerable, uint>();
        m_focusControllerMap = new Dictionary<uint, RemoteFocusInteractioController>();
        m_focusControllerReverse = new Dictionary<RemoteFocusInteractioController, uint>();
        m_physicsControllerMap = new Dictionary<uint, RemotePhysicsController>();
        m_physicsControllerReverse = new Dictionary<RemotePhysicsController, uint>();
        m_lampControllerMap = new Dictionary<uint, LampController>();
        m_lampControllerReverse = new Dictionary<LampController, uint>();

        m_disconnectionList = Utility.GetAllComponents<IDisconnectable>();
    }

    /// <summary>
    /// Gets invoked when a client got disconnected, that the interaction with the client gets released.
    /// </summary>
    /// <param name="clientId">Id of the client to get released.</param>
    public void ReleaseInteractionOnDisconnect(ulong clientId)
    {
        foreach (IDisconnectable disconnectable in m_disconnectionList)
            disconnectable.InformClientDisconnection(clientId);
    }

    #region Poking related.

    public void RegisterPoker(RemotePokeController controller)
    {
        uint hash = Utility.GetHashId(controller.gameObject);
        Debug.Assert(!m_pokerControllerMap.ContainsKey(hash), "Hash clash in poker.");
        m_pokerControllerMap[hash] = controller;
        m_pokerControllerReverse[controller] = hash;
    }

    /// <summary>
    /// Gets called from the client side to trigger the poker.
    /// </summary>
    public void PokeInvocation(RemotePokeController controller)
    {
        uint hash = m_pokerControllerReverse[controller];
        PokeServerRpc(hash);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PokeServerRpc(uint hashCode, ServerRpcParams serverRpcParams = default)
    {
        RemotePokeController poker = m_pokerControllerMap[hashCode];
        poker.PokeReception(serverRpcParams.Receive.SenderClientId);
    }

    #endregion

    #region Spatial manipulation related.

    public void RegisterSpatialController(RemoteSpatialManipulationController controller)
    {
        uint hash = Utility.GetHashId(controller.gameObject);
        Debug.Assert(!m_spatialControllerMap.ContainsKey(hash), "Hash clash in spatial controller.");
        m_spatialControllerMap[hash] = controller;
        m_spatialControllerReverse[controller] = hash;
    }

    public void StartInteraction(RemoteSpatialManipulationController controller, Vector3 position, Quaternion rotation)
    {
        uint hash = m_spatialControllerReverse[controller];
        StartInteractionServerRpc(hash, position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartInteractionServerRpc(uint hash, Vector3 position, Quaternion rotation,
        ServerRpcParams serverRpcParams = default)
    {
        RemoteSpatialManipulationController controller = m_spatialControllerMap[hash];
        controller.StartInteractionReception(serverRpcParams.Receive.SenderClientId, position, rotation);
    }

    public void ProcessInteraction(RemoteSpatialManipulationController controller, Vector3 position,
        Quaternion rotation)
    {
        uint hash = m_spatialControllerReverse[controller];
        ProcessInteractionServerRpc(hash, position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ProcessInteractionServerRpc(uint hash, Vector3 position, Quaternion rotation,
        ServerRpcParams serverRpcParams = default)
    {
        RemoteSpatialManipulationController controller = m_spatialControllerMap[hash];
        controller.ProcessInteractionReception(serverRpcParams.Receive.SenderClientId, position, rotation);
    }

    public void StopInteraction(RemoteSpatialManipulationController controller)
    {
        uint hash = m_spatialControllerReverse[controller];
        StopInteractionServerRpc(hash);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopInteractionServerRpc(uint hash, ServerRpcParams serverRpcParams = default)
    {
        RemoteSpatialManipulationController controller = m_spatialControllerMap[hash];
        controller.StopInteractionReception(serverRpcParams.Receive.SenderClientId);
    }

    #endregion

    #region Physics interaction manipulation related.

    public void RegisterPhysicsController(RemotePhysicsController controller)
    {
        uint hash = Utility.GetHashId(controller.gameObject);
        Debug.Assert(!m_physicsControllerMap.ContainsKey(hash), "Hash clash in physics controller.");
        m_physicsControllerMap[hash] = controller;
        m_physicsControllerReverse[controller] = hash;
    }

    public void StartPhysics(RemotePhysicsController controller, bool isLeftHanded, Vector3 position,
        Quaternion rotation)
    {
        uint hash = m_physicsControllerReverse[controller];
        StartPhysicsServerRpc(hash, isLeftHanded, position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartPhysicsServerRpc(uint hash, bool isLeftHanded, Vector3 position, Quaternion rotation,
        ServerRpcParams serverRpcParams = default)
    {
        RemotePhysicsController controller = m_physicsControllerMap[hash];
        controller.StartPhysicsReception(serverRpcParams.Receive.SenderClientId, isLeftHanded, position, rotation);
    }

    public void ProcessPhysics(RemotePhysicsController controller, bool isLeftHanded, Vector3 position,
        Quaternion rotation)
    {
        uint hash = m_physicsControllerReverse[controller];
        ProcessPhysicsServerRpc(hash, isLeftHanded, position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ProcessPhysicsServerRpc(uint hash, bool isLeftHanded, Vector3 position, Quaternion rotation,
        ServerRpcParams serverRpcParams = default)
    {
        RemotePhysicsController controller = m_physicsControllerMap[hash];
        controller.ProcessPhysicsReception(serverRpcParams.Receive.SenderClientId, isLeftHanded, position, rotation);
    }

    public void StopPhysics(RemotePhysicsController controller, bool isLeftHanded)
    {
        uint hash = m_physicsControllerReverse[controller];
        StopPhysicsServerRpc(hash, isLeftHanded);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StopPhysicsServerRpc(uint hash, bool isLeftHanded,
        ServerRpcParams serverRpcParams = default)
    {
        RemotePhysicsController controller = m_physicsControllerMap[hash];
        controller.StopPhysicsReception(serverRpcParams.Receive.SenderClientId, isLeftHanded);
    }

    #endregion

    #region Focus related.

    /// <summary>
    /// Gets called to register a focal.
    /// </summary>
    /// <param name="focal">Gazer to register.</param>
    public void RegisterFocus(RemoteFocusInteractioController focal)
    {
        uint hash = Utility.GetHashId(focal.gameObject);
        Debug.Assert(!m_focusControllerMap.ContainsKey(hash), "Hash clash in focal.");
        m_focusControllerMap[hash] = focal;
        m_focusControllerReverse[focal] = hash;
    }

    /// <summary>
    /// Gets called from the client side to trigger the focal.
    /// </summary>
    /// <param name="focal">Gazer to toggle.</param>
    public void FocusStart(RemoteFocusInteractioController focal)
    {
        uint hash = m_focusControllerReverse[focal];
        FocusAcquireServerRpc(hash);
    }

    /// <summary>
    /// Gets called when the focus got lost.
    /// </summary>
    /// <param name="focal">focal object.</param>
    public void FocusEnd(RemoteFocusInteractioController focal)
    {
        uint hash = m_focusControllerReverse[focal];
        FocusLostServerRpc(hash);
    }

    /// <summary>
    /// Gets called when the object with focus got selected (clicked).
    /// </summary>
    /// <param name="focal">Controller used for selection.</param>
    public void FocusSelected(RemoteFocusInteractioController focal)
    {
        uint hash = m_focusControllerReverse[focal];
        FocusSelectedServerRpc(hash);
    }

    /// <summary>
    /// Entry point from client to server for 
    /// </summary>
    /// <param name="hashCode"></param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    public void FocusAcquireServerRpc(uint hashCode, ServerRpcParams serverRpcParams = default)
    {
        RemoteFocusInteractioController focus = m_focusControllerMap[hashCode];
        focus.StartFocus(serverRpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Entry point from client to server for 
    /// </summary>
    /// <param name="hashCode"></param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    public void FocusLostServerRpc(uint hashCode, ServerRpcParams serverRpcParams = default)
    {
        RemoteFocusInteractioController focus = m_focusControllerMap[hashCode];
        focus.EndFocus(serverRpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Entry point from client to server for 
    /// </summary>
    /// <param name="hashCode"></param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    public void FocusSelectedServerRpc(uint hashCode, ServerRpcParams serverRpcParams = default)
    {
        RemoteFocusInteractioController focus = m_focusControllerMap[hashCode];
        focus.SelectedFocus(serverRpcParams.Receive.SenderClientId);
    }

    #endregion

    #region Trigger related

    // The trigger is the only command here that goes from server to client.
    public void RegisterTrigger(ITriggerable trigger)
    {
        uint hash = Utility.GetHashId(trigger.InternalObject);
        Debug.Assert(!m_triggerMap.ContainsKey(hash), "Hash clash in trigger.");
        m_triggerMap[hash] = trigger;
        m_triggerReverse[trigger] = hash;
    }

    public void TriggerInvocation(ITriggerable trigger)
    {
        uint hash = m_triggerReverse[trigger];
        InvokeTriggerClientRPC(hash);
    }

    [ClientRpc]
    private void InvokeTriggerClientRPC(uint hashCode)
    {
        ITriggerable trigger = m_triggerMap[hashCode];
        trigger.Trigger();
    }

    #endregion

    #region All lamp related

    /// <summary>
    /// Method to register a lamp.
    /// </summary>
    /// <param name="lamp">Lamp to register.</param>
    public void RegisterLamp(LampController lamp)
    {
        uint hash = Utility.GetHashId(lamp.gameObject);
        Debug.Assert(!m_lampControllerMap.ContainsKey(hash), "Hash clash in lampController.");
        m_lampControllerMap[hash] = lamp;
        m_lampControllerReverse[lamp] = hash;
    }

    /// <summary>
    /// Gets called on the server to switch a lamp status for a specific client to the indicated value.
    /// </summary>
    /// <param name="lamp">Lamp controller that changes it.</param>
    /// <param name="isOn">Flag whether we go on.</param>
    /// <param name="clientId">The client id to witch it.</param>
    public void LampInvocation(LampController lamp, bool isOn, ulong clientId)
    {
        uint hash = m_lampControllerReverse[lamp];
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        InvokeLampClientRpc(hash, isOn, clientRpcParams);
    }

    /// <summary>
    /// Switches the lamp to off for all clients.
    /// </summary>
    /// <param name="lamp">Lamp to switch.</param>
    public void SwitchOffLampForAll(LampController lamp)
    {
        uint hash = m_lampControllerReverse[lamp];
        InvokeLampClientRpc(hash, false);
    }

    [ClientRpc]
    private void InvokeLampClientRpc(uint hash, bool isOn, ClientRpcParams clientRpcParams = default)
    {
        LampController controller = m_lampControllerMap[hash];
        controller.SwitchReception(isOn);
    }

    #endregion
}