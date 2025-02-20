using UnityEngine;

public class ObjProperties : MonoBehaviour
{

    //private char objectID;   //blocks are numbered 1, 2...
    private Rigidbody _rb;
    private bool _prevIsKinematic;
    [SerializeField] public float _mass;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _prevIsKinematic = false;
        _rb.mass = _mass;
    }

}