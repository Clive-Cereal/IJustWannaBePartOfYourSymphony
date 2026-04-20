using System.Collections.Generic;
using UnityEngine;

public class PlayerMusic : MonoBehaviour
{
    public static PlayerMusic Instance { get; private set; }

    private static readonly Dictionary<SoundType, int> MaxInstances = new()
    {
        { SoundType.Ambient, 3 },
        { SoundType.Beats,   1 },
        { SoundType.Melody,  5 },
    };

    private struct ActiveSound
    {
        public Sound Source;
        public FMOD.Studio.EventInstance Instance;
    }

    private readonly Dictionary<SoundType, List<ActiveSound>> _active = new()
    {
        { SoundType.Ambient, new List<ActiveSound>() },
        { SoundType.Beats,   new List<ActiveSound>() },
        { SoundType.Melody,  new List<ActiveSound>() },
    };

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        StopAll();
        if (Instance == this) Instance = null;
    }

    public void TryActivate(Sound sound)
    {
        List<ActiveSound> active = _active[sound.SoundType];

        if (active.Exists(a => a.Source == sound)) return;

        if (active.Count >= MaxInstances[sound.SoundType])
            StopOldest(sound.SoundType);

        if (sound.SoundEvent.IsNull) return;

        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(sound.SoundEvent);
        instance.start();
        active.Add(new ActiveSound { Source = sound, Instance = instance });
    }

    public void Deactivate(Sound sound)
    {
        List<ActiveSound> active = _active[sound.SoundType];
        int index = active.FindIndex(a => a.Source == sound);
        if (index < 0) return;
        Stop(active[index]);
        active.RemoveAt(index);
    }

    void StopOldest(SoundType type)
    {
        List<ActiveSound> active = _active[type];
        if (active.Count == 0) return;
        Stop(active[0]);
        active.RemoveAt(0);
    }

    void StopAll()
    {
        foreach (List<ActiveSound> list in _active.Values)
        {
            foreach (ActiveSound a in list) Stop(a);
            list.Clear();
        }
    }

    void Stop(ActiveSound a)
    {
        if (!a.Instance.isValid()) return;
        a.Instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        a.Instance.release();
    }
}
