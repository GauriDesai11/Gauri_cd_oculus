using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Diagnostics;

public class MyHandVisual : MonoBehaviour
{
    [SerializeField]
    private HandVisual _handVisual; // Reference to the existing HandVisual component

    //[SerializeField]
    //private bool _scaleMovements = false; // Toggle for movement scaling

    //[SerializeField]
    //private float _scaleRatio = 1.0f; // Scaling factor

    [SerializeField]
    private bool _scaleMovements = false; // "on/off" toggle

    [SerializeField]
    private float _scaleRatio = 1.0f;     // The actual ratio to apply

    public bool ScaleMovements
    {
        get { return _scaleMovements; }
        set { _scaleMovements = value; }
    }

    public float ScaleRatio
    {
        get { return _scaleRatio; }
        set { _scaleRatio = value; }
    }


    private Transform _root;
    private IList<Transform> _joints;
    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlockEditor _materialEditor;
    private IHand _trackedHand; // Direct reference to the tracked hand

    private bool _started = false;

    private void Start()
    {
        if (_handVisual == null)
        {
            UnityEngine.Debug.LogError("MyHandVisual: HandVisual component is not assigned!");
            return;
        }

        // Get references to HandVisual components
        _root = _handVisual.Root;
        _joints = _handVisual.Joints;
        _meshRenderer = _handVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        _materialEditor = _handVisual.GetComponentInChildren<MaterialPropertyBlockEditor>();
        _trackedHand = _handVisual.Hand;

        if (_root == null || _meshRenderer == null || _trackedHand == null)
        {
            UnityEngine.Debug.LogError("MyHandVisual: Missing required references!");
            return;
        }

        // Ensure original HandVisual does not show
        //_handVisual.ForceOffVisibility = true;

        // Force the virtual hand to be visible
        _meshRenderer.enabled = true;

        _started = true;
    }

    private void Update()
    {
        if (!_started || _handVisual == null || _root == null || _joints == null || _trackedHand == null)
        {
            return;
        }

        // Hide the original HandVisual
        //_handVisual.ForceOffVisibility = true;
        _meshRenderer.enabled = true; // Ensure the hand remains visible

        if (!_trackedHand.IsTrackedDataValid)
        {
            UnityEngine.Debug.LogWarning("MyHandVisual: Hand tracking data is invalid!");
            _meshRenderer.enabled = false; // Hide the hand if tracking is lost
            return;
        }

        if (ScaleMovements)
        {
            UnityEngine.Debug.Log("[MyHandVisual] Scaling the position");

            // Retrieve the actual tracked hand pose
            if (_trackedHand.GetRootPose(out Pose handRootPose))
            {
                Vector3 scaledPosition = _root.parent.position + (handRootPose.position - _root.parent.position) * ScaleRatio;
                _root.position = scaledPosition;
                _root.rotation = handRootPose.rotation;

                UnityEngine.Debug.Log("[MyHandVisual] New Position: " + _root.position);
            }
            else
            {
                UnityEngine.Debug.LogWarning("MyHandVisual: Could not get root pose from tracked hand!");
            }

            // Ensure the mesh follows the new root position
            _meshRenderer.transform.position = _root.position;
            _meshRenderer.transform.rotation = _root.rotation;

            // If the SkinnedMeshRenderer uses bones, update rootBone
            if (_meshRenderer.rootBone != null)
            {
                _meshRenderer.rootBone.position = _root.position;
                _meshRenderer.rootBone.rotation = _root.rotation;
            }

            // Retrieve and update joint positions
            if (_trackedHand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                for (var i = 0; i < _joints.Count; ++i)
                {
                    if (_joints[i] == null) continue;

                    Pose jointPose = localJoints[i];
                    Vector3 scaledJointPosition = _root.position + (jointPose.position - _root.position) * ScaleRatio;

                    _joints[i].SetPositionAndRotation(scaledJointPosition, jointPose.rotation);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("MyHandVisual: Could not get joint positions from tracked hand!");
            }
        }

        // Update material properties (ensures wrist scaling works)
        if (_materialEditor != null)
        {
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", ScaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }
}