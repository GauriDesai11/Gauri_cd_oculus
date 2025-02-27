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
    [SerializeField] private OVRInput.Controller controller;

    [SerializeField]
    private HandVisual _handVisual;

    [Header("Default Settings")]
    [SerializeField]
    private float _defaultScaleRatio = 1f;

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
    private Grabbable _grabbedGrabbable = null;  // Reference to the Grabbable script
    private Rigidbody _grabbedRigidbody = null;  // Rigidbody reference for physics
    private MyScaledTransformer _grabbedTransformer = null;
    private Vector3 _grabStartRealPos;
    private Vector3 _grabStartVirtualPos;
    //private Vector3 _grabbedObjectStartPos;
    //private Quaternion _grabbedObjectRotationOffset;

    private bool _started = false;

    private void Awake()
    {
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

        // Make sure the hand's skinned mesh is visible
        _meshRenderer.enabled = true;
        _started = true;
    }

    /// <summary>
    /// Called when the interactor's state changes, e.g. Hover → Select or Select → Unselect.
    /// </summary>
    private void OnHandGrabStateChanged(InteractorStateChangeArgs args)
    {
   
        // When user GRABS an object
        if (args.NewState == InteractorState.Select)
        {
            var interactable = _handGrabInteractor.SelectedInteractable;
            if (interactable != null)
            {

                _grabbedObject = interactable.gameObject;

                // Get Grabbable & Rigidbody
                _grabbedGrabbable = _grabbedObject.GetComponent<Grabbable>();
                _grabbedRigidbody = _grabbedObject.GetComponent<Rigidbody>();
                _grabbedTransformer = _grabbedObject.GetComponent<MyScaledTransformer>();

                if ((_grabbedTransformer != null) && (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger, controller)))
                {
                    _grabbedTransformer.controllerPressed();
                    UnityEngine.Debug.Log("[HandScalerOnGrab] called transformer pressed function");
                }

                if (_grabbedTransformer == null)
                {
                    UnityEngine.Debug.Log("[HandScalerOnGrab] no transformer");
                } else
                {
                    UnityEngine.Debug.Log("[HandScalerOnGrab] there is a transformer");
                }

                /*
                // If the object has a rigidbody, set it kinematic so we can manually move it
                if (_grabbedRigidbody != null)
                {
                    _grabbedRigidbody.isKinematic = true;
                    _grabbedRigidbody.linearVelocity = Vector3.zero;
                    _grabbedRigidbody.angularVelocity = Vector3.zero;
                }
                */

                // Determine the current scale ratio based on the object's mass
                float mass = (_grabbedRigidbody != null) ? _grabbedRigidbody.mass : 1f;
                _currentScaleRatio = (mass > 0f) ? 1f / mass : _defaultScaleRatio;

                _isScaling = true;

                // Store positions for offset calculations
                if (_trackedHand.GetRootPose(out Pose realHandPose))
                {
                    _grabStartRealPos = realHandPose.position;
                }
                else
                {
                    _grabStartRealPos = Vector3.zero;
                }

                _grabStartVirtualPos = _handRoot.position;
                //_grabbedObjectStartPos = _grabbedObject.transform.position;

                // Calculate rotation offset between the hand and grabbed object
                //_grabbedObjectRotationOffset =
                //    Quaternion.Inverse(_handRoot.rotation) * _grabbedObject.transform.rotation;

                UnityEngine.Debug.Log($"[HandScalerOnGrab] Grabbed {_grabbedObject.name}, ScaleRatio: {_currentScaleRatio}");
            }
        }
        // When user RELEASES the object
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            _isScaling = false;
            _currentScaleRatio = _defaultScaleRatio;

            if ((_grabbedTransformer != null) && (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger, controller)))
            {
                _grabbedTransformer.controllerReleased();
                UnityEngine.Debug.Log("[HandScalerOnGrab] called transformer release function");
            }
            if (_grabbedTransformer == null)
            {
                UnityEngine.Debug.Log("[HandScalerOnGrab] no transformer");
            }
            else
            {
                UnityEngine.Debug.Log("[HandScalerOnGrab] there is a transformer");
            }

            // Re-enable physics
            if (_grabbedRigidbody != null)
            {
                _grabbedRigidbody.isKinematic = false;
                _grabbedRigidbody = null;
            }

            _grabbedObject = null;
            _grabbedGrabbable = null;

            UnityEngine.Debug.Log("[HandScalerOnGrab] Released object, scaling off.");
        }
    }

    private void Update()
    {
        if ((OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger, controller)) || (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger, controller)))
            {

            UnityEngine.Debug.Log("[HandScalerOnGrab - update] controller works");
        }

        if ((_grabbedTransformer != null) && (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger, controller)))
        {
            _grabbedTransformer.controllerReleased();
            UnityEngine.Debug.Log("[HandScalerOnGrab - update] called transformer release function");
        }

        if ((_grabbedTransformer != null) && (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger, controller)))
        {
            _grabbedTransformer.controllerPressed();
            UnityEngine.Debug.Log("[HandScalerOnGrab - update] called transformer pressed function");
        }

        if (!_started) return;

        // Hide the hand mesh if tracking is lost
        if (!_trackedHand.IsTrackedDataValid)
        {
            _meshRenderer.enabled = false;
            return;
        }
        else
        {
            _meshRenderer.enabled = true;
        }

        // Get the real hand's current pose
        if (_trackedHand.GetRootPose(out Pose currentRealPose))
        {
            // Always match the real hand's rotation in full
            _handRoot.rotation = currentRealPose.rotation;

            if (_isScaling && _grabbedObject != null)
            {
                
                // Compute offset from the original grab position
                Vector3 realDelta = currentRealPose.position - _grabStartRealPos;
                Vector3 scaledOffset = realDelta * _currentScaleRatio;
                

                // Move virtual hand
                _handRoot.position = _grabStartVirtualPos + scaledOffset;

                /*
                // Move grabbed object with the same offset
                _grabbedObject.transform.position = _grabbedObjectStartPos + scaledOffset;

                // Align rotation
                _grabbedObject.transform.rotation = _handRoot.rotation * _grabbedObjectRotationOffset;
                */
            }
            else
            {
                // No scaling or no grabbed object → normal 1:1 movement
                _handRoot.position = currentRealPose.position;
            }
        }

        // Update finger joints to match the new hand root
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

        // Optionally adjust wrist scaling in a shader (if your material uses this property)
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

    [SerializeField]
    private HandVisual _handVisual;

    [Header("Default Settings")]
    [SerializeField]
    private float _defaultScaleRatio = 1f;

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
    private Grabbable _grabbedGrabbable = null;  // Reference to the Grabbable script
    private Vector3 _grabStartRealPos;
    private Vector3 _grabStartVirtualPos;
    private Vector3 _grabbedObjectStartPos;
    private Quaternion _grabbedObjectRotationOffset;

    private bool _started = false;

    private void Awake()
    {
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

    /// <summary>
    /// Called when the interactor's state changes, e.g. from Hover → Select or Select → Unselect.
    /// </summary>
    private void OnHandGrabStateChanged(InteractorStateChangeArgs args)
    {
        // When user GRABS an object
        if (args.NewState == InteractorState.Select)
        {
            var interactable = _handGrabInteractor.SelectedInteractable;
            if (interactable != null)
            {
                _grabbedObject = interactable.gameObject;

                // Try to access the Grabbable component
                _grabbedGrabbable = _grabbedObject.GetComponent<Grabbable>();
                if (_grabbedGrabbable != null)
                {
                    _grabbedGrabbable.enabled = false; // Disable Grabbable to stop default Oculus movement
                    //_grabbedGrabbable._throwWhenUnselected = false;
                }

                // Read mass to determine scale ratio
                Rigidbody rb = _grabbedObject.GetComponent<Rigidbody>();
                _currentScaleRatio = (rb != null) ? 1f / rb.mass : _defaultScaleRatio;

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
                _grabbedObjectStartPos = _grabbedObject.transform.position;
                //_grabbedObjectStartRot = _grabbedObject.transform.rotation;
                _grabbedObjectRotationOffset = Quaternion.Inverse(_handRoot.rotation) * _grabbedObject.transform.rotation;

                UnityEngine.Debug.Log($"[HandScalerOnGrab] Grabbed {_grabbedObject.name}, Scale Ratio: {_currentScaleRatio}");
            }
        }
        // When user RELEASES the object
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            _isScaling = false;
            _currentScaleRatio = _defaultScaleRatio;

            // Re-enable Grabbable so it can be grabbed again
            if (_grabbedGrabbable != null)
            {
                _grabbedGrabbable.enabled = true;
                //_grabbedGrabbable._throwWhenUnselected = false;
                _grabbedGrabbable = null;
            }

            _grabbedObject = null;

            UnityEngine.Debug.Log("[HandScalerOnGrab] Released object, scaling off.");
        }
    }

    private void Update()
    {
        if (!_started) return;

        // Hide mesh if tracking is lost
        if (!_trackedHand.IsTrackedDataValid)
        {
            _meshRenderer.enabled = false;
            return;
        }
        else
        {
            _meshRenderer.enabled = true;
        }

        // Get current real hand position
        if (_trackedHand.GetRootPose(out Pose currentRealPose))
        {
            _handRoot.rotation = currentRealPose.rotation; // Always 1:1 rotation

            if (_isScaling)
            {
                // Compute movement offset from real grab position
                Vector3 realDelta = currentRealPose.position - _grabStartRealPos;
                Vector3 scaledOffset = realDelta * _currentScaleRatio;

                // Move virtual hand
                _handRoot.position = _grabStartVirtualPos + scaledOffset;

                // Move grabbed object
                if (_grabbedObject != null)
                {
                    _grabbedObject.transform.position = _grabbedObjectStartPos + scaledOffset;
                    _grabbedObject.transform.rotation = _handRoot.rotation * _grabbedObjectRotationOffset;
                    //_grabbedObject.SetPositionAndRotation(_grabbedObjectStartPos + scaledOffset, _grabbedObject.transform.rotation);
                }
            }
            else
            {
                // Normal 1:1 movement
                _handRoot.position = currentRealPose.position;
            }
        }

        // Update finger joints to match new hand root
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

        // Optional: Adjust wrist scaling in shader
        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _currentScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }
}
*/