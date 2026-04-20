using UnityEngine;

public class Sound : Interactable
{
    [SerializeField] private SoundType soundType;
    [SerializeField] private FMODUnity.EventReference soundEvent;

    public SoundType SoundType => soundType;
    public FMODUnity.EventReference SoundEvent => soundEvent;

    protected override void OnPlayerContact(Player player)
    {
        if (PlayerMusic.Instance != null) PlayerMusic.Instance.TryActivate(this);
    }

    void OnDestroy()
    {
        if (PlayerMusic.Instance != null) PlayerMusic.Instance.Deactivate(this);
    }
}
