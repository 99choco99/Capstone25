using UnityEngine;
using UnityEngine.Audio;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;

    [SerializeField] private AudioMixer mainMixer;
    [SerializeField, Range(0f, 1f)] private float defaultBGMVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 0.5f;

    [Header("설정값")]
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float MouseSensitivity { get; private set; }

    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";
    private const string SENS_KEY = "MouseSensitivity";
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
    }

    private void Start()
    {
        // (AudioMixer는 데시벨(log) 값을 사용하므로 변환 필요)
        SetBGMVolume(BGMVolume);
        SetSFXVolume(SFXVolume);
    }

    public void SetBGMVolume(float volume)
    {
        BGMVolume = volume;
        // 3. 0이 아닐 때만 정상적으로 데시벨 계산
        mainMixer.SetFloat(BGM_KEY, ToDecibels(volume));

        PlayerPrefs.SetFloat(BGM_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = volume;
        // 3. 0이 아닐 때만 정상적으로 데시벨 계산
        mainMixer.SetFloat(SFX_KEY, ToDecibels(volume));
        PlayerPrefs.SetFloat(SFX_KEY, volume);
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        MouseSensitivity = sensitivity;
        PlayerPrefs.SetFloat(SENS_KEY, sensitivity);
    }

    private static float ToDecibels(float volume)
    {
        return volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20;
    }
}
