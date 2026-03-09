using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Sound
{
    public string name;         // 키값
    public AudioClip clip;      // 재생할 오디오 파일

    [Range(0f, 1f)]
    public float volume;        // 볼륨
    [Range(0f, 2f)]
    public float pitch;         // 피치 (재생속도, 높낮이)

    public bool loop;           // 반복 재생

    [HideInInspector]
    public AudioSource source;  // 실제 재생할 오디오소스 
}
public class SoundManager : MonoBehaviour
{
    public static SoundManager audioManager { get; private set; }

    [Header("사운드 목록")]
    [Tooltip("여기에 사운드를 추가!")]
    public Sound[] sounds;

    Dictionary<string, Sound> audioDic;

    [Header("BGM 설정")]
    [Range(0f, 1f)]
    public AudioSource bgmSource;
    string currentBGM = "";

    [Header("SFX 설정")]
    [Range(0f, 1f)]
    public AudioSource sfxSource;

    [Header("볼륨 설정")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    void Awake()
    {
        // 싱글톤 초기화
        if (audioManager == null)
        {
            audioManager = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 딕셔너리 초기화
        audioDic = new Dictionary<string, Sound>();
        // 모든 사운드에 AudioSource 추가
        foreach (Sound s in sounds)
        {
            // 빈 사운드 오브젝트를 자식으로 생성하여 오디오소스 추가관리
            GameObject soundObject = new GameObject(s.name);
            soundObject.transform.SetParent(transform);

            s.source = soundObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;

            // 딕셔너리에 추가
            audioDic.Add(s.name, s);
        }

        // BGM 전용 오디오소스 생성
        GameObject bgmObject = new GameObject("BGM");
        bgmObject.transform.SetParent(transform);
        bgmSource = bgmObject.AddComponent<AudioSource>();
        bgmSource.loop = true;

        // SFX 전용 오디오소스 생성
        GameObject sfxObject = new GameObject("SFX");
        sfxObject.transform.SetParent(transform);
        sfxSource = sfxObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
    }

    /// <summary>
    /// BGM 재생
    /// </summary>
    /// <param name="name"></param>
    public void PlayBGM(string name, float volumeScale = 1f)
    {
        // 딕셔너리에서 사운드 찾기
        if (!audioDic.ContainsKey(name))
        {
            // BGM1, bmg1, Bgm1 정확한 키값이 필요하다
            print($"사운드 '{name}'를 찾을 수 없음");
            return;
        }

        // 이미 같은 BGM이 재생 중이라면 리턴
        if (currentBGM == name && bgmSource.isPlaying)
        {
            print($"BGM '{name}'은 이미 재생 중입니다.");
            return;
        }

        // 딕셔너리에서 키값으로 찾아서 실제 클래스를 넘겨받음
        Sound bgm = audioDic[name];

        bgmSource.clip = bgm.clip;
        bgmSource.volume = masterVolume * bgmVolume * bgm.volume * volumeScale;
        bgmSource.Play();

        currentBGM = name;
        print($"BGM 재생: {name}");
    }

    public void PlaySFX(string name, float volumeScale = 1f)
    {
        // 딕셔너리에서 사운드 찾기
        if (!audioDic.ContainsKey(name))
        {
            // BGM1, bmg1, Bgm1 정확한 키값이 필요하다
            print($"사운드 '{name}'를 찾을 수 없음");
            return;
        }

        // 딕셔너리에서 키값으로 찾아서 실제 클래스를 넘겨받음
        Sound sfx = audioDic[name];

        sfxSource.clip = sfx.clip;
        sfxSource.volume = masterVolume * sfxVolume * sfx.volume * volumeScale;
        sfxSource.Play();

        currentBGM = name;
        //print($"SFX 재생: {name}");
    }


    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
        currentBGM = "";
        print("BGM 정지");
    }

    /// <summary>
    /// 마스터 볼륨 설정
    /// </summary>
    /// <param name="volume"></param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        if (bgmSource.isPlaying && !string.IsNullOrEmpty(currentBGM))
        {
            Sound bgm = audioDic[currentBGM];
            bgmSource.volume = masterVolume * bgmVolume * bgm.volume;
        }
    }

    public void SetBGMOnlyVol(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SetSFXOnlyVol(float volume)
    {
        sfxSource.volume = volume;
    }

    ///// <summary>
    ///// 효과음 볼륨 설정
    ///// </summary>
    ///// <param name="volume"></param>
    //public void SetSFXVolume(float volume)
    //{
    //    sfxVolume = Mathf.Clamp01(volume);
    //    // 효과음은 재생시 볼륨이 결정되므로 자동으로 적용된다
    //}

    /// <summary>
    /// 사운드가 재생 중인지 확인
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool IsPlaying(string name)
    {
        if (!audioDic.ContainsKey(name)) return false;

        return audioDic[name].source.isPlaying;
    }

    /// <summary>
    /// 현재 재생 중인 BGM 이름 반환
    /// </summary>
    /// <returns></returns>
    public string GetCurrentBGM()
    {
        return currentBGM;
    }
}
