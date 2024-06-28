using System.Collections.Generic;
using UnityEngine;

public class RemotePhysicsController : MonoBehaviour
{
    /// <summary>
    /// The spring constant used in the force model.
    /// </summary>
    private const float SpringConstant = 200.0f;

    /// <summary>
    /// The spacing we use for the spring attachment.
    /// </summary>
    private const float Spacing = 1.00f;

    /// <summary>
    /// The number of points we use for positional control.
    /// </summary>
    private const int ConnectionPoints = 4;

    /// <summary>
    /// The system for the interaction on the local server.
    /// </summary>
    private ServerInteractionControllerPhysics m_serverInteraction;

    /// <summary>
    /// The anchor transform we have.
    /// </summary>
    private Transform m_anchorTransform;

    /// <summary>
    /// Flags that we are running a mission.
    /// </summary>
    private bool m_isMissionRunning;

    /// <summary>
    /// The rigidbody we use for steering.
    /// </summary>
    private Rigidbody m_rigidBody;

    /// <summary>
    /// The coordinate systems of the grabble points given in object coordinates.
    /// </summary>
    private readonly Dictionary<ulong, TransformLight> m_grabblePoints = new Dictionary<ulong, TransformLight>();

    /// <summary>
    /// The destination where we should get pulled to.
    /// </summary>
    private readonly Vector3[] m_destinationForcePoints = new Vector3[ConnectionPoints];

    /// <summary>
    /// The points where the forces are attached.
    /// </summary>
    private readonly Vector3[] m_sourceForcePoints = new Vector3[ConnectionPoints];

    /// <summary>
    /// The processing list used for physics update.
    /// </summary>
    private Dictionary<ulong, TransformLight> m_grabblesToProcess = new Dictionary<ulong, TransformLight>();

    /// <summary>
    /// The distance we use for the grabble points in object space.
    /// </summary>
    private Vector3 m_spatialDistance;

    /// <summary>
    /// The rpc channeler
    /// </summary>
    private RPCChanneler m_rpc;

    /// <summary>
    /// The original drag we had in the rigid body.
    /// </summary>
    private float m_backupDrag;

    /// <summary>
    /// The original angular drag we had in the rigid body.
    /// </summary>
    private float m_angularBackupDrag;

    /// <summary>
    /// The value we use for critical damping as a drag.
    /// </summary>
    private float m_criticalDamping;


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
    /// Generates four grabble points for a given transformation.
    /// </summary>
    /// <param name="trans">The transformation used.</param>
    /// <param name="grabblePoints">The grabble points filled.</param>
    private void FillPoints(TransformLight trans, Vector3[] grabblePoints)
    {
        grabblePoints[0] = trans.TransformPosition(Vector3.zero);
        grabblePoints[1] = trans.TransformPosition(Vector3.up * m_spatialDistance.y);
        grabblePoints[2] = trans.TransformPosition(Vector3.forward * m_spatialDistance.z);
        grabblePoints[3] = trans.TransformPosition(Vector3.right * m_spatialDistance.x);
    }

    /// <summary>
    /// Finds the anchor and starts the interaction controller.
    /// </summary>
    public void Start()
    {
        m_rpc = RPCChanneler.Singleton;

        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        m_spatialDistance = filter.mesh.bounds.extents * Spacing;
        m_rigidBody = GetComponent<Rigidbody>();
        Debug.Assert(m_rigidBody != null, "We do not have a rigid body to excert physics control.");
        m_backupDrag = m_rigidBody.drag;
        m_angularBackupDrag = m_rigidBody.angularDrag;
        m_criticalDamping = 2.0f * Mathf.Sqrt(m_rigidBody.mass * SpringConstant);
      
        // Search for anchor.
        m_anchorTransform = Utility.GetAnchorTransform(transform);
        m_serverInteraction = new ServerInteractionControllerPhysics(this, m_anchorTransform);
        m_rpc.RegisterPhysicsController(this);
    }

    public void Update()
    {
        m_serverInteraction.Update();
    }

    /// <summary>
    /// Gets called when an outside entity want to acquire control of an object.
    /// </summary>
    /// <param name="client">Client Id of the client, that wants to acquire control.</param>
    /// <param name="isLeftHanded">Flags that we are processing with a left hand.</param>
    /// <param name="position">Interaction position in anchor coordinates. </param>
    /// <param name="rotation">Interaction orientation in anchor coordinates.</param>
    public void StartPhysicsReception(ulong client, bool isLeftHanded, Vector3 position, Quaternion rotation)
    {
        if (!m_isMissionRunning)
            return;

        GetIntoWorldCoordinates(ref position, ref rotation);
        TransformLight oldPosition = new TransformLight(m_rigidBody.position, m_rigidBody.rotation);

        TransformLight referencePoint = oldPosition.Inverse() * new TransformLight(position, rotation);
        ulong finalIndex = 2 * client + (isLeftHanded ? 1uL : 0);
        m_grabblePoints.Add(finalIndex, referencePoint);

        m_rigidBody.drag = m_criticalDamping;
        m_rigidBody.angularDrag = m_criticalDamping;
    }

    /// <summary>
    /// Gets called, when a someone moves an acquired object.
    /// </summary>
    /// <param name="client">The client who moves the object.</param>
    /// <param name="isLeftHanded">Indicates if we are interacting with the left hand.</param>
    /// <param name="position">Interaction position in anchor coordinates. </param>
    /// <param name="rotation">Interaction orientation in anchor coordinates.</param>
    public void ProcessPhysicsReception(ulong client, bool isLeftHanded, Vector3 position, Quaternion rotation)
    {
        if (!m_isMissionRunning)
            return;

        GetIntoWorldCoordinates(ref position, ref rotation);
        ulong finalIndex = 2 * client + (isLeftHanded ? 1uL : 0);
        m_grabblesToProcess[finalIndex] = new TransformLight(position, rotation);
    }

    public void FixedUpdate()
    {
        if (m_grabblesToProcess.Count == 0)
            return;

        TransformLight newCoord = new TransformLight(m_rigidBody.position, m_rigidBody.rotation);
        foreach (var values in m_grabblesToProcess)
        {
            TransformLight sourceTransform = newCoord * m_grabblePoints[values.Key];
            FillPoints(sourceTransform, m_sourceForcePoints);
            FillPoints(values.Value, m_destinationForcePoints);

            for (int i = 0; i < ConnectionPoints; ++i)
            {
                Vector3 springForce = (m_destinationForcePoints[i] - m_sourceForcePoints[i]) * SpringConstant;
                m_rigidBody.AddForceAtPosition(
                    springForce,
                    m_sourceForcePoints[i]);
            }
        }
    }

    /// <summary>
    /// Relinquishes the control of an owned object.
    /// </summary>
    /// <param name="client">The client that relinquishes control.</param>
    /// <param name="isLeftHanded">Flags that we are left handed.</param>
    public void StopPhysicsReception(ulong client, bool isLeftHanded)
    {
        if (!m_isMissionRunning)
            return;

        ulong finalIndex = 2 * client + (isLeftHanded ? 1uL : 0);
        m_grabblePoints.Remove(finalIndex);
        m_grabblesToProcess.Remove(finalIndex);

        if (m_grabblesToProcess.Count == 0)
        {
            m_rigidBody.drag = m_backupDrag;
            m_rigidBody.angularDrag = m_angularBackupDrag;
        }
    }

    public void StartMission()
    {
        m_isMissionRunning = true;
    }

    public void StartDebrief()
    {
        m_isMissionRunning = false;
        m_grabblePoints.Clear();
        m_grabblesToProcess.Clear();
    }
}