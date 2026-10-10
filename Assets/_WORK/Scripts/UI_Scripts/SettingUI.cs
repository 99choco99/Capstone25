using UnityEngine;
using UnityEngine.UI;

public class SettingUI : UIBase
{
    [Header("UI Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider sensitivitySlider;

    public override void Init()
    {
        bgmSlider.onValueChanged.AddListener(OnBGMChange);
        sfxSlider.onValueChanged.AddListener(OnSFXChange);
        sensitivitySlider.onValueChanged.AddListener(OnMouseChange);
    }

    public override void Open()
    {
        bgmSlider.SetValueWithoutNotify(SettingManager.Instance.BGMVolume);
        sfxSlider.SetValueWithoutNotify(SettingManager.Instance.SFXVolume);
        sensitivitySlider.SetValueWithoutNotify(SettingManager.Instance.MouseSensitivity);
        base.Open();
    }

    public override void Close()
    {
        if (IsOpen) PlayerPrefs.Save();
        base.Close();
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
