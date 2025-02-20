using UnityEngine;
using Oculus.Interaction;
using System.Diagnostics;

public class HandScalerOnGrab : MonoBehaviour
{
    [SerializeField]
    private TouchHandGrabInteractor _handGrabInteractor;

    [SerializeField]
    private MyHandVisual _handVisual; // Your script that scales the hand

    [SerializeField]
    private float _defaultScaleRatio = 1f;

    private void Awake()
    {
        // Subscribe to state changes
        _handGrabInteractor.WhenStateChanged += OnHandGrabStateChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        _handGrabInteractor.WhenStateChanged -= OnHandGrabStateChanged;
    }

    private void OnHandGrabStateChanged(InteractorStateChangeArgs args)
    {
        // Detect a transition *to* Select
        if (args.NewState == InteractorState.Select)
        {
            var grabbedObj = _handGrabInteractor.SelectedInteractable;
            if (grabbedObj != null)
            {
                // For example, read Rigidbody.mass
                var rb = grabbedObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float mass = rb.mass;
                    // Scale the hand movement by 1/mass
                    _handVisual.ScaleMovements = true;
                    _handVisual.ScaleRatio = 1f / mass;

                    UnityEngine.Debug.Log($"[HandScalerOnGrab] Grabbed '{grabbedObj.name}' with mass={mass}.");
                }
            }
        }
        // Detect a transition *from* Select to something else (Hover, Idle, etc.)
        else if (args.PreviousState == InteractorState.Select && args.NewState != InteractorState.Select)
        {
            // We just released
            _handVisual.ScaleMovements = false;
            _handVisual.ScaleRatio = _defaultScaleRatio;
            UnityEngine.Debug.Log("[HandScalerOnGrab] Released object, restoring default scale.");
        }
    }
}
