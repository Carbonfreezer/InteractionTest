using UnityEngine;

public class RemoteFocusInteractioController : MonoBehaviour
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
    /// Flags that we have currently focus.
    /// </summary>
    private bool m_hasFocus;

    /// <summary>
    /// The collider we use to trace against.
    /// </summary>
    private Collider m_collider;

    public void Start()
    {
        RPCChanneler.Singleton.RegisterFocus(this);
        m_hasFocus = false;
        m_collider = GetComponent<Collider>();
    }

    public void Update()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if ((Physics.Raycast(cameraRay, out RaycastHit hitInfo, 10.0f)) && (hitInfo.collider == m_collider))
        {
            if (Input.GetMouseButtonDown(0))
            {
                RPCChanneler.Singleton.FocusSelected(this);
                return;
            }

            if (!m_hasFocus)
            {
                m_hasFocus = true;
                RPCChanneler.Singleton.FocusStart(this);
            }
        }
        else
        {
            if (m_hasFocus)
            {
                m_hasFocus = false;
                RPCChanneler.Singleton.FocusEnd(this);
            }
        }
    }

    /// <summary>
    /// Gets called on the sever, when we got the focus acquired.
    /// </summary>
    /// <param name="clientId"></param>
    public void StartFocus(ulong clientId)
    {
        FocusAcquired?.Invoke(clientId);
    }

    /// <summary>
    /// Gets called on the server when the focus on the current object got lost.
    /// </summary>
    /// <param name="clientId"></param>
    public void EndFocus(ulong clientId)
    {
        FocusLost?.Invoke(clientId);
    }

    /// <summary>
    /// Gets called when the focus got selected.
    /// </summary>
    /// <param name="clientId"></param>
    public void SelectedFocus(ulong clientId)
    {
        FocusSelected?.Invoke(clientId);
    }
}