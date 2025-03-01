using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;

public class HandScalerOnGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private LayerMask grabbableLayer; // Define layer for grab objects
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
    private MyScaledTransformer _grabbedTransformer = null;

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

        _handRoot = _handVisual.Root;
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
        Collider[] colliders = Physics.OverlapSphere(_handRoot.position, 0.1f, grabbableLayer);
        if (colliders.Length == 0) return;

        float minDistance = float.MaxValue;
        GameObject closestObject = null;
        Rigidbody closestRigidbody = null;
        MyScaledTransformer closestTransformer = null;

        foreach (Collider col in colliders)
        {
            float distance = Vector3.Distance(_handRoot.position, col.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestObject = col.gameObject;
                closestRigidbody = closestObject.GetComponent<Rigidbody>();
                closestTransformer = closestObject.GetComponent<MyScaledTransformer>();
            }
        }

        if (closestObject == null) return;

        _grabbedObject = closestObject;
        _grabbedRigidbody = closestRigidbody;
        _grabbedTransformer = closestTransformer;

        if (_grabbedRigidbody != null)
        {
            _grabbedRigidbody.isKinematic = true;
            _currentScaleRatio = 2.0f - _grabbedObject.GetComponent<ObjProperties>().CD_ratio;

            //float mass = _grabbedRigidbody.mass > 0 ? _grabbedRigidbody.mass : 1f;
            //_currentScaleRatio = 1f / mass;
        }
        else
        {
            _currentScaleRatio = _defaultScaleRatio;
        }

        _isScaling = true;

        if (_trackedHand.GetRootPose(out Pose realHandPose))
        {
            _grabStartRealPos = realHandPose.position;
            _grabStartVirtualPos = _handRoot.position;
            _grabbedObjectStartPos = _grabbedObject.transform.position;

            // Calculate offset of object from virtual hand
            _objectOffsetFromHand = _grabbedObjectStartPos - _grabStartRealPos;
        }

        _grabbedTransformer?.AttachToVirtualHand(_handRoot);
    }

    private void UpdateVirtualHandAndObjectMovement()
    {
        if (!_trackedHand.GetRootPose(out Pose currentRealPose)) return;

        Vector3 realHandDelta = currentRealPose.position - _grabStartRealPos;
        Vector3 scaledHandOffset = realHandDelta * _currentScaleRatio;
        _handRoot.position = _grabStartVirtualPos + scaledHandOffset;

        // Move object relative to where it was originally held
        //Vector3 objectScaledOffset = _objectOffsetFromHand * _currentScaleRatio;
        //_grabbedObject.transform.position = _handRoot.position + objectScaledOffset;
        _grabbedObject.transform.position = _handRoot.position + _objectOffsetFromHand;

        //_grabbedObject.transform.rotation = _handRoot.rotation;
    }

    private void ReleaseObject()
    {
        if (_grabbedObject != null)
        {
            _grabbedTransformer?.DetachFromVirtualHand();

            if (_grabbedRigidbody != null)
            {
                _grabbedRigidbody.isKinematic = false;
            }
        }

        _grabbedObject = null;
        _grabbedRigidbody = null;
        _grabbedTransformer = null;
        _isScaling = false;
        _currentScaleRatio = _defaultScaleRatio;
    }
}



// SCALING OBJECT RELATIVE TO THE VIRTUAL HAND
/*
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;

public class HandScalerOnGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TouchHandGrabInteractor _handGrabInteractor;
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private LayerMask grabbableLayer; // Define layer for grab objects
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
    private MyScaledTransformer _grabbedTransformer = null;
    private Vector3 _grabStartRealPos;
    private Vector3 _grabStartVirtualPos;
    private Vector3 _objectGrabOffset; // Offset for keeping object in relative hand position

    private bool _started = false;

    private void Start()
    {
        if (_handVisual == null)
        {
            UnityEngine.Debug.LogError("[HandScalerOnGrab] HandVisual is not assigned!");
            return;
        }

        _handRoot = _handVisual.Root;
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

        // Ensure hand visibility when tracking
        bool isHandTracked = _trackedHand.IsTrackedDataValid;
        _meshRenderer.enabled = isHandTracked;

        // Get controller inputs
        bool gripPressed = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller);
        bool gripReleased = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller);

        if (gripPressed)
        {
            UnityEngine.Debug.Log("[HandScalerOnGrab] Attempting to grab an object...");
            TryGrabClosestObject();
        }
        else if (gripReleased)
        {
            UnityEngine.Debug.Log("[HandScalerOnGrab] Releasing object...");
            ReleaseObject();
        }

        if (_trackedHand.GetRootPose(out Pose currentRealPose))
        {
            _handRoot.rotation = currentRealPose.rotation;

            if (_isScaling && _grabbedObject != null)
            {
                UpdateVirtualHandMovement();
            }
            else
            {
                _handRoot.position = currentRealPose.position;
            }
        }

        if (_trackedHand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
        {
            for (int i = 0; i < _fingerJoints.Count; i++)
            {
                var jointTransform = _fingerJoints[i];
                if (jointTransform == null) continue;

                Pose localPose = localJoints[i];

                // Convert local joint data into world space under _handRoot
                Vector3 worldPos = _handRoot.TransformPoint(localPose.position);
                Quaternion worldRot = _handRoot.rotation * localPose.rotation;

                jointTransform.SetPositionAndRotation(worldPos, worldRot);
            }
        }

        // Update wrist scaling effect in shader
        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _currentScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }

    private void TryGrabClosestObject()
    {
        Collider[] colliders = Physics.OverlapSphere(_handRoot.position, 0.5f, grabbableLayer);
        if (colliders.Length == 0) return;

        // Find closest object
        float minDistance = float.MaxValue;
        GameObject closestObject = null;
        Rigidbody closestRigidbody = null;
        MyScaledTransformer closestTransformer = null;

        foreach (Collider col in colliders)
        {
            float distance = Vector3.Distance(_handRoot.position, col.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestObject = col.gameObject;
                closestRigidbody = closestObject.GetComponent<Rigidbody>();
                closestTransformer = closestObject.GetComponent<MyScaledTransformer>();
            }
        }

        if (closestObject == null) return;

        UnityEngine.Debug.Log($"[HandScalerOnGrab] Closest object found: {closestObject.name}");

        // Assign grabbed object
        _grabbedObject = closestObject;
        _grabbedRigidbody = closestRigidbody;
        _grabbedTransformer = closestTransformer;

        if (_grabbedRigidbody != null)
        {
            _grabbedRigidbody.isKinematic = true;
            float mass = _grabbedRigidbody.mass > 0 ? _grabbedRigidbody.mass : 1f;
            _currentScaleRatio = 1f / mass; // Scale based on mass
        }
        else
        {
            _currentScaleRatio = _defaultScaleRatio;
        }

        _isScaling = true;

        if (_trackedHand.GetRootPose(out Pose realHandPose))
        {
            _grabStartRealPos = realHandPose.position;
        }
        else
        {
            _grabStartRealPos = Vector3.zero;
        }

        _grabStartVirtualPos = _handRoot.position;

        // Compute offset to maintain relative position of the object in hand
        _objectGrabOffset = _grabbedObject.transform.position - _handRoot.position;

        _grabbedTransformer?.AttachToVirtualHand(_handRoot);

        UnityEngine.Debug.Log($"✅ Grabbed {_grabbedObject.name}, Scale Ratio: {_currentScaleRatio}");
    }

    private void UpdateVirtualHandMovement()
    {
        if (!_trackedHand.GetRootPose(out Pose currentRealPose)) return;

        // Compute scaled movement
        Vector3 realDelta = currentRealPose.position - _grabStartRealPos;
        Vector3 scaledOffset = realDelta * _currentScaleRatio;
        _handRoot.position = _grabStartVirtualPos + scaledOffset;

        // Ensure the object stays relative to the hand's original grab point
        if (_grabbedObject != null)
        {
            _grabbedObject.transform.position = _handRoot.position + _objectGrabOffset * _currentScaleRatio;
            _grabbedObject.transform.rotation = _handRoot.rotation;
        }

        UnityEngine.Debug.Log($"[HandScalerOnGrab] Virtual hand and object updated.");
    }

    private void ReleaseObject()
    {
        if (_grabbedObject != null)
        {
            _grabbedTransformer?.DetachFromVirtualHand();

            if (_grabbedRigidbody != null)
            {
                _grabbedRigidbody.isKinematic = false;
            }

            UnityEngine.Debug.Log($"❌ Released {_grabbedObject.name}");
        }

        _grabbedObject = null;
        _grabbedRigidbody = null;
        _grabbedTransformer = null;
        _isScaling = false;
        _currentScaleRatio = _defaultScaleRatio;
    }
}



/////////////////////
*/
