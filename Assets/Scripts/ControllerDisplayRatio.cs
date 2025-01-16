using UnityEngine;
using UnityEngine.XR;

public class VirtualController : MonoBehaviour
{
    public XRNode controllerNode; // XRNode for left or right controller
    public XRNode headsetNode = XRNode.Head; // XRNode for the headset
    public float c_d = 1.0f; // Scaling factor for controller movement

    private Vector3 previousHeadsetPosition;
    private Vector3 initialControllerPosition;
    private bool initialized = false;

    void Update()
    {
        // Get the real controller and headset positions
        InputDevice controllerDevice = InputDevices.GetDeviceAtXRNode(controllerNode);
        InputDevice headsetDevice = InputDevices.GetDeviceAtXRNode(headsetNode);

        if (controllerDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 currentControllerPosition) &&
            controllerDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentControllerRotation) &&
            headsetDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 currentHeadsetPosition))
        {
            // Initialize positions on the first frame
            if (!initialized)
            {
                previousHeadsetPosition = currentHeadsetPosition;
                initialControllerPosition = currentControllerPosition;
                initialized = true;
            }

            // Calculate headset movement
            Vector3 headsetMovement = currentHeadsetPosition - previousHeadsetPosition;

            // Calculate scaled controller movement
            Vector3 scaledControllerMovement = (currentControllerPosition - initialControllerPosition) / c_d;

            // Compute the virtual controller's new position
            Vector3 virtualPosition = initialControllerPosition + scaledControllerMovement + headsetMovement;

            // Update the virtual controller's position and rotation
            transform.position = virtualPosition;
            transform.rotation = currentControllerRotation;

            // Update the previous headset position for the next frame
            previousHeadsetPosition = currentHeadsetPosition;
        }
    }
}
