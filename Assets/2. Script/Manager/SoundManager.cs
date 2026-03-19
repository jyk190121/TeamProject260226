using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
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

    // ==========================================
    // [추가] 나레이션 전용 AudioSource
    // sfxSource와 완전히 분리하여 서로 간섭 없이 재생
    // Inspector에서 별도 AudioSource 컴포넌트를 할당해주세요
    // ==========================================
    [SerializeField] private AudioSource narrationSource;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam = "BGMVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("Default Volume (0 ~ 1)")]
    [Range(0f, 1f)][SerializeField] private float defaultMaster = 0.8f;
    [Range(0f, 1f)][SerializeField] private float defaultBgm = 0.8f;
    [Range(0f, 1f)][SerializeField] private float defaultSfx = 0.8f;

    [Header("Scene BGM")]
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private string inGameSceneName = "InGame";

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

    // ==========================================
    // [추가] 나레이션 재생 중 여부를 외부에서 확인할 수 있는 프로퍼티
    // ==========================================
    public bool IsNarrationPlaying => narrationSource != null && narrationSource.isPlaying;

    private Dictionary<BgmId, SoundLibrary.BgmEntry> bgmMap;
    private Dictionary<SfxId, SoundLibrary.SfxEntry> sfxMap;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

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

        if (SaveManager.Instance != null)
        {
            SaveData data = SaveManager.Instance.Load();
            Master = Mathf.Clamp01(data.volMaster);
            Bgm = Mathf.Clamp01(data.volBgm);
            Sfx = Mathf.Clamp01(data.volSfx);
        }
        else
        {
            Master = Mathf.Clamp01(defaultMaster);
            Bgm = Mathf.Clamp01(defaultBgm);
            Sfx = Mathf.Clamp01(defaultSfx);
        }

        ApplyAllToMixer();
    }

    private void Start()
    {
        PlayCurrentSceneBgm();
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

    public void ApplyLoadedVolumes(float master, float bgm, float sfx)
    {
        Master = Mathf.Clamp01(master);
        Bgm = Mathf.Clamp01(bgm);
        Sfx = Mathf.Clamp01(sfx);

        ApplyAllToMixer();
    }

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

    #region Scene BGM

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayCurrentSceneBgm();
    }

    /// <summary>
    /// GameSceneManager의 SceneName()을 우선 사용해서
    /// 현재 씬에 맞는 BGM을 자동 재생한다.
    /// </summary>
    private void PlayCurrentSceneBgm()
    {
        string currentSceneName = GetCurrentSceneName();

        if (currentSceneName == titleSceneName)
        {
            PlayBgm(BgmId.Title);
        }
        else if (currentSceneName == inGameSceneName)
        {
            PlayBgm(BgmId.InGame);
        }
        else
        {
            StopBgm();
        }
    }

    /// <summary>
    /// 현재 씬 이름을 가져온다.
    /// GameSceneManager가 있으면 그쪽을 사용하고,
    /// 없으면 SceneManager에서 직접 가져온다.
    /// </summary>
    private string GetCurrentSceneName()
    {
        if (GameSceneManager.Instance != null)
            return GameSceneManager.Instance.SceneName();

        return SceneManager.GetActiveScene().name;
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

    // ==========================================
    // [추가] 나레이션 전용 재생 메서드
    // sfxSource와 완전히 분리된 narrationSource를 사용하여
    // 나레이션 재생 중 다른 SFX가 끊기거나 간섭받지 않는다
    // ==========================================
    #region Narration

    /// <summary>
    /// 나레이션 클립을 재생한다.
    /// narrationSource가 없으면 경고 후 무시한다.
    /// </summary>
    public void PlayNarrationClip(AudioClip clip, float volumeScale = 1f)
    {
        if (narrationSource == null)
        {
            Debug.LogWarning("[SoundManager] narrationSource가 할당되지 않았습니다. Inspector에서 AudioSource를 연결해주세요.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] 재생하려는 나레이션 클립이 null입니다.");
            return;
        }

        if (narrationSource.isPlaying)
            narrationSource.Stop();

        narrationSource.clip = clip;
        narrationSource.loop = false;
        narrationSource.volume = Mathf.Clamp01(volumeScale);
        narrationSource.pitch = 1f;
        narrationSource.Play();
    }

    /// <summary>
    /// 현재 재생 중인 나레이션을 즉시 정지한다.
    /// 스킵 처리 시 NarrationManager에서 호출한다.
    /// </summary>
    public void StopNarration()
    {
        if (narrationSource == null) return;

        narrationSource.Stop();
        narrationSource.clip = null;
    }

    #endregion
}
