
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Diagnostics;

public class TableCube : MonoBehaviour
{
    [Header("MRUK & Controller")]
    [SerializeField] private MRUK mruk;
    [SerializeField] private OVRInput.Controller controller;

    [Header("Prefab Options")]
    [SerializeField] private GameObject prefabA;
    [SerializeField] private GameObject prefabB;
    [SerializeField] private GameObject prefabC;
    [SerializeField] private GameObject TargetPlace;

    private bool sceneLoaded;
    private MRUKRoom currRoom;
    private List<GameObject> spawnedObjects = new();

    private bool SceneAndRoomInfoAvailable => currRoom != null && sceneLoaded;

    // Possible prefab combinations
    private GameObject[,] prefabCombinations;
    private int comboIndex = 0; // Tracks the current prefab combination

    private void Awake()
    {
        // Define the prefab combinations
        prefabCombinations = new GameObject[,]
        {
            { prefabA, prefabB },
            { prefabB, prefabC },
            { prefabA, prefabC }
        };
    }

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
            UnityEngine.Debug.Log("[TableCube] Controller Pressed - Spawning new prefab combination");

            // Destroy previous objects
            foreach (var obj in spawnedObjects)
            {
                Destroy(obj);
            }
            spawnedObjects.Clear();

            // Get the current prefab pair
            GameObject prefab1 = prefabCombinations[comboIndex, 0];
            GameObject prefab2 = prefabCombinations[comboIndex, 1];

            // Cycle to the next combination for the next button press
            comboIndex = (comboIndex + 1) % prefabCombinations.GetLength(0);

            foreach (var anchor in currRoom.Anchors)
            {
                Vector3 center = anchor.transform.position;
                Vector3 scale = anchor.transform.localScale;

                float surfaceWidth = scale.x;
                float surfaceHeight = scale.y;
                float surfaceLength = scale.z;

                // Calculate the 3/8th positions from each end
                float positionOffset = (3f / 8f) * surfaceWidth;

                Vector3 leftPosition = center - new Vector3((surfaceLength / 2) - positionOffset, 0, 0);
                Vector3 rightPosition = center + new Vector3((surfaceLength / 2) - positionOffset, 0, 0);

                // Adjust height to place objects ON TOP of the surface
                //leftPosition.y += surfaceHeight / 2;
                //rightPosition.y += surfaceHeight / 2;

                // Instantiate prefabs
                GameObject objA = Instantiate(prefab1, leftPosition, Quaternion.identity, anchor.transform);
                GameObject objB = Instantiate(prefab2, rightPosition, Quaternion.identity, anchor.transform);
                GameObject objATarget = Instantiate(TargetPlace, leftPosition, Quaternion.identity, anchor.transform);
                GameObject objBTarget = Instantiate(TargetPlace, rightPosition, Quaternion.identity, anchor.transform);

                spawnedObjects.Add(objA);
                spawnedObjects.Add(objB);
                spawnedObjects.Add(objATarget);
                spawnedObjects.Add(objBTarget);

                UnityEngine.Debug.Log($"[TableCube] Spawned {prefab1.name} & {prefab2.name}");
            }
        }
    }
}


/*
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
*/