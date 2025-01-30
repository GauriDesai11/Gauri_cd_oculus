using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;   
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ControllerData : MonoBehaviour
{
    [SerializeField] public XRNode controllerNode = XRNode.RightHand;
    [SerializeField] public Transform visualPrefab;
    [SerializeField] public Transform realPrefab;
    [SerializeField] public bool showReal;
    //[SerializeField] public bool showSliders;
    [SerializeField] public float grabRadius = 0.1f;
    [SerializeField] public LayerMask layerMask;
    [SerializeField] public XROrigin xrOrigin;
    [SerializeField] public float DC_ratio;
}
