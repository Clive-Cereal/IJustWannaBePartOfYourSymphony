using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null) OnPlayerContact(player);
    }

    protected abstract void OnPlayerContact(Player player);
}
