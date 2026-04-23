using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    [Header("Rooms")]
    [SerializeField] private List<GameObject> roomPrefabs;
    [SerializeField] private GameObject startRoomPrefab;
    [SerializeField] private int lookAhead = 3;

    [Header("Spawn")]
    [SerializeField] private Vector3 spawnPoint = Vector3.zero;

    private Room _frontRoom;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        // Destroy all currently spawned rooms (children of this transform)
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _frontRoom = null;

        Vector3 startPos = spawnPoint;
        GameObject startPrefab = startRoomPrefab != null ? startRoomPrefab : PickRandom();
        _frontRoom = PlaceRoom(startPrefab, startPos, Quaternion.identity);

        for (int i = 0; i < lookAhead; i++)
            SpawnNext();
    }

    void SpawnNext()
    {
        if (_frontRoom == null) return;

        Vector3 pos = _frontRoom.ExitPosition;
        Quaternion rot = Quaternion.LookRotation(_frontRoom.ExitForward);
        _frontRoom = PlaceRoom(PickRandom(), pos, rot);
    }

    Room PlaceRoom(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, position, rotation, transform);
        Room room = go.GetComponent<Room>();

        if (room != null)
        {
            // Align the room's entry point to the requested position
            Vector3 entryOffset = go.transform.InverseTransformPoint(room.EntryPosition);
            go.transform.position -= go.transform.TransformVector(entryOffset);

            room.PlayerEntered += SpawnNext;
        }

        return room;
    }

    GameObject PickRandom()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0) return null;
        return roomPrefabs[Random.Range(0, roomPrefabs.Count)];
    }
}
