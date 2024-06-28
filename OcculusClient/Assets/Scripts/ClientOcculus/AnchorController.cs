using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the anchor points in the QR System.
/// </summary>
public class AnchorController : MonoBehaviour
{


    [Tooltip("Contains the list of the transformations we want to influence.")]
    public List<Transform> m_listOfTransforms;

  

    /// <summary>
    /// Just dummy on the hololens
    /// </summary>
    public string m_lookAtPositioning;


    /// <summary>
    /// Just dummy on the hololens.
    /// </summary>
    public string m_namePositioning;


    #region dummy for interface purposes
    /// <summary>
    /// Delegate method to toggle calibarion.
    /// </summary>
    public delegate void ToggleCalibration();

    /// <summary>
    /// The event gets invoked when we have toggled calibration.
    /// </summary>
    public event ToggleCalibration CalibrationToggled;

    #endregion



    /// <summary>
    /// Provide a manipulator and a collider and move the object down.
    /// </summary>
    public void Start()
    {
        Utility.PatchAnchorAnnotations(m_listOfTransforms);
        foreach (Transform trans in m_listOfTransforms)
        {
            CapsuleCollider capsule = trans.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0.0f, 0.0f, 0.3f);
            capsule.radius = 0.1f;
            capsule.height = 0.7f;
            capsule.direction = 2;

            trans.gameObject.AddComponent<AnchorManipulator>();
        }

    }


    
    /// <summary>
    /// Updates the list of QR Codes if necesarry.
    /// </summary>
    public void Update()
    {
     
      

       

    }


    
   

}
