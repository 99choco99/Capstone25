using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("UI Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider sensitivitySlider;


    private void Start()
    {
        bgmSlider.value = SettingManager.instance.BGMVolume;
        sfxSlider.value = SettingManager.instance.SFXVolume;
        sensitivitySlider.value = SettingManager.instance.MouseSensitivity;
        bgmSlider.onValueChanged.AddListener(OnBGMChange);
        sfxSlider.onValueChanged.AddListener(OnSFXChange);
        sensitivitySlider.onValueChanged.AddListener(OnMouseChange);
    }


    public void OnBGMChange(float value)
    {
        SettingManager.instance.SetBGMVolume(value);
    }

    public void OnSFXChange(float value)
    {
        SettingManager.instance.SetSFXVolume(value);
    }

    public void OnMouseChange(float value)
    {
        SettingManager.instance.SetMouseSensitivity(value);
    }
}
