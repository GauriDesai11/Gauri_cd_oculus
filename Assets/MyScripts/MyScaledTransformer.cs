using UnityEngine;
using Oculus.Interaction;

public class MyScaledTransformer : MonoBehaviour, ITransformer
{
    private Grabbable _grabbable;
    private Transform _virtualHand;
    private bool _isAttached = false;

    private Vector3 _initialOffset; // Offset from the virtual hand when grabbed
    private Quaternion _initialRotationOffset; // Rotation offset

    [SerializeField]
    private float _scaleFactor = 1f; // Scale factor for movement
    [SerializeField]
    private float _smoothingFactor = 10f; // Controls movement smoothness

    public void Initialize(IGrabbable grabbable)
    {
        _grabbable = (Grabbable)grabbable;
    }

    public void BeginTransform()
    {
        if (_grabbable.GrabPoints.Count > 0 && _virtualHand != null)
        {
            // Store the initial offset from the hand's center
            _initialOffset = _virtualHand.InverseTransformPoint(transform.position);

            // Store the initial rotation offset
            _initialRotationOffset = Quaternion.Inverse(_virtualHand.rotation) * transform.rotation;

            //_isAttached = true;
        }
    }

    public void AttachToVirtualHand(Transform virtualHand)
    {
        UnityEngine.Debug.Log("[MyScaledTransformer] attaching object to hand");

        _virtualHand = virtualHand;
        _isAttached = true;

        // Capture initial offset & rotation relative to virtual hand
        _initialOffset = _virtualHand.InverseTransformPoint(transform.position);
        _initialRotationOffset = Quaternion.Inverse(_virtualHand.rotation) * transform.rotation;
    }

    public void DetachFromVirtualHand()
    {
        UnityEngine.Debug.Log("[MyScaledTransformer] detaching object from hand");

        _isAttached = false;

        // Enable physics when released
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    public void UpdateTransform()
    {
        if (_isAttached && _virtualHand != null)
        {
            // Compute new position and rotation relative to the virtual hand
            Vector3 targetPosition = _virtualHand.TransformPoint(_initialOffset);
            Quaternion targetRotation = _virtualHand.rotation * _initialRotationOffset;

            // Apply movement smoothing for better realism
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * _smoothingFactor);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _smoothingFactor);

            UnityEngine.Debug.Log("[MyScaledTransformer] updated object position");
        }
    }

    public void EndTransform()
    {
        if (_isAttached)
        {
            UnityEngine.Debug.Log("[MyScaledTransformer] transformer ending but object is still attached");
        }
        _isAttached = false;
    }
}



/*
using UnityEngine;
using Oculus.Interaction;

public class MyScaledTransformer : MonoBehaviour, ITransformer
{
    private Grabbable _grabbable;
    private Pose _initialGrabPose;
    private Pose _initialObjectPose;

    [SerializeField]
    private float _scaleFactor = 0.5f; // Example scale factor

    // Initialize is called once by the Grabbable
    public void Initialize(IGrabbable grabbable)
    {
        _grabbable = (Grabbable)grabbable;
    }

    // Called when the grab begins (e.g., pointer event = Select or Unselect)
    public void BeginTransform()
    {
        if (_grabbable.GrabPoints.Count > 0)
        {
            // The initial "grab" pose of the hand
            _initialGrabPose = _grabbable.GrabPoints[0];

            // The object's pose at the moment of grab
            _initialObjectPose = new Pose(
                _grabbable.Transform.position,
                _grabbable.Transform.rotation
            );
        }
    }

    // Called every frame while grabbed (pointer event = Move)
    public void UpdateTransform()
    {
        if (_grabbable.GrabPoints.Count == 0) return;

        // Current hand pose (real hand)
        Pose currentHandPose = _grabbable.GrabPoints[0];

        // Calculate how much the hand has moved
        Vector3 handPositionDelta = currentHandPose.position - _initialGrabPose.position;
        // Apply your custom scale factor to that movement
        Vector3 scaledDelta = handPositionDelta * _scaleFactor;

        // Reconstruct the new position for the object
        Vector3 newPosition = _initialObjectPose.position + scaledDelta;
        // For rotation, you can do your own logic or leave it at 1:1, for example:
        Quaternion rotationOffset =
            Quaternion.Inverse(_initialGrabPose.rotation) * currentHandPose.rotation;
        Quaternion newRotation = _initialObjectPose.rotation * rotationOffset;

        // Finally, apply those transforms to the target transform
        _grabbable.Transform.SetPositionAndRotation(newPosition, newRotation);
    }

    public void controllerPressed()
    {
        UnityEngine.Debug.Log("[Transformer] controller button pressed down");
    }

    public void controllerReleased()
    {
        UnityEngine.Debug.Log("[Transformer] controller button released");
    }

    // Called when the transform ends (grab is fully released)
    public void EndTransform()
    {
        // Nothing special to do here in this example
    }
}
*/