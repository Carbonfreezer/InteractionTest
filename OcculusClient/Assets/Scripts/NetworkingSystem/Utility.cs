using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// General utility functions.
/// </summary>
public static class Utility
{
    /// <summary>
    /// Creates a network object component and patches it with the pathname.
    /// </summary>
    /// <param name="belonging">The object to add a network object component to,</param>
    /// <returns>The network component.</returns>
    public static NetworkObject PatchNetworkObject(GameObject belonging)
    {
        NetworkObject netObj = belonging.GetComponent<NetworkObject>();

        // Get the pathname.
        string pathname = "/" + belonging.name;
        Transform scanner = belonging.transform;
        while (scanner.parent != null)
        {
            scanner = scanner.parent;
            pathname = "/" + scanner.gameObject.name + pathname;
        }

        PatchNetworkObject(pathname, netObj);

        return netObj;
    }

    /// <summary>
    /// Returns a hash id for a given game object.
    /// </summary>
    /// <param name="probing">The object we want to get the hash from.</param>
    /// <returns>Hash id.</returns>
    public static uint GetHashId(GameObject probing)
    {
        // Get the pathname.
        string pathname = "/" + probing.name;
        Transform scanner = probing.transform;
        while (scanner.parent != null)
        {
            scanner = scanner.parent;
            pathname = "/" + scanner.gameObject.name + pathname;
        }

        uint hashId = (uint)(pathname.GetHashCode());

        return hashId;
    }

    /// <summary>
    /// Patches an existing network object component with a pathname.
    /// </summary>
    /// <param name="pathname">The pathname the hash should be calculated from.</param>
    /// <param name="netObj">The network object component to patch.</param>
    public static void PatchNetworkObject(string pathname, NetworkObject netObj)
    {
        uint hashId = (uint)(pathname.GetHashCode());

        FieldInfo prop = netObj.GetType()
            .GetField("GlobalObjectIdHash", BindingFlags.NonPublic | BindingFlags.Instance);
        Debug.Assert(prop != null, "GlobalObjectIdHash not found");
        prop.SetValue(netObj, hashId);
    }

    /// <summary>
    /// Gets the corresponding anchor of the transformation. The anchor node is always the second node in the hierarchy (below the transfer node). 
    /// </summary>
    /// <param name="currentNode"></param>
    /// <returns></returns>
    public static Transform GetAnchorTransform(Transform currentNode)
    {
        while (currentNode.parent.parent != null)
            currentNode = currentNode.parent;

        return currentNode;
    }

    /// <summary>
    /// Gets all components under the node of Transfer of a certain type.
    /// </summary>
    /// <typeparam name="TComponent">The type of componentswe are looking for.</typeparam>
    /// <returns>List of searched components.</returns>
    public static List<TComponent> GetAllComponents<TComponent>()
    {
        GameObject searchNode = GameObject.Find("/Transfer");
        return new List<TComponent>(searchNode.GetComponentsInChildren<TComponent>(true));
    }


    /// <summary>
    /// Puts in the annotations of the different anchors with serial numbers.
    /// </summary>
    /// <param name="listOfAnnotators">The list of the annotations to patch.</param>
    public static void PatchAnchorAnnotations(List<Transform> listOfAnnotators)
    {
        int counter = 1;
        foreach (Transform annotator in listOfAnnotators)
        {
            Transform element = annotator.Find("Sign/Canvas/Text (TMP)");
            element.gameObject.GetComponent<TextMeshProUGUI>().text = counter.ToString();
            counter++;
        }
    }
}