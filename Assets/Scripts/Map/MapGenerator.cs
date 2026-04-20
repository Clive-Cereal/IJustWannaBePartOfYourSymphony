using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    [Header("Rooms")]
    [SerializeField] private List<GameObject> roomPrefabs;
    [SerializeField] private int minRooms = 5;
    [SerializeField] private int maxRooms = 10;

    [Header("Start")]
    [SerializeField] private GameObject startRoomPrefab;
    [SerializeField] private Transform spawnPoint;

    private readonly List<Room> _spawnedRooms = new();
    private int _currentRoomIndex = -1;

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
        ClearRooms();
        _currentRoomIndex = -1;

        Vector3 nextPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion nextRotation = Quaternion.identity;

        GameObject startPrefab = startRoomPrefab != null ? startRoomPrefab : PickRandom();
        Room current = PlaceRoom(startPrefab, nextPosition, nextRotation);

        int count = Random.Range(minRooms, maxRooms + 1) - 1;
        for (int i = 0; i < count; i++)
        {
            (nextPosition, nextRotation) = AlignToExit(current);
            current = PlaceRoom(PickRandom(), nextPosition, nextRotation);
        }
    }

    Room PlaceRoom(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, position, rotation, transform);
        Room room = go.GetComponent<Room>();

        if (room != null)
        {
            Vector3 entryLocalOffset = go.transform.InverseTransformPoint(room.EntryPosition);
            go.transform.position -= go.transform.TransformVector(entryLocalOffset);

            int index = _spawnedRooms.Count;
            room.PlayerEntered += () => OnPlayerEnteredRoom(index);
            _spawnedRooms.Add(room);
        }

        return room;
    }

    void OnPlayerEnteredRoom(int index)
    {
        // Collapse the previous room immediately
        if (_currentRoomIndex >= 0 && _currentRoomIndex < _spawnedRooms.Count)
        {
            Room previous = _spawnedRooms[_currentRoomIndex];
            if (previous != null) previous.CollapseImmediate();
        }

        _currentRoomIndex = index;
    }

    (Vector3 position, Quaternion rotation) AlignToExit(Room room)
    {
        if (room == null) return (Vector3.zero, Quaternion.identity);
        return (room.ExitPosition, Quaternion.LookRotation(room.ExitForward));
    }

    GameObject PickRandom()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0) return null;
        return roomPrefabs[Random.Range(0, roomPrefabs.Count)];
    }

    public void ClearRooms()
    {
        foreach (Room r in _spawnedRooms)
            if (r != null) Destroy(r.gameObject);
        _spawnedRooms.Clear();
    }
}
