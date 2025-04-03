using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;

public class HandScalerOnGrab : MonoBehaviour
{
    /*
        This class scales the movement of the virtual hand and virtual cube when needed.
    */

    [Header("References")]
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private LayerMask grabbableLayer; // Define layer for cubes to be grabbed
    [SerializeField] private HandVisual _handVisual;

    [Header("Default Settings")]
    [SerializeField] private float _defaultScaleRatio = 1f;

    // Internal references
    private Transform _handRoot;
    private IList<Transform> _fingerJoints;
    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlockEditor _materialEditor;
    private IHand _trackedHand;

    // Scaling state
    private bool _isScaling = false;
    private float _currentScaleRatio = 1f;

    // Grabbed object tracking
    private GameObject _grabbedObject = null;
    private Rigidbody _grabbedRigidbody = null;

    private Vector3 _grabStartRealPos;
    private Vector3 _grabStartVirtualPos;
    private Vector3 _grabbedObjectStartPos;
    private Vector3 _objectOffsetFromHand;

    private bool _started = false;

    private void Start()
    {
        if (_handVisual == null)
        {
            UnityEngine.Debug.LogError("[HandScalerOnGrab] HandVisual is not assigned!");
            return;
        }

        _handRoot = _handVisual.Root; // The visuals of the virtual hand
        _fingerJoints = _handVisual.Joints;
        _meshRenderer = _handVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        _materialEditor = _handVisual.GetComponentInChildren<MaterialPropertyBlockEditor>();
        _trackedHand = _handVisual.Hand;

        if (_handRoot == null || _fingerJoints == null || _meshRenderer == null || _trackedHand == null)
        {
            UnityEngine.Debug.LogError("[HandScalerOnGrab] Missing references from HandVisual!");
            return;
        }

        _meshRenderer.enabled = true;
        _started = true;
    }

    private void Update()
    {
        if (!_started) return;

        bool isHandTracked = _trackedHand.IsTrackedDataValid;
        _meshRenderer.enabled = isHandTracked;

        // Decide whether to scale the virtual hand and object depending on the input from the 
        // controller
        bool gripPressed = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller);
        bool gripReleased = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller);

        if (gripPressed)
        {
            TryGrabClosestObject();
        }
        else if (gripReleased)
        {
            ReleaseObject();
        }

        if (_trackedHand.GetRootPose(out Pose currentRealPose))
        {
            _handRoot.rotation = currentRealPose.rotation;

            if (_isScaling && _grabbedObject != null)
            {
                UpdateVirtualHandAndObjectMovement();
            }
            else
            {
                _handRoot.position = currentRealPose.position;
            }
        }

        // Set finger joints correctly to match the read hand
        if (_trackedHand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
        {
            for (int i = 0; i < _fingerJoints.Count; i++)
            {
                var jointTransform = _fingerJoints[i];
                if (jointTransform == null) continue;

                Pose localPose = localJoints[i];

                Vector3 worldPos = _handRoot.TransformPoint(localPose.position);
                Quaternion worldRot = _handRoot.rotation * localPose.rotation;

                jointTransform.SetPositionAndRotation(worldPos, worldRot);
            }
        }

        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _currentScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }

    private void TryGrabClosestObject()
    {
        // All cubes are added to the correct layer which is specified in grabbableLayer
        Collider[] colliders = Physics.OverlapSphere(_handRoot.position, 0.1f, grabbableLayer);
        if (colliders.Length == 0) return;

        float minDistance = float.MaxValue;
        GameObject closestObject = null;
        Rigidbody closestRigidbody = null;

        foreach (Collider col in colliders)
        {
            float distance = Vector3.Distance(_handRoot.position, col.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestObject = col.gameObject;
                closestRigidbody = closestObject.GetComponent<Rigidbody>();
            }
        }

        if (closestObject == null) return;

        _grabbedObject = closestObject;
        _grabbedRigidbody = closestRigidbody;

        if (_grabbedRigidbody != null)
        {
            _grabbedRigidbody.isKinematic = true;
            _currentScaleRatio = _grabbedObject.GetComponent<ObjProperties>().CD_ratio;
        }
        else
        {
            _currentScaleRatio = _defaultScaleRatio;
        }

        _isScaling = true;

        // Save the initial positions of the real hand, virtual hand and the virtual object
        if (_trackedHand.GetRootPose(out Pose realHandPose))
        {
            _grabStartRealPos = realHandPose.position;
            _grabStartVirtualPos = _handRoot.position;
            _grabbedObjectStartPos = _grabbedObject.transform.position;
        }
    }

    private void UpdateVirtualHandAndObjectMovement()
    {   /* NOTE:
            Here, larger _currentScaleRatio results in a 'lighter' object when using pseudohaptics.
            This is because the object's movement is scaled down (ratio is <1) when the user is made 
            to think it's heavier. However, in the report (dissertation) explaining this system, larger
            ratio relates to a 'heavier' object. To replicate the results in the report:

            Vector3 scaledHandOffset = realHandDelta / _currentScaleRatio;

        */

        if (!_trackedHand.GetRootPose(out Pose currentRealPose)) return;

        /*
        - currentRealPose = current position of the real hand
        - _grabStartRealPos = position at which the hand grabbed the virtual object
        - _currentScaleRatio = C/D ratio assigned to the virtual cube being grabbed
        - _handRoot.position = updated position of the virtual hand (scaled)
        - _grabbedObjectStartPos = position of the virtual cube when it was grabbed
        - _grabbedObject.transform.position = updated position of the virtual object
        */

        Vector3 realHandDelta = currentRealPose.position - _grabStartRealPos;
        Vector3 scaledHandOffset = realHandDelta * _currentScaleRatio;

        _handRoot.position = _grabStartVirtualPos + scaledHandOffset;
        _grabbedObject.transform.position = _grabbedObjectStartPos + scaledHandOffset;
    }

    private void ReleaseObject()
    {
        if (_grabbedObject != null)
        {
            if (_grabbedRigidbody != null)
            {
                _grabbedRigidbody.isKinematic = false;
            }
        }

        _grabbedObject = null;
        _grabbedRigidbody = null;
        _isScaling = false;
        _currentScaleRatio = _defaultScaleRatio;
    }
}
