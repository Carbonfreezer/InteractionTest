using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A helper class to control the local interaction with the player on the server.
/// </summary>
public class ServerInteractionController
{
    /// <summary>
    /// The object we are a part of. 
    /// </summary>
    private GameObject m_belongingObject;

    /// <summary>
    /// The remote controller we work with.
    /// </summary>
    private RemoteSpatialManipulationController m_controller;





    /// <summary>
    /// The collider we work with.
    /// </summary>
    private readonly List<Collider> m_arrayOfColliders;

    #region camera related stuff.

    /// <summary>
    /// The main camera stored.
    /// </summary>
    private Camera m_mainCamera;

    /// <summary>
    /// The transofrmation of the camera.
    /// </summary>
    private Transform m_cameraTransform;




    #endregion

    #region interaction related stuff.

    /// <summary>
    /// Flags that the interaction was active.
    /// </summary>
    private bool m_wasInteracting;


    /// <summary>
    /// The ray distance used.
    /// </summary>
    private float m_rayDistance;


    /// <summary>
    /// The current view rotation angle we use.
    /// </summary>
    private float m_rotationAngle;

    /// <summary>
    /// Here we can store the transformlight as the anchor will not move.
    /// </summary>
    private TransformLight m_anchorTransformInverse;



    #endregion

    public ServerInteractionController(RemoteSpatialManipulationController controller, Transform anchor)
    {
        m_controller = controller;
        m_belongingObject = m_controller.gameObject;
        m_mainCamera = Camera.main;
        m_cameraTransform = m_mainCamera.gameObject.transform;
        m_arrayOfColliders = new List<Collider>(m_belongingObject.GetComponents<Collider>());
        m_wasInteracting = false;
        m_anchorTransformInverse = ((TransformLight)anchor).Inverse();

    }

    /// <summary>
    /// Helper method to get a look at transformation.
    /// </summary>
    /// <param name="worldPosition">Position to look at.</param>
    /// <param name="localZRotation">Rotation around the local z Axis = looking Axis aferwards.</param>
    /// <returns></returns>
    private TransformLight GetCameraTransformForPoint(Vector3 worldPosition, float localZRotation)
    {
        Vector3 delta = worldPosition - m_cameraTransform.position;
        delta = delta.normalized;


        float dot = Vector3.Dot(Vector3.forward, delta);

        Quaternion lookAt;
        if (Math.Abs(dot - (-1.0f)) < 0.000001f)
        {
            lookAt = new Quaternion(Vector3.up.x, Vector3.up.y, Vector3.up.z, 3.1415926535897932f);
        }
        else if (Math.Abs(dot - (1.0f)) < 0.000001f)
        {
            lookAt = Quaternion.identity;
        }
        else
        {
            float rotAngle = (float)Math.Acos(dot);
            Vector3 rotAxis = Vector3.Cross(Vector3.forward, delta);
            rotAxis = Vector3.Normalize(rotAxis);
            lookAt = Quaternion.AngleAxis(Mathf.Rad2Deg * rotAngle, rotAxis);
        }

        lookAt = lookAt * Quaternion.AngleAxis(localZRotation, Vector3.forward);
        return new TransformLight(worldPosition, lookAt);
    }

    public void Release()
    {
        if (m_wasInteracting)
        {
            RPCChanneler.Singleton.StopInteraction(m_controller);
            m_wasInteracting = false;
        }
    }


    /// <summary>
    /// Has to be called on frame intervals does the interaction with objects.
    /// </summary>
    public void Update()
    {

        if (Input.GetMouseButtonUp(0) && m_wasInteracting)
        {
            RPCChanneler.Singleton.StopInteraction(m_controller);
            m_wasInteracting = false;
        }

        if (!Input.GetMouseButton(0))
            return;

        // First we need to get the ray point and hit information.
        Ray cameraRay = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if ((!m_wasInteracting) && Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo;
            if (!Physics.Raycast(cameraRay, out hitInfo, 10.0f))
                return;

            if (!m_arrayOfColliders.Contains(hitInfo.collider))
                return;

            TransformLight pointerCoords = GetCameraTransformForPoint(hitInfo.point, 0.0f);
            pointerCoords = m_anchorTransformInverse * pointerCoords;

            // Start the interaction.
            RPCChanneler.Singleton.StartInteraction(m_controller, pointerCoords.m_position, pointerCoords.m_rotation);
            m_wasInteracting = true;
            m_rayDistance = hitInfo.distance;
            m_rotationAngle = 0.0f;
            return;
        }

        if (!m_wasInteracting)
            return;


        m_rayDistance += Input.GetAxis("Mouse ScrollWheel");

        // In this case we are interacting with the object.
        Vector3 desiredWorldPosition = cameraRay.GetPoint(m_rayDistance);
        TransformLight finalPose = GetCameraTransformForPoint(desiredWorldPosition, m_rotationAngle);

        // Get into anchor coordinatess.
        finalPose = m_anchorTransformInverse * finalPose;

        if (Input.GetKey(KeyCode.LeftArrow))
            m_rotationAngle += 90.0f * Time.deltaTime;

        if (Input.GetKey(KeyCode.RightArrow))
            m_rotationAngle -= 90.0f * Time.deltaTime;

        // Get the position relative to the parent.
        RPCChanneler.Singleton.ProcessInteraction(m_controller, finalPose.m_position, finalPose.m_rotation);
    }
}
