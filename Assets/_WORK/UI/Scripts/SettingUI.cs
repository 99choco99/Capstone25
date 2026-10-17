using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingUI : UIBase
{
    [Header("UI Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider sensitivitySlider;

    [Header("Resolution")]
    [SerializeField] private TextMeshProUGUI resolutionText;
    [SerializeField] private Button previousResolutionButton;
    [SerializeField] private Button nextResolutionButton;

    public override void Init()
    {
        bgmSlider.onValueChanged.AddListener(OnBGMChange);
        sfxSlider.onValueChanged.AddListener(OnSFXChange);
        sensitivitySlider.onValueChanged.AddListener(OnMouseChange);
        previousResolutionButton.onClick.AddListener(OnPreviousResolutionClick);
        nextResolutionButton.onClick.AddListener(OnNextResolutionClick);
    }

    public override void Open()
    {
        bgmSlider.SetValueWithoutNotify(SettingManager.Instance.BGMVolume);
        sfxSlider.SetValueWithoutNotify(SettingManager.Instance.SFXVolume);
        sensitivitySlider.SetValueWithoutNotify(SettingManager.Instance.MouseSensitivity);
        SettingManager.Instance.RefreshResolutions();
        RefreshResolutionControls();
        base.Open();
    }

    public override void Close()
    {
        if (IsOpen)
            PlayerPrefs.Save();
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

    private void OnPreviousResolutionClick() => ChangeResolution(-1);
    private void OnNextResolutionClick() => ChangeResolution(1);

    private void ChangeResolution(int offset)
    {
        SettingManager manager = SettingManager.Instance;
        if (manager.SetResolution(manager.ResolutionIndex + offset))
            RefreshResolutionControls();
    }

    private void RefreshResolutionControls()
    {
        SettingManager manager = SettingManager.Instance;
        int index = manager.ResolutionIndex;
        Vector2Int size = index >= 0 ? manager.AvailableResolutions[index] : new(Screen.width, Screen.height);
        resolutionText.text = $"{size.x} x {size.y}";
        previousResolutionButton.interactable = index > 0;
        nextResolutionButton.interactable = index < manager.AvailableResolutions.Count - 1;
    }
}
