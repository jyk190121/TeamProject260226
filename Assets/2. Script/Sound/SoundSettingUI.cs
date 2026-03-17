using UnityEngine;
using UnityEngine.UI;

public class SoundSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    void OnEnable()
    {
        // 1. 데이터 로드 및 초기화
        InitUI();

        // 2. 이벤트 리스너 등록
        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
    }

    void OnDisable()
    {
        // 리스너 제거 (메모리 누수 방지)
        masterSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
    }

    private void InitUI()
    {
        // SaveManager에서 데이터를 가져오거나, SoundManager의 현재 값을 슬라이더에 세팅
        SaveData data = SaveManager.Instance.Load();

        masterSlider.value = data.volMaster;
        bgmSlider.value = data.volBgm;
        sfxSlider.value = data.volSfx;

        // SoundManager에도 즉시 반영
        SoundManager.Instance.ApplyLoadedVolumes(data.volMaster, data.volBgm, data.volSfx);
    }

    private void OnMasterSliderChanged(float value)
    {
        SoundManager.Instance.SetMaster(value);
        SaveVolumeData();
    }

    private void OnBgmSliderChanged(float value)
    {
        SoundManager.Instance.SetBgm(value);
        SaveVolumeData();
    }

    private void OnSfxSliderChanged(float value)
    {
        SoundManager.Instance.SetSfx(value);
        SaveVolumeData();
    }

    // 볼륨 변경 시 실시간 저장 (혹은 설정창 닫을 때 한 번만 호출해도 됨)
    private void SaveVolumeData()
    {
        SaveData data = SaveManager.Instance.Load();
        data.volMaster = masterSlider.value;
        data.volBgm = bgmSlider.value;
        data.volSfx = sfxSlider.value;
        SaveManager.Instance.Save(data);
    }
}