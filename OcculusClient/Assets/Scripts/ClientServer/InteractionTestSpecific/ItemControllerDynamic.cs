using SteeringLogging;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class ItemControllerDynamic : NetworkBehaviour, ILoggable, ISteerable
{
    /// <summary>
    /// The snychronization mechanism for the the network.
    /// </summary>
    private readonly SyncedCoordinateSystem m_coordinateSync = new SyncedCoordinateSystem();

    /// <summary>
    /// The rigid body we have on server.
    /// </summary>
    private Rigidbody m_rigidBody;

    /// <summary>
    /// The sound controller we use for playing sound effects.
    /// </summary>
    private RemoteSoundController m_audioPlayer;

    /// <summary>
    /// The position where we have started living.
    /// </summary>
    private TransformLight m_startPosition;

    /// <summary>
    /// Contains the information, if we are currently running a mission.
    /// </summary>
    private bool m_isMissionRunning;

    public void Awake()
    {
        m_coordinateSync.CurrentCoordinateSystem = transform;
        m_startPosition = (TransformLight)transform;

#pragma warning disable CS0162
        if (!Platform.IsHost)
            return;

        m_rigidBody = GetComponent<Rigidbody>();
        m_audioPlayer = GetComponent<RemoteSoundController>();

#pragma warning restore CS0162
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public void OnCollisionEnter(Collision collision)
    {
#pragma warning disable CS0162
        if (!Platform.IsHost)
            return;

        if (!m_isMissionRunning)
            return;

        if (collision.collider.isTrigger)
            return;

        float velocity = m_rigidBody.velocity.magnitude;
        if (velocity < 0.2f)
            return;

        float volume = Mathf.Clamp01(velocity * 0.15f);
        m_audioPlayer.Volume = volume;
        m_audioPlayer.Pitch = Random.value * 0.4f + 0.8f;
        m_audioPlayer.PlaySound();

#pragma warning restore CS0162
    }


    // Update is called once per frame
    void Update()
    {
        m_coordinateSync.SyncCoordinates();
    }

    #region Implementation of ILoggable

    public void Serialize(BinaryReader reader)
    {
        m_coordinateSync.Serialize(reader);
    }

    public void Serialize(BinaryWriter writer)
    {
        m_coordinateSync.Serialize(writer);
    }

    #endregion

    #region Implementation of ISteerable

    public void StartMission(bool requiresReset)
    {
        m_isMissionRunning = true;
        if (!requiresReset)
            return;
        m_rigidBody.isKinematic = false;
        m_rigidBody.position = m_startPosition.m_position;
        m_rigidBody.rotation = m_startPosition.m_rotation;
        m_rigidBody.angularVelocity = Vector3.zero;
        m_rigidBody.velocity = Vector3.zero;
    }

    public void StartDebrief()
    {
        m_rigidBody.isKinematic = true;
        m_isMissionRunning = false;
        transform.position = m_startPosition.m_position;
        transform.rotation = m_startPosition.m_rotation;
    }

    #endregion
}