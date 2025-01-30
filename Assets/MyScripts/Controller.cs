using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

public class Controller : MonoBehaviour
{
    public virtual string Name => "Base Controller";
    private ControllerData _controllerData;
    private InputDevice _device;

    protected Vector3 RealPosition;
    protected Quaternion RealRotation;
    protected Vector3 VirtualPosition;
    protected Quaternion VirtualRotation = Quaternion.identity;

    protected float DisplayControlRatio = 1f;   //reciprocal of Control/Display ratio
    protected Vector3 DisplayControlRatioVec = new Vector3(1f, 1f, 1f);

    private bool _grabDown;
    private bool _toggleDown;
    protected Vector3 _grabPositionOffset;  //where the object is grabbed relative to its origin
    protected Quaternion _grabRotationOffset;

    private Transform _realRepresentation;
    private Transform _visual;
    //private GameObject UIobject;
    //private Vector3 UIobjectDisplayPos;
    private readonly Collider[] _colliders = new Collider[5];
    protected Rigidbody _selected_rb;

    private bool io_output;


    protected virtual void Start()
    {
        _controllerData = GetComponent<ControllerData>();
        _realRepresentation = Instantiate(_controllerData.realPrefab, transform);
        _visual = Instantiate(_controllerData.visualPrefab, transform);
        InputDevices.deviceConnected += DeviceConnected;
        UpdateDevice();
        // Reset drift once all setup so that we start with no drift
        Invoke(nameof(ResetDrift), 1f);

        /*
        //Sliders
        UIobject = GameObject.Find("UI Sample");
        UIobjectDisplayPos = UIobject.transform.position;   //save initial position of UI setup
        */
    }

    private void OnEnable()
    {
        ResetDrift();
    }

    private void OnDisable()
    {
        _realRepresentation.position = Vector3.one * -100f;
        _visual.position = Vector3.one * -100f;
    }

    private void DeviceConnected(InputDevice device)
    {
        UpdateDevice();
    }

    private void UpdateDevice()
    {
        _device = InputDevices.GetDeviceAtXRNode(_controllerData.controllerNode);
    }

    protected Vector3 GetPosition()
    {
        if (_device.isValid && _device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            return position;
        return Vector3.zero;
    }

    protected Quaternion GetRotation()
    {
        if (_device.isValid && _device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            return rotation;
        }
        return Quaternion.identity;
    }


    protected void CheckGrab()
    {
        io_output = _device.TryGetFeatureValue(CommonUsages.gripButton, out bool value);
        if (_grabDown && !value)    //grip released
        {
            _grabDown = false;
            Release();
        }
        if (!_grabDown && value)  //grab pressed
        {
            _grabDown = true;
            Grab();
        }

    }


    protected void CheckSliderToggle()
    {
        //primary button is X (left hand controller)
        //secondary button is Y  (left hand controller)
        io_output = _device.TryGetFeatureValue(CommonUsages.primaryButton, out bool value);
        //        Debug.Log(Time.time + ":  CheckSliderToggl: _device.isValid = " + _device.isValid
        //           + ", io_output = " + io_output + ", value = " + value);
        if (_toggleDown && !value)  //button released
        {
            _toggleDown = false;
        }
        if (!_toggleDown && value)  //button pressed
        {
            _toggleDown = true;
            //_controllerData.showSliders = !_controllerData.showSliders;
        }
    }



    private void ProcessInput()
    {
        Vector3 localPosition = GetPosition();
        //RealPosition = transform.TransformPoint(localPosition);
        RealPosition = transform.position;
        RealRotation = GetRotation();

        //Vector3 worldPos = transform.position;
        //Debug.Log("local: " + localPosition +  ", world position: " + worldPos + ", global position: " + RealPosition);

        CheckGrab();
        CheckSliderToggle();
    }

    protected virtual void UpdateVirtual()
    {
        VirtualPosition = RealPosition;
        VirtualRotation = RealRotation;
    }

    protected virtual void UpdateRepresentation()
    {
        _visual.position = VirtualPosition;
        _visual.localRotation = VirtualRotation.normalized;
        if (_controllerData.showReal)
        {
            _realRepresentation.position = RealPosition;
            _realRepresentation.localRotation = RealRotation.normalized;
        }
        else
        {
            _realRepresentation.position = Vector3.one * -100f; //move real controllers out of the way
        }
        /*
        if (_controllerData.showSliders)
        {
            UIobject.transform.position = UIobjectDisplayPos;
        }
        else
        {
            UIobject.transform.position = Vector3.one * -100f;  //move UI (sliders + reset button) out of the way
        }
        */
    }

    protected virtual void UpdateHolding()
    {
        if (_selected_rb)
        {
            _selected_rb.isKinematic = true;
            _selected_rb.position = VirtualPosition + _grabPositionOffset;
            _selected_rb.rotation = VirtualRotation.normalized * _grabRotationOffset;
            
        }
    }

    private void Update()
    {
        ProcessInput();
        UpdateVirtual();
        UpdateRepresentation();
        UpdateHolding();
    }

    protected virtual void ResetDrift()
    {
        VirtualPosition = RealPosition;
        VirtualRotation = RealRotation;
    }

    protected virtual Vector3 printPositions()
    {
        Vector3 localPosition = GetPosition();
        
        Vector3 worldPos = transform.position;

        Debug.Log("local: " + localPosition + ", world position: " + worldPos + ", global position: " + RealPosition);
        return worldPos;
    }

    protected virtual void Grab()
    {
        Vector3 WP = printPositions();

        if (_selected_rb) return;
        int size = Physics.OverlapSphereNonAlloc(VirtualPosition, _controllerData.grabRadius, _colliders, _controllerData.layerMask);
        if (size == 0) return;

        Debug.Log("Grab: RealPosition " + RealPosition + " VirtualPosition " + VirtualPosition + " _controllerData.grabRadius " + _controllerData.grabRadius
            + " size " + size);

        // Find closest pickable object within range
        float smallestDistance = Mathf.Infinity;
        Collider _selected_collider = null;
        for (int i = 0; i < size; i++)
        {
            float distance = (_colliders[i].ClosestPoint(VirtualPosition) - VirtualPosition).sqrMagnitude;
            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                _selected_collider = _colliders[i];
            }
        }
        _selected_rb = _selected_collider.attachedRigidbody;
        DisplayControlRatio = 1f / _selected_rb.mass;   //large mass -> less display movement realtive to controller movement
        DisplayControlRatioVec.y = 1f / _selected_rb.mass;
        _selected_rb.isKinematic = true;    //turn off physics for selected object
        _grabPositionOffset = _selected_rb.position - VirtualPosition;
        _grabRotationOffset = Quaternion.Inverse(VirtualRotation) * _selected_rb.rotation;
    }

    protected virtual void Release()
    {
        if (!_selected_rb) return;
        _selected_rb.isKinematic = false;   //turn physics back on
        _selected_rb = null;
        DisplayControlRatio = 1f;
        DisplayControlRatioVec.y = 1f;
    }
}
