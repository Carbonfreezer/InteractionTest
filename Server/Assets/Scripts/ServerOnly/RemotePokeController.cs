using UnityEngine;

/// <summary>
/// The server side of the poke controller. Handles the click of the finger
/// with something for instance button.
/// </summary>
public class RemotePokeController : MonoBehaviour
{
    /// <summary>
    /// Delegate for being invoked when someone pokes on us.
    /// </summary>
    /// <param name="clientId">The client id of the object poking.</param>
    public delegate void Poke(ulong clientId);

    /// <summary>
    /// Gets called when someone pokes us.
    /// </summary>
    public event Poke Poking;

    /// <summary>
    /// Indicates if we are using an omni directional poker component.
    /// </summary>
    public bool IsOmni { get; set; }

    /// <summary>
    /// The camera we are using for poking.
    /// </summary>
    private Camera m_mainCamera;

    /// <summary>
    /// The collider of the object we trace against.
    /// </summary>
    private Collider m_collider;

    /// <summary>
    /// Stores the point when we have been poked last time success fully.
    /// </summary>
    private float m_lastTimePoked;

    /// <summary>
    /// Debouncing of poking system.
    /// </summary>
    private const float PokingDelayTime = 0.4f;

    public void Start()
    {
        m_mainCamera = Camera.main;
        m_collider = GetComponent<Collider>();
        RPCChanneler.Singleton.RegisterPoker(this);
    }

    /// <summary>
    /// In the update we deal with the local user.
    /// </summary>
    public void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Ray cameraRay = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (!Physics.Raycast(cameraRay, out hitInfo, 10.0f))
            return;
        if (hitInfo.collider != m_collider)
            return;

        RPCChanneler.Singleton.PokeInvocation(this);
    }

    public void PokeReception(ulong clientId)
    {
        if (Time.time < m_lastTimePoked + PokingDelayTime)
            return;

        Poking?.Invoke(clientId);
        m_lastTimePoked = Time.time;
    }
}