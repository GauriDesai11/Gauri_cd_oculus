/*
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

[System.Serializable]
public class AnchorData
{
    public List<Vector3> anchorPositions = new();
}

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
    private List<Vector3> anchorPositions = new(); // Store detected tables

    private bool SceneAndRoomInfoAvailable => sceneLoaded && anchorPositions.Count > 0;
    private string anchorDataPath;

    private GameObject[,] prefabCombinations;
    private int comboIndex = 0;

    private void Awake()
    {
        prefabCombinations = new GameObject[,]
        {
            { prefabA, prefabB },
            { prefabB, prefabC },
            { prefabA, prefabC }
        };

        anchorDataPath = Path.Combine(UnityEngine.Application.persistentDataPath, "anchors.json");

        // Load stored anchors from previous runs
        LoadAnchorPositions();
    }

    private void OnEnable()
    {
        mruk.RoomCreatedEvent.AddListener(BindRoomInfo);
    }

    private void OnDisable()
    {
        mruk.RoomCreatedEvent.RemoveListener(BindRoomInfo);
    }

    public void EnableMRUKDemo()
    {
        sceneLoaded = true;
    }

    private void BindRoomInfo(MRUKRoom room)
    {
        currRoom = room;

        if (anchorPositions.Count == 0) // Only store anchors if they haven't been stored before
        {
            anchorPositions.Clear();
            foreach (var anchor in currRoom.Anchors)
            {
                anchorPositions.Add(anchor.transform.position);
            }
            SaveAnchorPositions();
            UnityEngine.Debug.Log($"[TableCube] Stored {anchorPositions.Count} anchor positions.");
        }
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

            // Find the table closest to the user and in front of them
            Vector3 userPosition = Camera.main.transform.position;
            Vector3 userForward = Camera.main.transform.forward;
            Vector3 bestTable = FindClosestFrontTable(userPosition, userForward);

            if (bestTable == Vector3.zero)
            {
                UnityEngine.Debug.LogWarning("[TableCube] No valid table detected in front of the user.");
                return;
            }

            // Get the current prefab pair
            GameObject prefab1 = prefabCombinations[comboIndex, 0];
            GameObject prefab2 = prefabCombinations[comboIndex, 1];

            // Cycle to the next combination
            comboIndex = (comboIndex + 1) % prefabCombinations.GetLength(0);

            // Define default table dimensions
            float surfaceWidth = 1.0f;
            float surfaceLength = 1.5f;
            float surfaceHeight = 0.05f;

            float positionOffset = (3f / 8f) * surfaceWidth;

            Vector3 leftPosition = bestTable + new Vector3(-positionOffset, surfaceHeight / 2, 0);
            Vector3 rightPosition = bestTable + new Vector3(positionOffset, surfaceHeight / 2, 0);

            // Instantiate prefabs
            GameObject objA = Instantiate(prefab1, leftPosition, Quaternion.identity);
            GameObject objB = Instantiate(prefab2, rightPosition, Quaternion.identity);
            GameObject objATarget = Instantiate(TargetPlace, leftPosition, Quaternion.identity);
            GameObject objBTarget = Instantiate(TargetPlace, rightPosition, Quaternion.identity);

            spawnedObjects.Add(objA);
            spawnedObjects.Add(objB);
            spawnedObjects.Add(objATarget);
            spawnedObjects.Add(objBTarget);

            UnityEngine.Debug.Log($"[TableCube] Spawned {prefab1.name} & {prefab2.name} on the closest front-facing table.");
        }
    }

    private Vector3 FindClosestFrontTable(Vector3 userPosition, Vector3 userForward)
    {
        Vector3 bestTable = Vector3.zero;
        float bestDistance = float.MaxValue;

        foreach (var anchorPos in anchorPositions)
        {
            Vector3 directionToTable = anchorPos - userPosition;
            float distance = directionToTable.magnitude;

            // Check if the table is in front of the user
            if (Vector3.Dot(userForward, directionToTable.normalized) > 0.5f) // 0.5 allows some angle tolerance
            {
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTable = anchorPos;
                }
            }
        }

        return bestTable;
    }

    private void SaveAnchorPositions()
    {
        AnchorData data = new AnchorData { anchorPositions = anchorPositions };
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(anchorDataPath, json);
        UnityEngine.Debug.Log("[TableCube] Anchor positions saved.");
    }

    private void LoadAnchorPositions()
    {
        if (File.Exists(anchorDataPath))
        {
            string json = File.ReadAllText(anchorDataPath);
            AnchorData data = JsonUtility.FromJson<AnchorData>(json);
            anchorPositions = data.anchorPositions;
            UnityEngine.Debug.Log("[TableCube] Anchor positions loaded.");
        }
        else
        {
            UnityEngine.Debug.Log("[TableCube] No previous anchor positions found.");
        }
    }
}
*/





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
