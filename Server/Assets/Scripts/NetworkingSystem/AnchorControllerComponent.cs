using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Simple creator class, that encapsulates the different anchor systems for the different platforms. 
/// </summary>
[RequireComponent(typeof(NetworkObjectWrapper))]
public class AnchorControllerComponent : NetworkBehaviour, IDisconnectable
{
    [Tooltip("The name of the node that is used for camera positioning.")]
    public string m_namePositioning;

    [Tooltip("The name of the node that is used for look at positioning.")]
    public string m_lookAtPositioning;

    [Tooltip("Contains the list of the transformations we want to influence.")]
    public List<Transform> m_listOfTransforms;

    /// <summary>
    /// Indicates that we are in anchoring mode.
    /// </summary>
    private NetworkVariable<bool> m_isAnchoring = new NetworkVariable<bool>();


    /// <summary>
    /// The interval we send positional updates from the client to the server.
    /// </summary>
    private const float IntervalPositionUpdate = 0.25f;

    /// <summary>
    /// The last time we have send a positional update to the server.
    /// </summary>
    private float m_lastTimePositionalUpdatesSend;

    /// <summary>
    /// Contains the player (camera positions) for a specific player in each of the anchor coordinates. 
    /// </summary>
    private readonly Dictionary<ulong, Vector3[]> m_localHeadPositions = new Dictionary<ulong, Vector3[]>();

    /// <summary>
    /// Contains the list of the head position (camera) in each of the anchor coordinates for one client.
    /// </summary>
    private Vector3[] m_localPositionsList;

    public void Awake()
    {
        AnchorController anchor = gameObject.AddComponent<AnchorController>();
        m_localPositionsList = new Vector3[m_listOfTransforms.Count];
        anchor.m_listOfTransforms = m_listOfTransforms;
        anchor.m_lookAtPositioning = m_lookAtPositioning;
        anchor.m_namePositioning = m_namePositioning;
    }

    /// <summary>
    /// Asks on the server for a specific client id and the anchor coordinate index to get the camera position
    /// in that index.
    /// </summary>
    /// <param name="clientId">The client we look for.</param>
    /// <param name="anchorCoordinate">The coordinate we look for.</param>
    /// <returns>The camera position for that specific client in anchor coordinates.</returns>
    public Vector3 GetAnchorPositionForCamera(ulong clientId, int anchorCoordinate)
    {
        Debug.Assert(Platform.IsHost, "Should only be called on host.");
        return m_localHeadPositions.ContainsKey(clientId) ? m_localHeadPositions[clientId][anchorCoordinate] : Vector3.zero ;
    }

    /// <summary>
    /// Gives the anchor index for a specific transform. -1 if not belonging to an anchor.
    /// </summary>
    /// <param name="probingPosition">The transformation to ask for.</param>
    /// <returns>The index of the anchor coordinates. Negative if not existent.</returns>
    public int GetBelongingAnchorPosition(Transform probingPosition)
    {
        Transform anchor = Utility.GetAnchorTransform(probingPosition);
        for (int i = 0; i < m_listOfTransforms.Count; ++i)
        {
            if (m_listOfTransforms[i].parent == anchor)
                return i;
        }

        Debug.Assert(false, "Anchor not found should not happen.");
        return -1;
    }

    /// <summary>
    /// Asks for a specific anchor transformation.
    /// </summary>
    /// <param name="anchorIndex">The index of the anchor transformation.</param>
    /// <returns>The transformation that belongs to the index.</returns>

    public Transform GetAnchorTransform(int anchorIndex)
    {
        return m_listOfTransforms[anchorIndex].parent;
    }

    public void Start()
    {
        // At the beginning we start with positioning active.
        if (Platform.IsHost)
        {
            m_isAnchoring.Value = true;
        }

        UpdateAnchoring();
    }

    public void Update()
    {
#pragma warning disable CS0162
        Vector3 worldCamPosition = Camera.main.transform.position;
        if (Platform.IsHost)
        {
            for (int i = 0; i < m_listOfTransforms.Count; ++i)
                m_localPositionsList[i] = m_listOfTransforms[i].parent.InverseTransformPoint(worldCamPosition);
            // We simply store our client Id.
            m_localHeadPositions[NetworkManager.LocalClientId] = m_localPositionsList;
            return;
        }

        m_lastTimePositionalUpdatesSend += Time.deltaTime;
        if (m_lastTimePositionalUpdatesSend <= IntervalPositionUpdate)
            return;

        m_lastTimePositionalUpdatesSend = 0.0f;
        for (int i = 0; i < m_listOfTransforms.Count; ++i)
            m_localPositionsList[i] = m_listOfTransforms[i].parent.InverseTransformPoint(worldCamPosition);


        if (NetworkObject.IsSpawned)
            UpdatePositionsServerRpc(m_localPositionsList);

#pragma warning restore CS0162
    }

    /// <summary>
    /// Gets invoked when the camera positions in the anchor coordinates have been updated.
    /// </summary>
    /// <param name="positionList">List of positions send.</param>
    /// <param name="serverRpcParams">Server RPC params.</param>
    [ServerRpc(RequireOwnership = false)]
    private void UpdatePositionsServerRpc(Vector3[] positionList, ServerRpcParams serverRpcParams = default)
    {
        m_localHeadPositions[serverRpcParams.Receive.SenderClientId] = positionList;
    }

    /// <summary>
    /// Property to set and get the locations of the transforms. This is used to save found positions.
    /// </summary>
    public List<TransformLight> AnchorPositions
    {
        get
        {
            List<TransformLight> result = new List<TransformLight>(m_listOfTransforms.Count);
            foreach (Transform element in m_listOfTransforms)
            {
                result.Add(new TransformLight(element.parent.position, element.parent.rotation));
            }

            return result;
        }

        set
        {
            for (int i = 0; i < m_listOfTransforms.Count; ++i)
            {
                m_listOfTransforms[i].parent.position = value[i].m_position;
                m_listOfTransforms[i].parent.rotation = value[i].m_rotation;
            }
        }
    }

    /// <summary>
    /// Sets the activation status.
    /// </summary>
    private void UpdateAnchoring()
    {
        bool isAnchoring = m_isAnchoring.Value;
        foreach (Transform trans in m_listOfTransforms)
        {
            trans.gameObject.SetActive(isAnchoring);
        }
    }

    #region Overrides of NetworkBehaviour

    public override void OnNetworkSpawn()
    {
        if (Platform.IsHost)
        {
            m_isAnchoring.Value = true;
            GetComponent<AnchorController>().CalibrationToggled += () =>
            {
                m_isAnchoring.Value = !m_isAnchoring.Value;
            };
        }

        m_isAnchoring.OnValueChanged += (value, newValue) => UpdateAnchoring();
        UpdateAnchoring();
        base.OnNetworkSpawn();
    }

    #endregion

    #region Implementation of IDisconnectable

    public void InformClientDisconnection(ulong clientId)
    {
        m_localHeadPositions.Remove(clientId);
    }

    #endregion
}