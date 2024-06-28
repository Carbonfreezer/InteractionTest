using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The anchor controller on the server side is more about the camera control. With the numbers though it is possible to jump directly to the indicated anchor with a key. 
/// </summary>
public class AnchorController : MonoBehaviour
{
    [Tooltip("The name of the node that is used for camera positioning.")]
    public string m_namePositioning;

    [Tooltip("The name of the node that is used for look at positioning.")]
    public string m_lookAtPositioning;

    [Tooltip("Contains the list of the transformations we want to influence.")]
    public List<Transform> m_listOfTransforms;

    /// <summary>
    /// The main camera stored.
    /// </summary>
    private Camera m_mainCamera;

    /// <summary>
    /// The transofrmation of the camera.
    /// </summary>
    private Transform m_cameraTransform;

    /// <summary>
    /// The mose position from previous frame.
    /// </summary>
    private Vector3 m_oldMousePosition;

    /// <summary>
    /// Mouse ensitivity for rotation speed.
    /// </summary>
    private const float MouseSensitivity = 20.0f;

    /// <summary>
    /// Movement speed for camera control.
    /// </summary>
    private const float MovementSpeed = 0.3f;

    /// <summary>
    /// Delegate method to toggle calibarion.
    /// </summary>
    public delegate void ToggleCalibration();

    /// <summary>
    /// The event gets invoked when we have toggled calibration.
    /// </summary>
    public event ToggleCalibration CalibrationToggled;

    public void Start()
    {
        m_mainCamera = Camera.main;
        m_cameraTransform = m_mainCamera.gameObject.transform;
        Utility.PatchAnchorAnnotations(m_listOfTransforms);
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(1))
            m_oldMousePosition = Input.mousePosition;

        if (Input.GetKey(KeyCode.W))
            m_cameraTransform.position += m_cameraTransform.forward * Time.deltaTime * MovementSpeed;

        if (Input.GetKey(KeyCode.S))
            m_cameraTransform.position -= m_cameraTransform.forward * Time.deltaTime * MovementSpeed;

        if (Input.GetKey(KeyCode.A))
            m_cameraTransform.position -= m_cameraTransform.right * Time.deltaTime * MovementSpeed;

        if (Input.GetKey(KeyCode.D))
            m_cameraTransform.position += m_cameraTransform.right * Time.deltaTime * MovementSpeed;

        if (Input.GetKey(KeyCode.R))
            m_cameraTransform.position += Vector3.up * Time.deltaTime * MovementSpeed;

        if (Input.GetKey(KeyCode.F))
            m_cameraTransform.position -= Vector3.up * Time.deltaTime * MovementSpeed;

        if (Input.GetKeyDown(KeyCode.Space))
            CalibrationToggled?.Invoke();

        if (Input.GetMouseButton(1))
        {
            Vector3 mousePosition = Input.mousePosition;

            Vector3 delta = mousePosition - m_oldMousePosition;
            Vector3 angleDelta = new Vector3(-delta.y, delta.x, 0.0f) * Time.deltaTime * MouseSensitivity;
            m_cameraTransform.eulerAngles += angleDelta;

            m_oldMousePosition = mousePosition;
        }

        for (int i = 1; i <= 9; ++i)
            if (Input.GetKeyDown(KeyCode.Keypad0 + i) && (i <= m_listOfTransforms.Count))
                PositionCamera(m_listOfTransforms[i - 1]);
    }

    /// <summary>
    /// Positions the camera relatively to the look at point.
    /// </summary>
    /// <param name="actionStation">The transform of the action stattion we want to get to..</param>
    private void PositionCamera(Transform actionStation)
    {
        Vector3 postion = actionStation.parent.Find(m_namePositioning).position;
        Vector3 lookAt = actionStation.parent.Find(m_lookAtPositioning).position;
        m_cameraTransform.position = postion;
        m_cameraTransform.LookAt(lookAt);
    }
}