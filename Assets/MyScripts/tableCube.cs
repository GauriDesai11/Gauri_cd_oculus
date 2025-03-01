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
    [SerializeField] private GameObject SCube1;  // Small reference cube
    [SerializeField] private GameObject LCube1;  // Large reference cube

    [Header("Cube Folders")]
    [SerializeField] private GameObject[] smallCubes; // Array for small cubes
    [SerializeField] private GameObject[] largeCubes; // Array for large cubes

    [SerializeField] private GameObject TargetPlace;

    private bool sceneLoaded;
    private MRUKRoom currRoom;
    private List<GameObject> spawnedObjects = new();
    private System.Random random = new System.Random();

    private bool SceneAndRoomInfoAvailable => currRoom != null && sceneLoaded;

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
            UnityEngine.Debug.Log("[TableCube] Controller Pressed - Spawning new cube pair");

            // Destroy previously spawned objects
            foreach (var obj in spawnedObjects)
            {
                Destroy(obj);
            }
            spawnedObjects.Clear();

            // Randomly decide if spawning small or large cubes
            bool spawnSmall = random.Next(2) == 0; // 50% chance

            GameObject prefab1, prefab2;

            if (spawnSmall)
            {
                // Select S-Cube-1 and another random small cube
                prefab1 = SCube1;
                prefab2 = smallCubes[random.Next(smallCubes.Length)];
            }
            else
            {
                // Select L-Cube-1 and another random large cube
                prefab1 = LCube1;
                prefab2 = largeCubes[random.Next(largeCubes.Length)];
            }

            // Ensure prefab1 and prefab2 are different
            while (prefab1 == prefab2)
            {
                prefab2 = spawnSmall ? smallCubes[random.Next(smallCubes.Length)] : largeCubes[random.Next(largeCubes.Length)];
            }

            // Randomize which cube is on the left and which is on the right
            bool swapPositions = random.Next(2) == 0; // 50% chance to swap

            foreach (var anchor in currRoom.Anchors)
            {
                Vector3 center = anchor.transform.position;
                Vector3 scale = anchor.transform.localScale;

                float surfaceWidth = scale.x;
                float surfaceLength = scale.z;

                float positionOffset = (3f / 8f) * surfaceWidth;

                Vector3 leftPosition = center - new Vector3((surfaceLength / 2) - positionOffset, 0, 0);
                Vector3 rightPosition = center + new Vector3((surfaceLength / 2) - positionOffset, 0, 0);

                // Swap cube positions randomly
                GameObject leftCube = swapPositions ? prefab2 : prefab1;
                GameObject rightCube = swapPositions ? prefab1 : prefab2;

                // Instantiate cubes on the table
                GameObject objA = Instantiate(leftCube, leftPosition, Quaternion.identity, anchor.transform);
                GameObject objB = Instantiate(rightCube, rightPosition, Quaternion.identity, anchor.transform);
                GameObject objATarget = Instantiate(TargetPlace, leftPosition, Quaternion.identity, anchor.transform);
                GameObject objBTarget = Instantiate(TargetPlace, rightPosition, Quaternion.identity, anchor.transform);

                spawnedObjects.Add(objA);
                spawnedObjects.Add(objB);
                spawnedObjects.Add(objATarget);
                spawnedObjects.Add(objBTarget);

                UnityEngine.Debug.Log($"[TableCube] Spawned {leftCube.name} on left & {rightCube.name} on right");
            }
        }
    }
}



/*
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
*/
