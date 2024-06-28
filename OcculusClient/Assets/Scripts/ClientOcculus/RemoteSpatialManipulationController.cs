using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// This is the client side of the interaction controller with the sphere pointer for grabbing near interaction on the HoloLens.
/// </summary>
public class RemoteSpatialManipulationController : MonoBehaviour, IMixedRealityPointerHandler
{
    // !! The event part is just dummy to keep the interface consistent.

    /// <summary>
    /// Callback if the interaction will be activated. Client is handed over and the server has to say yes or no
    /// and can therefore deny the client.
    /// </summary>
    /// <param name="clientId">The id of the client who wants to participate.</param>
    /// <returns>Accepting or not</returns>
    public delegate bool ActivationFunction(ulong clientId);


    /// <summary>
    /// The progress function where the interactor is described in the local coordinate system of the object under discussion.
    /// At the beginning it is in the origin and no rotation.
    /// </summary>
    /// <param name="position">Position in local coordinate system</param>
    /// <param name="orientation">Rotation in local coordinate system</param>
    public delegate void CoordinateFunction(Vector3 position, Quaternion orientation);

    /// <summary>
    /// Gets called when the interaction ends.
    /// </summary>
    public delegate void DeactivationFunction();

    /// <summary>
    /// This gets called when we have taken the controle.
    /// </summary>
    public event ActivationFunction ControlTaken;

    /// <summary>
    /// This gets called when we have set the coordinates.
    /// </summary>
    public event CoordinateFunction CoordinateSet;

    /// <summary>
    /// This gets called when we have released the controle.
    /// </summary>
    public event DeactivationFunction ControlReleased;



    /// <summary>
    /// The handedness we use.
    /// </summary>
    private Handedness m_usedHandedness;

    /// <summary>
    /// Flags that we are currently active.
    /// </summary>
    private bool m_isActive;


    /// <summary>
    /// The anchor transform we have.
    /// </summary>
    private Transform m_anchorTransform;

    /// <summary>
    /// Checks if we have found the anchor transform.
    /// </summary>
    private bool m_anchorFound;

    /// <summary>
    /// Get the interaction grabbable.
    /// </summary>
    public void Awake()
    {
        gameObject.AddComponent<NearInteractionGrabbable>();
    }

    public void Start()
    {
        RPCChanneler.Singleton.RegisterSpatialController(this);
    }

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
            m_anchorTransform =  Utility.GetAnchorTransform(transform);
            m_anchorFound = true;
        }

        TransformLight anchorTransformInverse = ((TransformLight)m_anchorTransform).Inverse();

        position = anchorTransformInverse.TransformPosition(pointer.Position);
        orientation = anchorTransformInverse.m_rotation * pointer.Rotation;
    }

  

    #region Implementation of IMixedRealityPointerHandler


    /// <summary>
    /// Gets called when interaction started.,
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {

        if (m_isActive)
            return;

        // We only want to interact with near.
        SpherePointer pointer = eventData.Pointer as SpherePointer;
        if (pointer == null)
            return;


        TransformIntoAnchorCoordinates(pointer,  out Vector3 position, out Quaternion orientation);

        m_isActive = true;
        m_usedHandedness = pointer.Handedness;


        RPCChanneler.Singleton.StartInteraction(this, position, orientation);
    }

    /// <summary>
    /// Gets called, when closed grip gets dragged.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // We only want to interact with near.
        SpherePointer pointer = eventData.Pointer as SpherePointer;
        if (pointer == null)
            return;

        if (m_usedHandedness != pointer.Handedness)
            return;

        TransformIntoAnchorCoordinates(pointer, out Vector3 position, out Quaternion orientation);

        RPCChanneler.Singleton.ProcessInteraction(this, position, orientation);
      
    }

    /// <summary>
    /// Gets called when interaction ended.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        // We only want to interact with near.
        SpherePointer pointer = eventData.Pointer as SpherePointer;
        if (pointer == null)
            return;
        if (m_usedHandedness != pointer.Handedness)
            return;

        m_isActive = false;

        RPCChanneler.Singleton.StopInteraction(this);
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
      // Nothing to do here.
    }

    #endregion


    #region Dummy implementatios

    /// <summary>
    /// Gets called when an outside entity want to acquire control of an object.
    /// </summary>
    /// <param name="client">Client Id of the client, that wants to acquire control.</param>
    public void StartInteractionReception(ulong client, Vector3 position, Quaternion rotation)
    {

    }

    /// <summary>
    /// Gets called, when a someone moves an acquired object.
    /// </summary>
    /// <param name="client">The client who moves the object.</param>
    /// <param name="position">The local position in relation to the belonging object where the interaction point is. </param>
    /// <param name="rotation">The local rotation in relation to the belonging object where the interaction point is.</param>
    public void ProcessInteractionReception(ulong client, Vector3 position, Quaternion rotation)
    {

    }


    /// <summary>
    /// Reliquihes the control of an owned object.
    /// </summary>
    /// <param name="client">The client that relinquishes control.</param>
    public void StopInteractionReception(ulong client)
    {

    }

    #endregion
}
