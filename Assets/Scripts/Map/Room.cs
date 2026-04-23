using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Room : MonoBehaviour
{
    private static readonly Vector3 EntryLocal = new(0f, 0f, -10f);
    private static readonly Vector3 ExitLocal  = new(0f, 0f,  10f);

    [Header("Events")]
    public UnityEvent onPlayerEntered;

    public event Action PlayerEntered;

    public Vector3 EntryPosition => transform.TransformPoint(EntryLocal);
    public Vector3 ExitPosition  => transform.TransformPoint(ExitLocal);
    public Vector3 ExitForward   => transform.forward;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;
        PlayerEntered?.Invoke();
        onPlayerEntered.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;
        Destroy(gameObject, 1f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(EntryPosition, 0.3f);
        Gizmos.DrawRay(EntryPosition, transform.forward);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(ExitPosition, 0.3f);
        Gizmos.DrawRay(ExitPosition, transform.forward);
    }
}
