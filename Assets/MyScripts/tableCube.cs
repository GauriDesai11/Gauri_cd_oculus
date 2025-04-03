using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Diagnostics;

public class TableCube : MonoBehaviour
{
    /*
        This class spawns virtual cubes on the detected table surfaces.
    */

    [Header("MRUK & Controller")]
    [SerializeField] private MRUK mruk;
    [SerializeField] private OVRInput.Controller controller;

    [Header("Prefab Options")]
    [SerializeField] private GameObject SCube1;  // Small reference cube
    [SerializeField] private GameObject LCube1;  // Large reference cube
    [SerializeField] private GameObject BlackCube; // Black cube prefab

    [Header("Cube Folders")]
    [SerializeField] private GameObject[] smallCubes; // Array for small cubes
    [SerializeField] private GameObject[] largeCubes; // Array for large cubes

    [SerializeField] private GameObject TargetPlace;

    private bool sceneLoaded;
    private MRUKRoom currRoom;
    private List<GameObject> spawnedObjects = new();
    private System.Random random = new System.Random();

    private List<(GameObject, GameObject)> uniquePairs = new();
    private int currentPairIndex = 0;
    private bool blackCubeSpawned = false; // Track if the black cube has been placed

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
        GenerateUniquePairs();
    }

    private void BindRoomInfo(MRUKRoom room)
    {
        UnityEngine.Debug.Log("[TableCube] BindRoomInfo");
        currRoom = room;
    }

    private void GenerateUniquePairs()
    {
        uniquePairs.Clear();

        // Generate all unique small cube pairs
        foreach (var cube in smallCubes)
        {
            if (cube != SCube1)
                uniquePairs.Add((SCube1, cube));
        }

        // Generate all unique large cube pairs
        foreach (var cube in largeCubes)
        {
            if (cube != LCube1)
                uniquePairs.Add((LCube1, cube));
        }

        // Shuffle the pairs for randomness
        for (int i = 0; i < uniquePairs.Count; i++)
        {
            int swapIndex = random.Next(uniquePairs.Count);
            (uniquePairs[i], uniquePairs[swapIndex]) = (uniquePairs[swapIndex], uniquePairs[i]);
        }

        currentPairIndex = 0;
    }

    private void Update()
    {
        if (blackCubeSpawned) return; // Stop further spawning if black cube is placed

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller) && SceneAndRoomInfoAvailable)
        {
            if (currentPairIndex >= uniquePairs.Count)
            {
                SpawnBlackCube(); // Spawn the black cube in the center of a table
                return;
            }

            UnityEngine.Debug.Log("[TableCube] Controller Pressed - Spawning new cube pair");

            // Destroy previously spawned objects
            foreach (var obj in spawnedObjects)
            {
                Destroy(obj);
            }
            spawnedObjects.Clear();

            // Get the next unique pair
            (GameObject prefab1, GameObject prefab2) = uniquePairs[currentPairIndex];
            currentPairIndex++; // Move to the next pair for the next spawn

            // Randomize which cube is on the left and which is on the right
            bool swapPositions = random.Next(2) == 0;

            foreach (var anchor in currRoom.Anchors)
            {
                // Only spawn on TABLE anchors
                if (anchor.name != "TABLE")
                {
                    continue; // Skip non-table anchors
                }

                // Spawn the cubes equidistance from the center of the detected table
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

                UnityEngine.Debug.Log($"[TableCube] Spawned {leftCube.name} on left & {rightCube.name} on right on a TABLE");
            }
        }
    }

    private void SpawnBlackCube()
    {
        // Delete the last two cubes before placing the black cube to signal the end of the experiment
        // i.e. all pair of cubes have been spawned exactly once.
        foreach (var obj in spawnedObjects)
        {
            Destroy(obj);
        }
        spawnedObjects.Clear();

        foreach (var anchor in currRoom.Anchors)
        {
            // Find the first TABLE anchor
            if (anchor.name != "TABLE") continue;

            Vector3 center = anchor.transform.position;

            // Instantiate the black cube in the center of the table
            Instantiate(BlackCube, center, Quaternion.identity, anchor.transform);

            UnityEngine.Debug.Log("[TableCube] Spawned Black Cube - No more spawning allowed");
            blackCubeSpawned = true; // Prevent further spawning
            return;
        }
    }
}
