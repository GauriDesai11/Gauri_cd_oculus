using UnityEngine;
using Oculus.Interaction;
using System.Diagnostics;

public class HandScalerOnGrab : MonoBehaviour
{
    /*
    [SerializeField]
    private TouchHandGrabInteractor _handGrabInteractor; // Reference the left-hand OVRTouchHandGrabInteractor

    [SerializeField]
    private MyHandVisual _handVisual; // Your script that does the scaling

    // Optional: how much to scale if no object is grabbed
    [SerializeField]
    private float _defaultScaleRatio = 1f;

    private void Awake()
    {
        // Subscribe to pointer events from the TouchHandGrabInteractor
        _handGrabInteractor.WhenPointerEventRaised += OnPointerEvent;
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid leaks
        _handGrabInteractor.WhenPointerEventRaised -= OnPointerEvent;
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                // The hand just grabbed an interactable
                var grabbedObj = _handGrabInteractor.SelectedInteractable;
                if (grabbedObj != null)
                {
                    // Check for a rigidbody to read mass
                    var rb = grabbedObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        float mass = rb.mass; // or your own script's mass property
                        _handVisual.ScaleMovements = true;
                        _handVisual.ScaleRatio = 1f / mass;
                        UnityEngine.Debug.Log($"[HandScalerOnGrab] Grabbed '{grabbedObj.name}' with mass={mass}.");
                    }
                }
                break;

            case PointerEventType.Unselect:
                // The user just released the object
                _handVisual.ScaleMovements = false;
                _handVisual.ScaleRatio = _defaultScaleRatio;
                UnityEngine.Debug.Log("[HandScalerOnGrab] Released object, restoring default scale.");
                break;
        }
    }
    */
}
