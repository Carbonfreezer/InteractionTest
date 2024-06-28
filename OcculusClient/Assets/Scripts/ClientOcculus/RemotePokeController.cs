using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

/// <summary>
/// The client side for being able to generate poking events.
/// </summary>
public class RemotePokeController : MonoBehaviour, IMixedRealityTouchHandler
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
    /// Indicates that the touch is currrently active.
    /// </summary>
    private bool m_touchActive;

    /// <summary>
    /// The position where the touch has started.
    /// </summary>
    private Vector3 m_touchStartPosition;



    public void Awake()
    {
        gameObject.AddComponent<NearInteractionTouchableVolume>();
    }

    public void Start()
    {
        RPCChanneler.Singleton.RegisterPoker(this);
        m_touchActive = false;
    }


    /// <summary>
    /// Dummy implementation.
    /// </summary>
    /// <param name="clientId"></param>
    public void PokeReception(ulong clientId)
    {

    }

    #region Implementation of IMixedRealityTouchHandler

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        if (IsOmni)
        {
            RPCChanneler.Singleton.PokeInvocation(this);
            return;
        }

        m_touchActive = true;
        m_touchStartPosition = eventData.InputData;
    }

    public void OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        if (m_touchActive)
            CheckMovement(eventData);

        m_touchActive = false;
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData)
    {
        if (!m_touchActive)
            return;

        // Have we moved far enough (3 mm)
        Vector3 delta = eventData.InputData - m_touchStartPosition;
        if (delta.magnitude < 0.003)
            return;

        m_touchActive = false;
        
        CheckMovement(eventData);
    }


    /// <summary>
    /// Does the handling if we are moving in the right direction and if the finger is pointing in the correct direction, 
    /// </summary>
    /// <param name="eventData"></param>
    private void CheckMovement(HandTrackingInputEventData eventData)
    {
        Vector3 delta = eventData.InputData - m_touchStartPosition; ;
        // Now see if we move the figer in the correct direction.
        Vector3 localDirection = transform.InverseTransformDirection(delta).normalized;

        bool movementDirectionOk = (localDirection.z < 0.0f);

        // Originally we checked also for finger direction. That was too unstable.  

        if (movementDirectionOk)
            RPCChanneler.Singleton.PokeInvocation(this);
    }

    #endregion
}
