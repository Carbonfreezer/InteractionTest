using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class RemoteFocusInteractioController : MonoBehaviour, IMixedRealityFocusHandler, IMixedRealityPointerHandler, IMixedRealityTouchHandler
{
    /// <summary>
    /// The general focus function.
    /// </summary>
    public delegate void FocusFunction(ulong clientId);

    /// <summary>
    /// Gets called when we got focus.
    /// </summary>
    public event FocusFunction FocusAcquired;

    /// <summary>
    /// Gets called when we have lost focus.
    /// </summary>
    public event FocusFunction FocusLost;

    /// <summary>
    /// Gets invoked when the object got selected.
    /// </summary>
    public event FocusFunction FocusSelected;

    /// <summary>
    /// Flags that we currently have focus.
    /// </summary>
    private bool m_hasFocus;

    public void Awake()
    {
        gameObject.AddComponent<NearInteractionTouchableVolume>();
        m_hasFocus = false;
    }

    public void Start()
    {
        RPCChanneler.Singleton.RegisterFocus(this);
    }

    public void Update()
    {
    }

    #region Dummy implementation

    /// <summary>
    /// Gets called on the sever, when we got the focus acquired.
    /// </summary>
    /// <param name="clientId"></param>
    public void StartFocus(ulong clientId)
    {
        // Nothing to be done here.
    }

    /// <summary>
    /// Gets called on the server when the focus on the current object got lost.
    /// </summary>
    /// <param name="clientId"></param>
    public void EndFocus(ulong clientId)
    {
        // Nothing to be done here.
    }

    /// <summary>
    /// Gets called when the focus got selected.
    /// </summary>
    /// <param name="clientId"></param>
    public void SelectedFocus(ulong clientId)
    {
        // Nothing to be done here.
    }

    #endregion


    #region Implementation of IMixedRealityTouchHandler

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        if (!m_hasFocus)
            RPCChanneler.Singleton.FocusStart(this);
        RPCChanneler.Singleton.FocusSelected(this);
        m_hasFocus = true;
    }

    public void OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        if (m_hasFocus)
            RPCChanneler.Singleton.FocusEnd(this);
        m_hasFocus = false;
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData)
    {
        // Nothing to do here.
    }

    #endregion


    #region Implementation of IMixedRealityFocusHandler

    public void OnFocusEnter(FocusEventData eventData)
    {
        if (!m_hasFocus)
            RPCChanneler.Singleton.FocusStart(this);
        m_hasFocus = true;
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        if (m_hasFocus)
            RPCChanneler.Singleton.FocusEnd(this);
        m_hasFocus = false;
    }

    #endregion

    #region Implementation of IMixedRealityPointerHandler

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        // Nothing to be done here.
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // Nothing to be done here.
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        // Nothing to be done here.
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        if (m_hasFocus)
            RPCChanneler.Singleton.FocusSelected(this);
    }

    #endregion
}