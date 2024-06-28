using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NavNodeEditor : Editor
{
    private const string NodePrefix = "Node_";

    [MenuItem("GameObject/Append Nav Node", false, 0)]
    public static void AppendNavNode(MenuCommand commnand)
    {
        GameObject selected = commnand.context as GameObject;
        Transform baseTrans = selected.transform;

        List<int> serialNumberList = new List<int>();
        for (int i = 0; i < baseTrans.childCount; ++i)
        {
            GameObject scan = baseTrans.GetChild(i).gameObject;
            if (!scan.name.StartsWith(NodePrefix))
                continue;

            int serialNumber = Int32.Parse(scan.name.Remove(0, NodePrefix.Length));
            serialNumberList.Add(serialNumber);
        }

        serialNumberList.Sort();
        int numElements = serialNumberList.Count;
        if (numElements < 2)
        {
            Debug.LogError("Minimum of two nodes must already exist.");
            return;
        }

        Vector3 lastPosition = baseTrans.GetChild(serialNumberList[numElements - 1]).position;
        Vector3 beforeLastPosition = baseTrans.GetChild(serialNumberList[numElements - 2]).position;

        GameObject newNode = new GameObject
        {
            name = NodePrefix + numElements,
            transform =
            {
                parent = baseTrans,
                position = lastPosition + (lastPosition - beforeLastPosition)
            }
        };
    }
}