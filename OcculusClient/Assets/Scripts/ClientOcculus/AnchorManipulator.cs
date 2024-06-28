using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

/// <summary>
/// Class for manipulation of the anchor objects to place them at the correct position. We allow after first interaction for 40 seconds far interaction.
/// </summary>
public class AnchorManipulator : MonoBehaviour, IMixedRealityPointerHandler
{

    /// <summary>
    /// The relative coordinate system of the object to be manipulated to the hand.
    /// </summary>
    private TransformLight m_offsetPose;

    /// <summary>
    /// Inverse of start hand orientation.
    /// </summary>
    private Quaternion m_startOrientationInverse;

    /// <summary>
    /// Where we have started with the hand orientation.
    /// </summary>

    private Quaternion m_startOrientation;






    /// <summary>
    /// Get the interaction grabbable.
    /// </summary>
    public void Awake()
    {
        gameObject.AddComponent<NearInteractionGrabbable>();
    }

    #region Implementation of IMixedRealityPointerHandler

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
       
        IMixedRealityPointer pointer = eventData.Pointer;

        m_startOrientationInverse = Quaternion.Inverse(pointer.Rotation);
        m_startOrientation = pointer.Rotation;


        TransformLight inversePointer = (new TransformLight(pointer.Position, pointer.Rotation)).Inverse();
        TransformLight objectPose = (TransformLight)transform.parent;

        m_offsetPose = inversePointer * objectPose;

        

    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (m_offsetPose == null)
            return;

        IMixedRealityPointer pointer = eventData.Pointer;

        Quaternion deltaRotation = pointer.Rotation * m_startOrientationInverse;
        Vector3 euler = deltaRotation.eulerAngles;
        euler.x = euler.z = 0.0f;
        Quaternion finalRot = Quaternion.Euler(euler) * m_startOrientation;
        TransformLight finalPose = new TransformLight(pointer.Position, finalRot) * m_offsetPose;

        transform.parent.position = finalPose.m_position;
        transform.parent.rotation = finalPose.m_rotation;
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        // Fix  orientation.
        Vector3 worldOrient = transform.parent.eulerAngles;
        worldOrient.x = worldOrient.z = 0.0f;
        transform.parent.eulerAngles = worldOrient;

        Vector3 position = transform.parent.position;
        position.y = 0.0f;
        transform.parent.position = position;

        m_offsetPose = null;


    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
       
    }

    #endregion
}
