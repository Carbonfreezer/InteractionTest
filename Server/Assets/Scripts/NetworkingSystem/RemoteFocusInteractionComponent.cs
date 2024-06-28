using UnityEngine;

/// <summary>
/// General abstraction component for the remote gazer component.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RemoteFocusInteractionComponent : MonoBehaviour
{
    /// <summary>
    /// The spaitial manipulation controller.
    /// </summary>
    private RemoteFocusInteractioController m_focusController;

    /// <summary>
    /// Get access for the spatial manipulation controller.
    /// </summary>
    public RemoteFocusInteractioController Controller => m_focusController;

    /// <summary>
    /// Constructs all the sub components.
    /// </summary>
    public void Awake()
    {
        m_focusController = gameObject.AddComponent<RemoteFocusInteractioController>();
    }
}