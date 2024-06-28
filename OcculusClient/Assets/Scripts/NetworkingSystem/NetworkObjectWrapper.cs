using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Simple class that just creates a patched network object.
/// if required.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkObjectWrapper : MonoBehaviour
{
    /// <summary>
    /// Creates a patched network component object.
    /// </summary>
    public void Awake()
    {
        Utility.PatchNetworkObject(gameObject);
    }
}