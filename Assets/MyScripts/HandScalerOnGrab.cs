using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Diagnostics;

public class HandScalerOnGrab : MonoBehaviour
{
    [Header("Hand Visual / Grab References")]
    [SerializeField]
    private HandVisual _handVisual;
    // The Oculus-provided HandVisual for your skeleton & mesh

    [SerializeField]
    private TouchHandGrabInteractor _handGrabInteractor;
    // The left-hand interactor that detects grabs

    [Header("Default Movement Scale")]
    [SerializeField]
    private float _defaultScaleRatio = 1f;
    // Movement scale when NOT grabbing an object

    // Runtime scaling logic
    private float _currentScaleRatio = 1f;
    private bool _isScaling = false;

    // Cached references from _handVisual
    private Transform _root;                        // Root of the hand skeleton
    private IList<Transform> _joints;               // Individual finger joints
    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlockEditor _materialEditor;
    private IHand _trackedHand;                     // The actual tracked hand data

    private bool _started = false;

    // For offset-based scaling:
    // We'll record where the real hand was and where the virtual hand was at the moment of grab,
    // then scale the delta each frame so it all lines up when you come back.
    private Vector3 _grabStartRealPos;
    private Vector3 _grabStartVirtualPos;

    private void Awake()
    {
        // Subscribe to grab state changes (Hover→Select, Select→Unselect, etc.)
        if (_handGrabInteractor != null)
        {
            _handGrabInteractor.WhenStateChanged += OnHandGrabStateChanged;
        }
        else
        {
            UnityEngine.Debug.LogError($"[{nameof(HandScalerOnGrab)}] HandGrabInteractor is not assigned.");
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
        // Validate references
        if (_handVisual == null)
        {
            UnityEngine.Debug.LogError($"[{nameof(HandScalerOnGrab)}] HandVisual is not assigned!");
            return;
        }

        _root = _handVisual.Root;
        _joints = _handVisual.Joints;
        _meshRenderer = _handVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        _materialEditor = _handVisual.GetComponentInChildren<MaterialPropertyBlockEditor>();
        _trackedHand = _handVisual.Hand;

        if (_root == null || _joints == null || _meshRenderer == null || _trackedHand == null)
        {
            UnityEngine.Debug.LogError($"[{nameof(HandScalerOnGrab)}] Missing one or more required references!");
            return;
        }

        // Ensure the HandVisual's SkinnedMesh is visible
        // (You can toggle or remove this if you want separate control)
        _meshRenderer.enabled = true;

        _started = true;
    }

    /// <summary>
    /// Called by TouchHandGrabInteractor whenever it changes states.
    /// We detect the moment the user grabs an object (Select) or releases (Unselect).
    /// </summary>
    private void OnHandGrabStateChanged(InteractorStateChangeArgs args)
    {
        // Transition TO Select => grabbed object
        if (args.NewState == InteractorState.Select)
        {
            var grabbedObj = _handGrabInteractor.SelectedInteractable;
            if (grabbedObj != null)
            {
                // Attempt to read mass from a Rigidbody
                var rb = grabbedObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float mass = rb.mass;
                    _currentScaleRatio = 1f / mass; // e.g. mass=2 => ratio=0.5
                    _isScaling = true;

                    // Where was the REAL hand at this instant?
                    if (_trackedHand.GetRootPose(out Pose realHandPose))
                    {
                        _grabStartRealPos = realHandPose.position;
                    }

                    // Where was the VIRTUAL hand's root at this instant?
                    _grabStartVirtualPos = _root.position;

                    UnityEngine.Debug.Log($"[{nameof(HandScalerOnGrab)}] Grabbed {grabbedObj.name}, mass={mass}, scaleRatio={_currentScaleRatio}");
                }
            }
        }
        // Transition OUT of Select => released object
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            _isScaling = false;
            _currentScaleRatio = _defaultScaleRatio;
            UnityEngine.Debug.Log($"[{nameof(HandScalerOnGrab)}] Released object, reverting to default scale={_defaultScaleRatio}");
        }
    }

    private void Update()
    {
        if (!_started || _root == null || _trackedHand == null)
        {
            return;
        }

        // If tracking is invalid, hide the mesh
        if (!_trackedHand.IsTrackedDataValid)
        {
            _meshRenderer.enabled = false;
            return;
        }
        else
        {
            _meshRenderer.enabled = true;
        }

        // Always update rotation from the real hand (1:1), 
        // so the wrist & fingers rotate normally
        if (_trackedHand.GetRootPose(out Pose currentRealPose))
        {
            _root.rotation = currentRealPose.rotation;

            if (_isScaling)
            {
                // (A) Offset-based approach
                // => The difference between current real hand & real hand at time of grab
                Vector3 realDelta = currentRealPose.position - _grabStartRealPos;

                // => Multiply by the ratio
                Vector3 scaledDelta = realDelta * _currentScaleRatio;

                // => Apply to the virtual hand's original position at time of grab
                _root.position = _grabStartVirtualPos + scaledDelta;
            }
            else
            {
                // (B) Normal 1:1 movement
                _root.position = currentRealPose.position;
            }
        }

        // Now update each finger joint to match the tracked hand's local joint data
        // Since we've moved _root above, the joints will appear in the correct
        // final positions & rotations in world space.
        if (_trackedHand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
        {
            for (int i = 0; i < _joints.Count; i++)
            {
                if (_joints[i] == null) continue;

                Pose localJointPose = localJoints[i];
                // Convert the local joint offset to world space:
                Vector3 worldJointPos = _root.TransformPoint(localJointPose.position);
                Quaternion worldJointRot = _root.rotation * localJointPose.rotation;

                _joints[i].SetPositionAndRotation(worldJointPos, worldJointRot);
            }
        }

        // Optional: Apply the scale ratio to the wrist scale in the material
        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _currentScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }
}
