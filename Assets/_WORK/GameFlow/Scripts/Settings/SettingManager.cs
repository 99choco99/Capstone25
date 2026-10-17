using UnityEngine;
using SoundEffectManager;
using System.Collections.Generic;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;

    [SerializeField, Range(0f, 1f)] private float defaultBGMVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 0.5f;

    [Header("설정값")]
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float MouseSensitivity { get; private set; }

    public IReadOnlyList<Vector2Int> AvailableResolutions => availableResolutions;
    public int ResolutionIndex { get; private set; } = -1;

    private readonly List<Vector2Int> availableResolutions = new();

    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";
    private const string SENS_KEY = "MouseSensitivity";
    private const string WIDTH_KEY = "ScreenWidth";
    private const string HEIGHT_KEY = "ScreenHeight";
    public const float DefaultMouseSensitivity = 0.3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeManager()
    {
        BGMVolume = PlayerPrefs.GetFloat(BGM_KEY, defaultBGMVolume);
        SFXVolume = PlayerPrefs.GetFloat(SFX_KEY, defaultSFXVolume);
        MouseSensitivity = PlayerPrefs.GetFloat(SENS_KEY, DefaultMouseSensitivity);
        RefreshResolutions();
    }

    private void Start()
    {
        // (AudioMixer는 데시벨(log) 값을 사용하므로 변환 필요)
        SetBGMVolume(BGMVolume);
        SetSFXVolume(SFXVolume);
        RestoreResolution();
    }

    public void SetBGMVolume(float volume)
    {
        BGMVolume = volume;
        // 3. 0이 아닐 때만 정상적으로 데시벨 계산
        SoundManager.Instance.SetBGMVolume(volume);

        PlayerPrefs.SetFloat(BGM_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = volume;
        // 3. 0이 아닐 때만 정상적으로 데시벨 계산
        SoundManager.Instance.SetSFXVolume(volume);
        PlayerPrefs.SetFloat(SFX_KEY, volume);
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        MouseSensitivity = sensitivity;
        PlayerPrefs.SetFloat(SENS_KEY, sensitivity);
    }

    public void RefreshResolutions()
    {
        availableResolutions.Clear();
        foreach (Resolution resolution in Screen.resolutions)
        {
            Vector2Int size = new(resolution.width, resolution.height);
            if (size.x > 0 && size.y > 0 && !availableResolutions.Contains(size))
                availableResolutions.Add(size);
        }

        Vector2Int currentSize = new(Screen.width, Screen.height);
        // 창 모드의 현재 크기는 모니터의 전체 화면 해상도 목록에 없을 수 있습니다.
        if ((Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen || availableResolutions.Count == 0)
            && currentSize.x > 0 && currentSize.y > 0 && !availableResolutions.Contains(currentSize))
            availableResolutions.Add(currentSize);

        availableResolutions.Sort((left, right) =>
        {
            int width = left.x.CompareTo(right.x);
            return width != 0 ? width : left.y.CompareTo(right.y);
        });
        ResolutionIndex = availableResolutions.IndexOf(currentSize);
    }

    public bool SetResolution(int index)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return false;

        Vector2Int size = availableResolutions[index];
        Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
        ResolutionIndex = index;
        PlayerPrefs.SetInt(WIDTH_KEY, size.x);
        PlayerPrefs.SetInt(HEIGHT_KEY, size.y);
        return true;
    }

    private void RestoreResolution()
    {
        if (!PlayerPrefs.HasKey(WIDTH_KEY) || !PlayerPrefs.HasKey(HEIGHT_KEY))
            return;

        Vector2Int savedSize = new(PlayerPrefs.GetInt(WIDTH_KEY), PlayerPrefs.GetInt(HEIGHT_KEY));
        int index = availableResolutions.IndexOf(savedSize);
        // 모니터가 바뀌어 저장한 해상도를 지원하지 않으면 현재 크기를 유지합니다.
        if (index >= 0)
            SetResolution(index);
    }

}
