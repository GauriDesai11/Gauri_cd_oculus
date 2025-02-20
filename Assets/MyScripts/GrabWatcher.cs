using UnityEngine;
using Oculus.Interaction; // Namespace where Grabbable/PointerEvent live

public class GrabWatcher : MonoBehaviour
{
    [SerializeField]
    private Grabbable _grabbable; // Assign in Inspector or via code

    private void Awake()
    {
        if (_grabbable == null)
        {
            _grabbable = GetComponent<Grabbable>();
        }

        // Subscribe to pointer events
        _grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent pointerEvent)
    {
        switch (pointerEvent.Type)
        {
            case PointerEventType.Select:
                // This means the object has just been grabbed
                Debug.Log($"{_grabbable.name} was grabbed!");
                // Your custom “on grab” logic here
                break;

            case PointerEventType.Unselect:
                // This means the object has just been released
                Debug.Log($"{_grabbable.name} was released!");
                // Your custom “on release” logic here
                break;

                // Optional: You can watch for Move, Cancel, etc. if needed.
        }
    }
}
