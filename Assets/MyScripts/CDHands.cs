using UnityEngine;
using Oculus.Interaction.Input; // Ensure you have the correct namespace for hand tracking
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class CDHands : MonoBehaviour
{
    [SerializeField]
    private bool scaleMovements = false; // Boolean to enable/disable scaling

    [SerializeField]
    private float scaleRatio = 1.0f; // User-defined scaling factor

    [SerializeField] private IHand hand; // Interface for hand data
    // Interface(typeof(IHand))

    //[SerializeField] UnityEngine.Object hand_object;

    private void Start()
    {
        UnityEngine.Debug.Log("[CDHands]: start");
        hand = GetComponent<IHand>();

        //hand = hand_object as IHand;


        if (hand == null)
        {
            UnityEngine.Debug.LogError("IHand component not found.");
        }
    }

    private void Update()
    {
        UnityEngine.Debug.Log("[CDHands]: Update");
        if (hand == null || !hand.IsTrackedDataValid)
        {
            return;
        }
        UnityEngine.Debug.Log("hand tracked");

        Vector3 handPosition = Vector3.one;
        Quaternion handRotation = Quaternion.identity;

        // Get the current hand position and rotation
        if  (hand.GetRootPose(out Pose handRootPose))
        {
            handPosition = handRootPose.position;
            handRotation = handRootPose.rotation;
        }

        UnityEngine.Debug.Log("position found so will be scaled now");
        UnityEngine.Debug.Log("old handPosition: " + handPosition);

        if (scaleMovements)
        {
            // Apply scaling to the hand position
            handPosition *= scaleRatio;
        }

        // Update the hand's transform
        transform.position = handPosition;
        transform.rotation = handRotation;

        UnityEngine.Debug.Log("transform.position: " + transform.position);
    }
}
