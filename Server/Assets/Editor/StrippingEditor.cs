using System;
using UnityEditor;
using UnityEngine;

public class StrippingEditor : Editor
{
    private static void CleanNotUsedComponents(GameObject obj)
    {
        // First we need to clear all scripts that do not belong to us.
        MonoBehaviour[] programmedBehaviour = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in programmedBehaviour)
        {
            Type behaviorType = behaviour.GetType();
            Attribute result = Attribute.GetCustomAttribute(behaviorType, typeof(StripAttribute));
            if (result != null)
                DestroyImmediate(behaviour);
        }

        // Remove all rigid bodies.
        Rigidbody[] rigidBodies = obj.GetComponents<Rigidbody>();
        foreach (Rigidbody body in rigidBodies)
        {
            DestroyImmediate(body);
        }

        // Remove all trigger colliders.
        Collider[] colliders = obj.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider.isTrigger)
            {
                DestroyImmediate(collider);
                continue;
            }

            MeshCollider meshCol = collider as MeshCollider;
            if (meshCol == null)
                continue;
            if (!meshCol.convex)
                DestroyImmediate(collider);
        }

        foreach (Transform child in obj.transform)
            CleanNotUsedComponents(child.gameObject);
    }

    [MenuItem("GameObject/PreparePrefab", false, 0)]
    public static void PreparePrefab(MenuCommand commnand)
    {
        GameObject selected = commnand.context as GameObject;
        GameObject duplicated = Instantiate(selected);

        // Do this a couple of time to break dependency chains.
        for (int i = 0; i < 10; ++i)
            CleanNotUsedComponents(duplicated);

        PrefabUtility.SaveAsPrefabAsset(duplicated, "Assets/Prefabs/Transfer/Transfer.prefab");
        DestroyImmediate(duplicated);
    }
}