/*
using UnityEngine;
using static UnityEngine.Debug;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;

public class MyHandVisual : MonoBehaviour
{
    [SerializeField]
    private HandVisual _handVisual; // Reference to the original HandVisual

    [SerializeField]
    private bool _scaleMovements = false; // Toggle for movement scaling

    [SerializeField]
    private float _scaleRatio = 1.0f; // Scaling factor

    [SerializeField]
    private bool _updateVisibility = true; // Mimics HandVisual visibility updates

    private Transform _root;
    private IList<Transform> _joints;
    private Transform _scaledRoot;
    private List<Transform> _scaledJoints = new List<Transform>();

    private SkinnedMeshRenderer _originalMeshRenderer;
    private SkinnedMeshRenderer _scaledMeshRenderer;
    private MaterialPropertyBlockEditor _handMaterialEditor;
    private MaterialPropertyBlockEditor _scaledMaterialEditor;

    private static MyHandVisual _instance; // Prevents multiple clones
    private bool _started = false;

    private void Start()
    {
        if (_instance != null)
        {
            Debug.LogWarning("MyHandVisual: A duplicate instance was prevented!");
            //Destroy(gameObject); // Destroy extra clones
            return;
        }

        _instance = this; // Set instance

        if (_handVisual == null)
        {
            Debug.LogError("MyHandVisual: HandVisual component is not assigned!");
            return;
        }

        // Initialize HandVisual references
        _root = _handVisual.Root;
        _joints = _handVisual.Joints;
        _handVisual.ForceOffVisibility = true; // Hide original hand

        // Ensure no previous duplicate exists
        if (_scaledRoot != null)
        {
            Destroy(_scaledRoot.gameObject);
            Debug.LogWarning("MyHandVisual: Destroyed old duplicate before creating a new one.");
        }

        // Duplicate the original hand's GameObject (including mesh, bones, etc.)
        GameObject scaledHandInstance = Instantiate(_handVisual.gameObject, _root.position, _root.rotation);
        _scaledRoot = scaledHandInstance.transform;

        // Store joint references **before destroying the HandVisual component**
        HandVisual scaledHandVisual = _scaledRoot.GetComponent<HandVisual>();
        _scaledJoints = new List<Transform>(scaledHandVisual.Joints); // Copy the joint list

        // Now destroy HandVisual to prevent conflicts
        Destroy(scaledHandVisual);

        if (_joints.Count != _scaledJoints.Count)
        {
            Debug.LogError($"MyHandVisual: Joint count mismatch! Original: {_joints.Count}, Scaled: {_scaledJoints.Count}");
            return;
        }

        // Assign SkinnedMeshRenderer & MaterialPropertyBlockEditor
        _originalMeshRenderer = _handVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        _scaledMeshRenderer = _scaledRoot.GetComponentInChildren<SkinnedMeshRenderer>();
        _handMaterialEditor = _handVisual.GetComponentInChildren<MaterialPropertyBlockEditor>();
        _scaledMaterialEditor = _scaledRoot.GetComponentInChildren<MaterialPropertyBlockEditor>();

        _started = true;
    }

    private void Update()
    {
        if (!_started || _handVisual == null || _root == null || _joints == null || _scaledRoot == null)
        {
            return;
        }

        // Hide the original hand
        _handVisual.ForceOffVisibility = true;

        if (_updateVisibility)
        {
            UpdateVisibility();
        }

        // Scale and apply position to the duplicated hand
        if (_scaleMovements)
        {
            Debug.Log("[MyHandVisual] Scaling the duplicated hand");

            // Scale Root Position (Apply scaling but keep relative to parent)
            Vector3 newPosition = _root.parent.position + (_root.position - _root.parent.position) * _scaleRatio;
            _scaledRoot.position = newPosition;
            _scaledRoot.rotation = _root.rotation;

            // Scale Joint Positions
            for (int i = 0; i < _joints.Count; i++)
            {
                if (_joints[i] == null || _scaledJoints[i] == null) continue;

                Pose jointPose = _joints[i].GetPose(Space.Self);
                Vector3 scaledJointPosition = _root.position + (jointPose.position - _root.position) * _scaleRatio;

                _scaledJoints[i].SetPositionAndRotation(scaledJointPosition, jointPose.rotation);
            }

            // Update Material Scaling (if applicable)
            if (_handMaterialEditor != null && _scaledMaterialEditor != null)
            {
                _scaledMaterialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _scaleRatio);
                _scaledMaterialEditor.UpdateMaterialPropertyBlock();
            }
        }
    }

    private void UpdateVisibility()
    {
        if (!_updateVisibility) return;

        bool isTracked = _handVisual.Hand.IsTrackedDataValid;

        if (_scaledMeshRenderer == null) return;

        if (!isTracked)
        {
            if (_scaledMeshRenderer.enabled)
            {
                _scaledMeshRenderer.enabled = false;
            }
        }
        else
        {
            if (!_scaledMeshRenderer.enabled)
            {
                _scaledMeshRenderer.enabled = true;
            }
        }
    }
}
*/

/*
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Diagnostics;

public class MyHandVisual : MonoBehaviour
{
    [SerializeField]
    private HandVisual _handVisual; // Reference to the existing HandVisual component

    [SerializeField]
    private bool _scaleMovements = false; // Toggle for movement scaling

    [SerializeField]
    private float _scaleRatio = 1.0f; // Scaling factor

    private Transform _root;
    private IList<Transform> _joints;

    private void Start()
    {
        if (_handVisual == null)
        {
            UnityEngine.Debug.LogError("MyHandVisual: HandVisual component is not assigned!");
            return;
        }

        _root = _handVisual.Root;
        _joints = _handVisual.Joints;
        _handVisual.ForceOffVisibility = true;

    }

    private void Update()
    {
        _handVisual.ForceOffVisibility = true;
        if (_handVisual == null || _root == null || _joints == null)
        {
            return;
        }

        if (_scaleMovements)
        {
            UnityEngine.Debug.Log("[MyHandVisual] scaling the position");
            UnityEngine.Debug.Log("[MyHandVisual] old position: " + _root.position);

            // Scale Root Position
            _root.position = _root.parent.position + (_root.position - _root.parent.position) * _scaleRatio;
            UnityEngine.Debug.Log("[MyHandVisual] new: " + _root.position);
            UnityEngine.Debug.Log("");

            // Scale Joint Positions
            //for (var i = 0; i < _joints.Count; ++i)
            //{
            //    if (_joints[i] == null) continue;

            //    Pose jointPose = _joints[i].GetPose(Space.Self);
            //    jointPose.position = _root.position + (jointPose.position - _root.position) * _scaleRatio;
            //    _joints[i].SetPose(jointPose, Space.Self);
            //}
        }
    }
}
*/

using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.Diagnostics;

public class MyHandVisual : MonoBehaviour
{
    [SerializeField]
    private HandVisual _handVisual; // Reference to the existing HandVisual component

    [SerializeField]
    private bool _scaleMovements = false; // Toggle for movement scaling

    [SerializeField]
    private float _scaleRatio = 1.0f; // Scaling factor

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

        if (_scaleMovements)
        {
            UnityEngine.Debug.Log("[MyHandVisual] Scaling the position");

            // Retrieve the actual tracked hand pose
            if (_trackedHand.GetRootPose(out Pose handRootPose))
            {
                Vector3 scaledPosition = _root.parent.position + (handRootPose.position - _root.parent.position) * _scaleRatio;
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
                    Vector3 scaledJointPosition = _root.position + (jointPose.position - _root.position) * _scaleRatio;

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
            _materialEditor.MaterialPropertyBlock.SetFloat("_WristScale", _scaleRatio);
            _materialEditor.UpdateMaterialPropertyBlock();
        }
    }
}
