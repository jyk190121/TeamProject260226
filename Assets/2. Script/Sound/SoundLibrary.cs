using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Sound Library", fileName = "SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class BgmEntry
    {
        public BgmId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = true;
    }

    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public BgmEntry[] bgms;
    public SfxEntry[] sfxs;
}
