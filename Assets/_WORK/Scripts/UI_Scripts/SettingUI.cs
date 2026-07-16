using UnityEngine;
using UnityEngine.UI;

public class SettingUI : UIBase
{
    [Header("UI Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider sensitivitySlider;


    private void Start()
    {
        bgmSlider.value = SettingManager.Instance.BGMVolume;
        sfxSlider.value = SettingManager.Instance.SFXVolume;
        sensitivitySlider.value = SettingManager.Instance.MouseSensitivity;
        bgmSlider.onValueChanged.AddListener(OnBGMChange);
        sfxSlider.onValueChanged.AddListener(OnSFXChange);
        sensitivitySlider.onValueChanged.AddListener(OnMouseChange);
    }


    public void OnBGMChange(float value)
    {
        SettingManager.Instance.SetBGMVolume(value);
    }

    public void OnSFXChange(float value)
    {
        SettingManager.Instance.SetSFXVolume(value);
    }

    public void OnMouseChange(float value)
    {
        SettingManager.Instance.SetMouseSensitivity(value);
    }
}
