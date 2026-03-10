using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 게임 전체 사운드를 관리하는 매니저.
/// - BGM 재생 / 정지
/// - SFX 재생
/// - AudioMixer 볼륨 적용
/// - SaveManager가 읽어갈 볼륨 키 / 현재 볼륨 값 제공
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Library")]
    [SerializeField] private SoundLibrary library;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("AudioSources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam = "BGMVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("Default Volume (0 ~ 1)")]
    [Range(0f, 1f)][SerializeField] private float defaultMaster = 0.8f;
    [Range(0f, 1f)][SerializeField] private float defaultBgm = 0.8f;
    [Range(0f, 1f)][SerializeField] private float defaultSfx = 0.8f;

    private const string KEY_MASTER = "vol_master";
    private const string KEY_BGM = "vol_bgm";
    private const string KEY_SFX = "vol_sfx";

    public event Action OnVolumeChanged;

    public float Master { get; private set; }
    public float Bgm { get; private set; }
    public float Sfx { get; private set; }

    public string MasterKey => KEY_MASTER;
    public string BgmKey => KEY_BGM;
    public string SfxKey => KEY_SFX;

    private Dictionary<BgmId, SoundLibrary.BgmEntry> bgmMap;
    private Dictionary<SfxId, SoundLibrary.SfxEntry> sfxMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMaps();

        Master = Mathf.Clamp01(defaultMaster);
        Bgm = Mathf.Clamp01(defaultBgm);
        Sfx = Mathf.Clamp01(defaultSfx);

        ApplyAllToMixer();
    }

    /// <summary>
    /// SoundLibrary의 리스트를 ID 기반 딕셔너리로 변환한다.
    /// </summary>
    private void BuildMaps()
    {
        bgmMap = new Dictionary<BgmId, SoundLibrary.BgmEntry>();
        sfxMap = new Dictionary<SfxId, SoundLibrary.SfxEntry>();

        if (library == null)
        {
            Debug.LogWarning("[SoundManager] SoundLibrary가 비어 있습니다.");
            return;
        }

        if (library.bgms != null)
        {
            foreach (var entry in library.bgms)
            {
                if (entry == null) continue;

                if (!bgmMap.ContainsKey(entry.id))
                    bgmMap.Add(entry.id, entry);
                else
                    Debug.LogWarning($"[SoundManager] Duplicate BGM id : {entry.id}");
            }
        }

        if (library.sfxs != null)
        {
            foreach (var entry in library.sfxs)
            {
                if (entry == null) continue;

                if (!sfxMap.ContainsKey(entry.id))
                    sfxMap.Add(entry.id, entry);
                else
                    Debug.LogWarning($"[SoundManager] Duplicate SFX id : {entry.id}");
            }
        }
    }

    /// <summary>
    /// 현재 Master / BGM / SFX 볼륨을 AudioMixer에 반영한다.
    /// </summary>
    private void ApplyAllToMixer()
    {
        ApplyToMixer(masterParam, Master);
        ApplyToMixer(bgmParam, Bgm);
        ApplyToMixer(sfxParam, Sfx);

        OnVolumeChanged?.Invoke();
    }

    /// <summary>
    /// 특정 Mixer 파라미터에 볼륨을 적용한다.
    /// </summary>
    private void ApplyToMixer(string exposedParam, float volume)
    {
        if (mixer == null) return;

        float db = VolumeToDb(volume);
        mixer.SetFloat(exposedParam, db);
    }

    /// <summary>
    /// 0~1 범위의 볼륨 값을 AudioMixer용 dB 값으로 변환한다.
    /// </summary>
    private float VolumeToDb(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (volume <= 0.0001f)
            return -80f;

        return Mathf.Log10(volume) * 20f;
    }

    #region Volume API

    public void SetMaster(float volume)
    {
        Master = Mathf.Clamp01(volume);
        ApplyToMixer(masterParam, Master);
        OnVolumeChanged?.Invoke();
    }

    public void SetBgm(float volume)
    {
        Bgm = Mathf.Clamp01(volume);
        ApplyToMixer(bgmParam, Bgm);
        OnVolumeChanged?.Invoke();
    }

    public void SetSfx(float volume)
    {
        Sfx = Mathf.Clamp01(volume);
        ApplyToMixer(sfxParam, Sfx);
        OnVolumeChanged?.Invoke();
    }

    /// <summary>
    /// SaveManager가 로드한 값을 한 번에 반영할 때 사용.
    /// </summary>
    public void ApplyLoadedVolumes(float master, float bgm, float sfx)
    {
        Master = Mathf.Clamp01(master);
        Bgm = Mathf.Clamp01(bgm);
        Sfx = Mathf.Clamp01(sfx);

        ApplyAllToMixer();
    }

    /// <summary>
    /// SaveManager가 저장할 때 사용할 키 / 값 묶음 반환.
    /// </summary>
    public Dictionary<string, float> GetVolumeSaveData()
    {
        return new Dictionary<string, float>
        {
            { KEY_MASTER, Master },
            { KEY_BGM, Bgm },
            { KEY_SFX, Sfx }
        };
    }

    #endregion

    #region BGM

    public void PlayBgm(BgmId id, bool restartIfSame = false, bool loopOverride = true)
    {
        if (bgmSource == null) return;

        if (id == BgmId.None)
        {
            StopBgm();
            return;
        }

        if (!bgmMap.TryGetValue(id, out var entry) || entry == null)
        {
            Debug.LogWarning($"[SoundManager] BGM entry not found : {id}");
            return;
        }

        AudioClip clip = entry.clip;
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM clip missing : {id}");
            return;
        }

        if (!restartIfSame && bgmSource.isPlaying && bgmSource.clip == clip)
            return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loopOverride ? entry.loop : false;
        bgmSource.volume = Mathf.Clamp01(entry.volume);
        bgmSource.pitch = 1f;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    #endregion

    #region SFX

    /// <summary>
    /// 단일 SFX 소스 방식.
    /// 현재 재생 중이면 끊고 새 SFX를 재생한다.
    /// </summary>
    public void PlaySfx(SfxId id, float volumeScale = 1f)
    {
        if (id == SfxId.None) return;
        if (sfxSource == null) return;

        if (!sfxMap.TryGetValue(id, out var entry) || entry == null)
        {
            Debug.LogWarning($"[SoundManager] SFX entry not found : {id}");
            return;
        }

        AudioClip clip = entry.clip;
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX clip missing : {id}");
            return;
        }

        if (sfxSource.isPlaying)
            sfxSource.Stop();

        sfxSource.clip = clip;
        sfxSource.loop = false;
        sfxSource.volume = Mathf.Clamp01(entry.volume) * Mathf.Clamp01(volumeScale);
        sfxSource.pitch = 1f;
        sfxSource.Play();
    }

    #endregion
}