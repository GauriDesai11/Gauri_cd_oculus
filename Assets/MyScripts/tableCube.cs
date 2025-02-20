
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;

public class TableCube : MonoBehaviour
{
    [SerializeField] private MRUK mruk;
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private GameObject objectToAdd;

    private bool sceneLoaded;
    private MRUKRoom currRoom;
    private List<GameObject> spawnedObjects = new();

    private bool SceneAndRoomInfoAvailable => currRoom != null && sceneLoaded;
    private int massIndexA = 0;
    private int massIndexB = 1;
    private readonly float offsetDistance = 0.2f; // Distance from center
    //private Vector3 z_offset = 

    private static readonly float[] MassValues = { 1f, 2f, 3f };

    private void OnEnable()
    {
        UnityEngine.Debug.Log("[TableCube] OnEnable");
        mruk.RoomCreatedEvent.AddListener(BindRoomInfo);
    }

    private void OnDisable()
    {
        UnityEngine.Debug.Log("[TableCube] OnDisable");
        mruk.RoomCreatedEvent.RemoveListener(BindRoomInfo);
    }

    public void EnableMRUKDemo()
    {
        UnityEngine.Debug.Log("[TableCube] EnableMRUKDemo");
        sceneLoaded = true;
    }

    private void BindRoomInfo(MRUKRoom room)
    {
        UnityEngine.Debug.Log("[TableCube] BindRoomInfo");
        currRoom = room;
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller) && SceneAndRoomInfoAvailable)
        {
            UnityEngine.Debug.Log("[TableCube] Controller Pressed");

            // Destroy previous objects
            foreach (var obj in spawnedObjects)
            {
                Destroy(obj);
            }
            spawnedObjects.Clear();

            // Cycle mass values
            massIndexA = (massIndexA + 1) % MassValues.Length;
            massIndexB = (massIndexB + 1) % MassValues.Length;

            foreach (var anchor in currRoom.Anchors)
            {
                Vector3 center = anchor.transform.position;
                Vector3 offset = new Vector3(offsetDistance, 0, 0); // Adjust offset along X-axis

                // Instantiate two objects with different masses
                GameObject objA = Instantiate(objectToAdd, center + offset, Quaternion.identity, anchor.transform);
                GameObject objB = Instantiate(objectToAdd, center - offset, Quaternion.identity, anchor.transform);

                // Assign mass values
                Rigidbody rbA = objA.GetComponent<Rigidbody>();
                Rigidbody rbB = objB.GetComponent<Rigidbody>();

                if (rbA != null) rbA.mass = MassValues[massIndexA];
                if (rbB != null) rbB.mass = MassValues[massIndexB];

                spawnedObjects.Add(objA);
                spawnedObjects.Add(objB);

                UnityEngine.Debug.Log($"[TableCube] Spawned Objects with Masses: A={MassValues[massIndexA]}, B={MassValues[massIndexB]}");
            }
        }
    }
}



/*
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System;

public class tableCube : MonoBehaviour
{
    [SerializeField] private MRUK mruk;
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private GameObject objectToAdd;

    private bool sceneLoaded;
    private MRUKRoom currRoom;
    private List<GameObject> tableAnchor = new();

    private bool SceneAndRoomInfoAvailable => currRoom != null && sceneLoaded;

    private void OnEnable()
    {
        Debug.Log("[tableCube] on enable");
        mruk.RoomCreatedEvent.AddListener(BindRoomInfo);
    }

    private void OnDisable()
    {
        Debug.Log("[tableCube] on disable");
        mruk.RoomCreatedEvent.RemoveListener(BindRoomInfo);
    }

    public void EnableMRUKDemo()
    {
        Debug.Log("[tableCube] enable demo");
        sceneLoaded = true;
    }

    private void BindRoomInfo(MRUKRoom room)
    {
        Debug.Log("[tableCube] bind room info");
        currRoom = room;
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller) && SceneAndRoomInfoAvailable)
        {
            Debug.Log("[tableCube] ready to spawn cube");
            if (tableAnchor.Count == 0)
            {
                Debug.Log("[tableCube] no anchors");
                foreach (var anchor in currRoom.Anchors)
                {
                    var createdTableObj = Instantiate(objectToAdd, (Vector3.one), Quaternion.identity, anchor.transform);
                    createdTableObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    tableAnchor.Add(createdTableObj);
                    Debug.Log("[tableCube] created an anchor");
                }
            }
            else
            {
                Debug.Log("[tableCube] anchor(s) found");
                foreach (var t in tableAnchor)
                {
                    Destroy(t);
                    tableAnchor.Clear();
                    Debug.Log("[tableCube] cleared the anchor");
                }
            }
        }
    }
       
}
*/