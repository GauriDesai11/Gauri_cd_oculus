using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseudoHapticController : Controller
{
    private Vector3 _prevRealPosition;
    private Quaternion _prevRealRotation;
    protected Vector3 deltaPosition;
    protected Quaternion deltaRotation;
    
    protected override void UpdateVirtual()
    {
        // Get real controller position/rotation change 
        deltaPosition = RealPosition - _prevRealPosition;
        deltaRotation = RealRotation * Quaternion.Inverse(_prevRealRotation);
        _prevRealPosition = RealPosition;
        _prevRealRotation = RealRotation;

        // Adjust amount moved since previous frame by DisplayControlRatio
        VirtualPosition += Vector3.Scale(deltaPosition , DisplayControlRatioVec);
        VirtualRotation = Quaternion.Slerp(Quaternion.identity, deltaRotation, DisplayControlRatio) * VirtualRotation;
        // Apply the rotation to the grab position offset
        _grabPositionOffset = Quaternion.Slerp(Quaternion.identity, deltaRotation, DisplayControlRatio) * _grabPositionOffset;
        VirtualRotation.Normalize();
        VirtualRotation = RealRotation;
    }

    protected override void Release()
    {
        if (_selected_rb is null) return;
        _selected_rb.linearVelocity = deltaPosition / Time.deltaTime;
        _selected_rb.angularVelocity = deltaRotation.eulerAngles / Time.deltaTime;
        base.Release();
    }
}
