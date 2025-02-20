using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Diagnostics;

public class HandScalerOnGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TouchHandGrabInteractor _handGrabInteractor;
    // e.g. “LeftHand/HandInteractorsLeft/OVRTouchHandGrabInteractor”

    [SerializeField]
    private HandVisual _handVisual;
    // The Oculus HandVisual that provides the virtual hand skeleton & mesh

    [Header("Default Settings")]
    [SerializeField]
    private float _defaultScaleRatio = 1f;
    // Movement scale ratio when NOT holding anything

    // Internal references
    private Transform _handRoot;                // The root transform of the virtual hand
    private IList<Transform> _fingerJoints;     // Finger bone transforms
    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlockEditor _materialEditor;
    private IHand _trackedHand;                 // The real (tracked) hand data

    // State for scaling
    private bool _isScaling = false;
    private float _currentScaleRatio = 1f;

    // Positions/rotations at the moment of grab
    private Vector3 _grabStartRealPos;          // Real hand position at grab time
    private Quaternion _grabStartRealRot;       // Real hand rotation at grab time

    private Vector3 _grabStartVirtualPos;       // Virtual hand position at grab time

    // The grabbed object and its transform at grab time
    private GameObject _grabbedObject = null;
    private Vector3 _grabbedObjectStartPos;
    private Quaternion _grabbedObjectStartRot;

    private bool _started = false;

    private void Awake()
    {
        // Subscribe to state changes on the grab interactor
        if (_handGrabInteractor != null)
        {
            _handGrabInteractor.WhenStateChanged += OnHandGrabStateChanged;
        }
        else
        {
            UnityEngine.Debug.LogError("[HandScalerOnGrab] _handGrabInteractor is not assigned!");
        }
    }

    private void OnDestroy()
    {
        if (_handGrabInteractor != null)
        {
            _handGrabInteractor.WhenStateChanged -= OnHandGrabStateChanged;
        }
    }

    private void Start()
    {
        // Gather references from the assigned HandVisual
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

        // Ensure the skinned mesh is visible
        _meshRenderer.enabled = true;

        _started = true;
    }

    /// <summary>
    /// Called when the interactor's state changes, e.g. from Hover → Select or Select → Unselect.
    /// </summary>
    private void OnHandGrabStateChanged(InteractorStateChangeArgs args)
    {
        // Transition INTO Select => user has grabbed an object
        if (args.NewState == InteractorState.Select)
        {
            var interactable = _handGrabInteractor.SelectedInteractable;
            if (interactable != null)
            {
                _grabbedObject = interactable.gameObject;

                // Try to read mass => scale ratio = 1/mass
                Rigidbody rb = _grabbedObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float mass = rb.mass;
                    _currentScaleRatio = 1f / mass; // e.g. mass=2 => ratio=0.5
                }
                else
                {
                    // If no rigidbody, fallback to default
                    _currentScaleRatio = _defaultScaleRatio;
                }

                _isScaling = true;

                // Record the real hand pose at grab
                if (_trackedHand.GetRootPose(out Pose realHandPose))
                {
                    _grabStartRealPos = realHandPose.position;
                    _grabStartRealRot = realHandPose.rotation;
                }
                else
                {
                    _grabStartRealPos = Vector3.zero;
                    _grabStartRealRot = Quaternion.identity;
                }

                // Record the virtual hand's position at grab
                _grabStartVirtualPos = _handRoot.position;

                // Record the grabbed object's position/rotation
                _grabbedObjectStartPos = _grabbedObject.transform.position;
                _grabbedObjectStartRot = _grabbedObject.transform.rotation;

                UnityEngine.Debug.Log($"[HandScalerOnGrab] Grabbed {_grabbedObject.name} => scaleRatio={_currentScaleRatio}");
            }
        }
        // Transition OUT of Select => user just released
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            // Stop scaling => revert to 1:1 movement for the hand
            _isScaling = false;
            _currentScaleRatio = _defaultScaleRatio;

            // We'll no longer update the object => it remains where it is
            _grabbedObject = null;

            UnityEngine.Debug.Log("[HandScalerOnGrab] Released object => scale off, returning 1:1 movement.");
        }
    }

    private void Update()
    {
        if (!_started) return;

        // If the hand isn't tracked or data is invalid, hide the mesh
        if (!_trackedHand.IsTrackedDataValid)
        {
            _meshRenderer.enabled = false;
            return;
        }
        else
        {
            _meshRenderer.enabled = true;
        }

        // Always read the current real hand pose
        if (_trackedHand.GetRootPose(out Pose currentRealPose))
        {
            // The virtual hand always uses 1:1 rotation
            _handRoot.rotation = currentRealPose.rotation;

            if (_isScaling)
            {
                // Movement offset from the real hand's grab position
                Vector3 realDelta = currentRealPose.position - _grabStartRealPos;
                Vector3 scaledOffset = realDelta * _currentScaleRatio;

                // Update the virtual hand’s position
                _handRoot.position = _grabStartVirtualPos + scaledOffset;

                // Also move the grabbed object by the same scaled offset
                if (_grabbedObject != null)
                {
                    // For rotation, we see how much the real hand rotated from grab-time
                    Quaternion realRotDelta = currentRealPose.rotation * Quaternion.Inverse(_grabStartRealRot);
                    // Apply that delta to the object's original rotation
                    Quaternion newObjRotation = realRotDelta * _grabbedObjectStartRot;

                    // For position, we do the same offset-based approach
                    Vector3 newObjPosition = _grabbedObjectStartPos + scaledOffset;

                    _grabbedObject.transform.SetPositionAndRotation(newObjPosition, newObjRotation);
                }
            }
            else
            {
                // Normal 1:1 movement for the virtual hand
                _handRoot.position = currentRealPose.position;
            }
        }

        // Update finger joints to match the moved _handRoot
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

        // If your shader supports a "_WristScale" or similar param, you can set it here
        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _currentScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }
}



/*
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Diagnostics;

public class HandScalerOnGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TouchHandGrabInteractor _handGrabInteractor;
    // e.g. “LeftHand/HandInteractorsLeft/OVRTouchHandGrabInteractor”

    [SerializeField]
    private HandVisual _handVisual;
    // The Oculus HandVisual (the same one your old MyHandVisual used)

    [Header("Default Settings")]
    [SerializeField]
    private float _defaultScaleRatio = 1f;
    // Movement scale ratio when NOT holding anything

    // Internal references
    private Transform _handRoot;           // The root transform of the virtual hand
    private IList<Transform> _fingerJoints;
    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlockEditor _materialEditor;
    private IHand _trackedHand;            // The real (tracked) hand data

    // State for scaling
    private bool _isScaling = false;
    private float _currentScaleRatio = 1f;

    // We record positions at the moment of grab
    private Vector3 _grabStartRealPos;
    private Vector3 _grabStartVirtualPos;

    // For tracking the grabbed object so we can re-parent it
    private GameObject _grabbedObject = null;

    private bool _started = false;

    private void Awake()
    {
        // Subscribe to state changes on the grab interactor
        if (_handGrabInteractor != null)
        {
            _handGrabInteractor.WhenStateChanged += OnHandGrabStateChanged;
        }
        else
        {
            UnityEngine.Debug.LogError("[HandScalerOnGrab] _handGrabInteractor is not assigned!");
        }
    }

    private void OnDestroy()
    {
        if (_handGrabInteractor != null)
        {
            _handGrabInteractor.WhenStateChanged -= OnHandGrabStateChanged;
        }
    }

    private void Start()
    {
        // Gather references from the assigned HandVisual
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

        // Ensure the skinned mesh is visible
        _meshRenderer.enabled = true;

        _started = true;
    }

    /// <summary>
    /// Called when the interactor's state changes, e.g. from Hover → Select or Select → Unselect.
    /// </summary>
    private void OnHandGrabStateChanged(InteractorStateChangeArgs args)
    {
        // Check for transition INTO Select => user has grabbed an object
        if (args.NewState == InteractorState.Select)
        {
            // Which object?
            var interactable = _handGrabInteractor.SelectedInteractable;
            if (interactable != null)
            {
                _grabbedObject = interactable.gameObject;

                // Try to read mass
                Rigidbody rb = _grabbedObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float mass = rb.mass;
                    _currentScaleRatio = 1f / mass; // e.g. mass=2 => ratio=0.5
                }
                else
                {
                    // If no rigidbody, fallback to default
                    _currentScaleRatio = _defaultScaleRatio;
                }

                _isScaling = true;

                // Record real hand position at moment of grab
                if (_trackedHand.GetRootPose(out Pose realHandPose))
                {
                    _grabStartRealPos = realHandPose.position;
                }
                else
                {
                    _grabStartRealPos = Vector3.zero;
                }

                // Record where the virtual hand was at this moment
                _grabStartVirtualPos = _handRoot.position;

                // Parent the grabbed object to the virtual hand so it follows the scaled movement
                _grabbedObject.transform.SetParent(_handRoot, true);

                UnityEngine.Debug.Log($"[HandScalerOnGrab] Grabbed {_grabbedObject.name} => scaleRatio={_currentScaleRatio}");
                UnityEngine.Debug.Log($"[HandScalerOnGrab] object has parent {_grabbedObject.transform.parent.name}");
            }
        }
        // Check for transition OUT of Select => user just released
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            // Stop scaling
            _isScaling = false;
            _currentScaleRatio = _defaultScaleRatio;

            // Un-parent the object so it stays where we leave it
            if (_grabbedObject != null)
            {
                _grabbedObject.transform.SetParent(null, true);
                _grabbedObject = null;
            }

            UnityEngine.Debug.Log("[HandScalerOnGrab] Released object => scale off, returning 1:1 movement.");
        }
    }

    private void Update()
    {
        if (!_started) return;

        // If the hand isn't tracked or data is invalid, hide the mesh
        if (!_trackedHand.IsTrackedDataValid)
        {
            _meshRenderer.enabled = false;
            return;
        }
        else
        {
            _meshRenderer.enabled = true;
        }

        // Always read the real hand's root pose
        if (_trackedHand.GetRootPose(out Pose currentRealPose))
        {
            // Rotation is always 1:1
            _handRoot.rotation = currentRealPose.rotation;

            if (_isScaling)
            {
                // Compute how far the real hand has moved from the grab start
                Vector3 realDelta = currentRealPose.position - _grabStartRealPos;

                // Scale that movement by (1/mass) or default if no RB
                Vector3 scaledOffset = realDelta * _currentScaleRatio;

                // Position = original position + scaled offset
                _handRoot.position = _grabStartVirtualPos + scaledOffset;
            }
            else
            {
                // Normal 1:1 movement
                _handRoot.position = currentRealPose.position;
            }
        }

        // Update the finger joints so the skeleton lines up with the new _handRoot
        if (_trackedHand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
        {
            for (int i = 0; i < _fingerJoints.Count; i++)
            {
                var jointTransform = _fingerJoints[i];
                if (jointTransform == null) continue;

                // Convert from local to world
                Pose localPose = localJoints[i];
                Vector3 worldPos = _handRoot.TransformPoint(localPose.position);
                Quaternion worldRot = _handRoot.rotation * localPose.rotation;

                jointTransform.SetPositionAndRotation(worldPos, worldRot);
            }
        }

        // If your shader supports a "_WristScale" or similar param, you can set it here
        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _currentScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }
}
*/