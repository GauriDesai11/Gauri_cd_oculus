using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System;
//using LearnXR.Core.Utilities;
//using System.Collections.Specialized;
//using System.Runtime.Remoting.Messaging;
//using System.Diagnostics;

public class tableCube : MonoBehaviour
{
       ///*
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
       //*/
}
