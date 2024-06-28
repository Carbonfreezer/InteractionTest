using UnityEngine;

/// <summary>
/// This is the server side of the interaction controller with the sphere pointer near interaction on the HoloLens.
/// </summary>
public class RemoteSpatialManipulationController : MonoBehaviour
{
    /// <summary>
    /// Callback if the interaction will be activated. Client is handed over and the server has to say yes or no
    /// and can therefore deny the client.
    /// </summary>
    /// <param name="clientId">The id of the client who wants to participate.</param>
    /// <returns>Accepting or not</returns>
    public delegate bool ActivationFunction(ulong clientId);

    /// <summary>
    /// The progress function where the interactor is described in the local coordinate system of the object under discussion.
    /// At the beginning it is in the origin and no rotation.
    /// </summary>
    /// <param name="position">Position in local coordinate system</param>
    /// <param name="orientation">Rotation in local coordinate system</param>
    public delegate void CoordinateFunction(Vector3 position, Quaternion orientation);

    /// <summary>
    /// Gets called when the interaction ends.
    /// </summary>
    public delegate void DeactivationFunction();

    /// <summary>
    /// This gets called when we have taken the controle.
    /// </summary>
    public event ActivationFunction ControlTaken;

    /// <summary>
    /// This gets called when we have set the coordinates.
    /// </summary>
    public event CoordinateFunction CoordinateSet;

    /// <summary>
    /// This gets called when we have released the controle.
    /// </summary>
    public event DeactivationFunction ControlReleased;

    /// <summary>
    /// Flags that we are currently active.
    /// </summary>
    private bool m_isActive;

    /// <summary>
    /// The client id we are currently handling with.
    /// </summary>
    private ulong m_clientId;

    /// <summary>
    /// The system for the interaction on the local server.
    /// </summary>
    private ServerInteractionController m_serverInteraction;

    /// <summary>
    /// The anchor transform we have.
    /// </summary>
    private Transform m_anchorTransform;

    /// <summary>
    /// The relative coordinate system of the object to be manipulated to the hand.
    /// </summary>
    private TransformLight m_offsetPose;

    /// <summary>
    /// Transforms the given anchor coordinates into world coordinates.
    /// </summary>
    /// <param name="position">Position to transform.</param>
    /// <param name="orientation">Orientation to transform.</param>
    private void GetIntoWorldCoordinates(ref Vector3 position, ref Quaternion orientation)
    {
        position = m_anchorTransform.TransformPoint(position);
        orientation = m_anchorTransform.rotation * orientation;
    }

    /// <summary>
    /// Finds the anchor and starts the interaction controller.
    /// </summary>
    public void Start()
    {
        // Search for anchor.
        m_anchorTransform = Utility.GetAnchorTransform(transform);

        m_serverInteraction = new ServerInteractionController(this, m_anchorTransform);
        RPCChanneler.Singleton.RegisterSpatialController(this);
    }

    public void OnDisable()
    {
        m_serverInteraction.Release();
    }

    /// <summary>
    /// This handles the local interaction.
    /// </summary>
    public void Update()
    {
        m_serverInteraction.Update();
    }

    /// <summary>
    /// Gets called when an outside entity want to acquire control of an object.
    /// </summary>
    /// <param name="client">Client Id of the client, that wants to acquire control.</param>
    /// <param name="position">Interaction position in anchor coordinates. </param>
    /// <param name="rotation">Interaction orientation in anchor coordinates.</param>
    public void StartInteractionReception(ulong client, Vector3 position, Quaternion rotation)
    {
        if (m_isActive || (ControlTaken == null))
            return;

        if (!ControlTaken.Invoke(client))
            return;

        m_isActive = true;
        m_clientId = client;

        GetIntoWorldCoordinates(ref position, ref rotation);

        TransformLight inversePointer = (new TransformLight(position, rotation)).Inverse();
        TransformLight objectPose = (TransformLight)gameObject.transform;

        m_offsetPose = inversePointer * objectPose;
    }

    /// <summary>
    /// Gets called, when a someone moves an acquired object.
    /// </summary>
    /// <param name="client">The client who moves the object.</param>
    /// <param name="position">The local position in relation to the belonging object where the interaction point is. </param>
    /// <param name="rotation">The local rotation in relation to the belonging object where the interaction point is.</param>
    public void ProcessInteractionReception(ulong client, Vector3 position, Quaternion rotation)
    {
        if ((!m_isActive) || (client != m_clientId))
            return;

        GetIntoWorldCoordinates(ref position, ref rotation);
        TransformLight finalPose = new TransformLight(position, rotation) * m_offsetPose;

        // Get the position relative to the parent.
        finalPose = ((TransformLight)gameObject.transform.parent).Inverse() * finalPose;

        CoordinateSet?.Invoke(finalPose.m_position, finalPose.m_rotation);
    }

    /// <summary>
    /// Reliquihes the control of an owned object.
    /// </summary>
    /// <param name="client">The client that relinquishes control.</param>
    public void StopInteractionReception(ulong client)
    {
        if ((!m_isActive) || (client != m_clientId))
            return;

        m_isActive = false;
        ControlReleased?.Invoke();
    }
}