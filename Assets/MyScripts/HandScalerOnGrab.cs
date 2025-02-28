
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
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private LayerMask grabbableLayer; // Define layer for grab objects

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
               

                UnityEngine.Debug.Log($"[HandScalerOnGrab] Grabbed {_grabbedObject.name}, ScaleRatio: {_currentScaleRatio}");
            }
        }
        // When user RELEASES the object
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            _isScaling = false;
            _currentScaleRatio = _defaultScaleRatio;

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
        if ((OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller)) || (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller)))
            {

            UnityEngine.Debug.Log("[HandScalerOnGrab - update] controller works");
        }

        if ((_grabbedTransformer != null) && (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller)))
        {
            //_grabbedTransformer.controllerReleased();
            UnityEngine.Debug.Log("[HandScalerOnGrab - update] called transformer release function");
        }

        if ((_grabbedTransformer != null) && (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller)))
        {
            //_grabbedTransformer.controllerPressed();
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

        // Check for virtual hand grabbing using the primary Hand Trigger (right grip)
        bool gripPressed = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller);

        if (gripPressed && _grabbedObject == null)
        {
            UnityEngine.Debug.Log("[HandScalerOnGrab] controller pressed");
            TryGrabObject();
        }
        else if (!gripPressed && _grabbedObject != null)
        {
            UnityEngine.Debug.Log("[HandScalerOnGrab] controller released");
            //ReleaseObject();
        }

        // Update virtual hand position and movement
        if (_isScaling && _grabbedObject != null)
        {
            UnityEngine.Debug.Log("[HandScalerOnGrab] want to update virtual hand");

            //UpdateVirtualHandMovement();
        }

        // Optionally adjust wrist scaling in a shader (if your material uses this property)
        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _currentScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }

    private void TryGrabObject()
    {
        UnityEngine.Debug.Log("[HandScalerOnGrab] trying to grab an object");
        Collider[] colliders = Physics.OverlapSphere(_handRoot.position, 0.5f, grabbableLayer);

        if (colliders.Length > 0)
        {
            _grabbedObject = colliders[0].gameObject;
            _grabbedRigidbody = _grabbedObject.GetComponent<Rigidbody>();
            _grabbedTransformer = _grabbedObject.GetComponent<MyScaledTransformer>();

            if (_grabbedRigidbody != null)
            {
                _grabbedRigidbody.isKinematic = true; // Disable physics while holding
                float mass = _grabbedRigidbody.mass > 0 ? _grabbedRigidbody.mass : 1f;
                _currentScaleRatio = 1f / mass; // Scale hand movement based on object mass
            }
            else
            {
                _currentScaleRatio = _defaultScaleRatio;
            }

            //_grabbedObject.transform.SetParent(_handRoot);
            _isScaling = true;
            UnityEngine.Debug.Log("[HandScalerOnGrab] scaling set to true");

            if (_trackedHand.GetRootPose(out Pose realHandPose))
            {
                _grabStartRealPos = realHandPose.position;
                UnityEngine.Debug.Log("[HandScalerOnGrab] got real hand position");
            }
            else
            {
                _grabStartRealPos = Vector3.zero;
                UnityEngine.Debug.Log("[HandScalerOnGrab] could not find real hand pose");
            }

            _grabStartVirtualPos = _handRoot.position;

            if (_grabbedTransformer == null)
            {
                UnityEngine.Debug.Log("[HandScalerOnGrab]transformer is null");
            }
            _grabbedTransformer.AttachToVirtualHand(_handRoot);

            UnityEngine.Debug.Log($"✅ Grabbed {_grabbedObject.name}, Scale Ratio: {_currentScaleRatio}");
        }
        else
        {
            UnityEngine.Debug.Log("[HandScalerOnGrab] could not find an object");
        }
    }
}
*/


using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;

public class HandScalerOnGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TouchHandGrabInteractor _handGrabInteractor;
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private LayerMask grabbableLayer; // Define layer for grab objects

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

    private bool _started = false;
    private bool _touchHandGrab = false;
    private bool _touchHandRelease = false;

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

                _touchHandGrab = true;

                
                // Determine the current scale ratio based on the object's mass
                float mass = (_grabbedRigidbody != null) ? _grabbedRigidbody.mass : 1f;
                _currentScaleRatio = (mass > 0f) ? 1f / mass : _defaultScaleRatio;

                //_isScaling = true;

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


                UnityEngine.Debug.Log($"[HandScalerOnGrab] ready to grab {_grabbedObject.name}, ScaleRatio: {_currentScaleRatio}");
                
            }
        }
        // When user RELEASES the object
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            _touchHandRelease = true;

            UnityEngine.Debug.Log($"[HandScalerOnGrab] ready to release");

            /*
            _isScaling = false;
            _currentScaleRatio = _defaultScaleRatio;

            // Re-enable physics
            if (_grabbedRigidbody != null)
            {
                _grabbedRigidbody.isKinematic = false;
                _grabbedRigidbody = null;
            }

            _grabbedObject = null;
            _grabbedGrabbable = null;

            UnityEngine.Debug.Log("[HandScalerOnGrab] Released object, scaling off.");
            */
        }
        
    }

    private void Update()
    {
        

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

        bool gripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller);
        bool gripUp = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller); // release

        if (_touchHandGrab && gripDown)
        {
            _isScaling = true;

            UnityEngine.Debug.Log($"[HandScalerOnGrab] grab confirmed");
        }
        if (_touchHandRelease && gripUp)
        {
            _isScaling = false;

            UnityEngine.Debug.Log($"[HandScalerOnGrab] release confirmed");
        }

        if (_touchHandRelease && gripDown)
        {
            // do not stop scaling if controller has not been released
            _isScaling = true;

            UnityEngine.Debug.Log($"[HandScalerOnGrab] not released by controller yet");
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

                if (_grabbedTransformer == null)
                {
                    UnityEngine.Debug.Log($"[HandScalerOnGrab] scaling and grabbed object but no transformer");
                }

                _grabbedTransformer.AttachToVirtualHand(_handRoot);

            }
            else
            {
                // No scaling or no grabbed object → normal 1:1 movement
                _handRoot.position = currentRealPose.position;

                // release the object if there is one
                if (_grabbedObject)
                {
                    _currentScaleRatio = _defaultScaleRatio;

                    // Re-enable physics
                    if (_grabbedRigidbody != null)
                    {
                        _grabbedRigidbody.isKinematic = false;
                        _grabbedRigidbody = null;
                    }

                    _grabbedTransformer.DetachFromVirtualHand(); // ensure object is truely released

                    _grabbedObject = null;
                    _grabbedGrabbable = null;
                }
                else
                {
                    UnityEngine.Debug.Log($"[HandScalerOnGrab] no grabbed object");
                }

                if (!_isScaling)
                {
                    UnityEngine.Debug.Log($"[HandScalerOnGrab] not scaling");
                }
            }
        }else
        {
            UnityEngine.Debug.Log($"[HandScalerOnGrab] cannot get pose");
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