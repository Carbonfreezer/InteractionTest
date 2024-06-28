using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class RemotePhysicsController : MonoBehaviour, IMixedRealityPointerHandler
{
    /// <summary>
    /// The anchor transform we have.
    /// </summary>
    private Transform m_anchorTransform;

    /// <summary>
    /// Checks if we have found the anchor transform.
    /// </summary>
    private bool m_anchorFound;


    /// <summary>
    /// Transforms the given world coordinates into anchor coordinates.
    /// </summary>
    /// <param name="pointer">Sphere interaction pointer.</param>
    /// <param name="position">Position in anchor coordinates.</param>
    /// <param name="orientation">Orientaton in anchor coordinates.</param>
    private void TransformIntoAnchorCoordinates(SpherePointer pointer, out Vector3 position, out Quaternion orientation)
    {
        if (!m_anchorFound)
        {
            m_anchorTransform = Utility.GetAnchorTransform(transform);
            m_anchorFound = true;
        }

        TransformLight anchorTransformInverse = ((TransformLight)m_anchorTransform).Inverse();

        position = anchorTransformInverse.TransformPosition(pointer.Position);
        orientation = anchorTransformInverse.m_rotation * pointer.Rotation;
    }

    /// <summary>
    /// Get the interaction grabbable.
    /// </summary>
    public void Awake()
    {
        gameObject.AddComponent<NearInteractionGrabbable>();
    }

    /// <summary>
    /// Finds the anchor and starts the interaction controller.
    /// </summary>
    public void Start()
    {
        RPCChanneler.Singleton.RegisterPhysicsController(this);
    }

    #region Implementation of IMixedRealityPointerHandler

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        // We only want to interact with near.
        SpherePointer pointer = eventData.Pointer as SpherePointer;
        if (pointer == null)
            return;

        TransformIntoAnchorCoordinates(pointer, out Vector3 position, out Quaternion orientation);
        bool isLeft = pointer.Handedness == Handedness.Left;
        RPCChanneler.Singleton.StartPhysics(this, isLeft, position, orientation);
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // We only want to interact with near.
        SpherePointer pointer = eventData.Pointer as SpherePointer;
        if (pointer == null)
            return;

        bool isLeft = pointer.Handedness == Handedness.Left;
        TransformIntoAnchorCoordinates(pointer, out Vector3 position, out Quaternion orientation);
        RPCChanneler.Singleton.ProcessPhysics(this, isLeft, position, orientation);
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        SpherePointer pointer = eventData.Pointer as SpherePointer;
        if (pointer == null)
            return;
        bool isLeft = pointer.Handedness == Handedness.Left;

        RPCChanneler.Singleton.StopPhysics(this, isLeft);
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        // Nothing to do here.
    }

    #endregion

    #region dummy implementation

    /// <summary>
    /// Gets called when an outside entity want to acquire control of an object.
    /// </summary>
    /// <param name="client">Client Id of the client, that wants to acquire control.</param>
    /// <param name="isLeftHanded">Flags that we are processing with a left hand.</param>
    /// <param name="position">Interaction position in anchor coordinates. </param>
    /// <param name="rotation">Interaction orientation in anchor coordinates.</param>
    public void StartPhysicsReception(ulong client, bool isLeftHanded, Vector3 position, Quaternion rotation)
    {

    }

    /// <summary>
    /// Gets called, when a someone moves an acquired object.
    /// </summary>
    /// <param name="client">The client who moves the object.</param>
    /// <param name="isLeftHanded">Indicates if we are interacting with the left hand.</param>
    /// <param name="position">Interaction position in anchor coordinates. </param>
    /// <param name="rotation">Interaction orientation in anchor coordinates.</param>
    public void ProcessPhysicsReception(ulong client, bool isLeftHanded, Vector3 position, Quaternion rotation)
    {


    }


    /// <summary>
    /// Relinquishes the control of an owned object.
    /// </summary>
    /// <param name="client">The client that relinquishes control.</param>
    /// <param name="isLeftHanded">Flags that we are left handed.</param>
    public void StopPhysicsReception(ulong client, bool isLeftHanded)
    {

    }

    public void StartMission()
    {

    }

    public void StartDebrief()
    {

    }
    #endregion
}

