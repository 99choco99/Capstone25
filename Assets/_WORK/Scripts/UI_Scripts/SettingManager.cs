using UnityEngine;
using UnityEngine.Audio;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;

    [SerializeField] private AudioMixer mainMixer;


    [Header("설정값")]
    public float BGMVolume {  get; private set; }
    public float SFXVolume { get; private set; }
    public float MouseSensitivity {  get; private set; }

    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";
    private const string SENS_KEY = "MouseSensitivity";

    private void Awake()
    {
        if(Instance == null)
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
        BGMVolume = PlayerPrefs.GetFloat(BGM_KEY, 0.5f);
        SFXVolume = PlayerPrefs.GetFloat(SFX_KEY, 0.5f);
        MouseSensitivity = PlayerPrefs.GetFloat(SENS_KEY, 0.3f);

        // (AudioMixer는 데시벨(log) 값을 사용하므로 변환 필요)
        mainMixer.SetFloat("BGMVolume", Mathf.Log10(BGMVolume) * 20);
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(SFXVolume) * 20);
    }

    public void SetBGMVolume(float volume)
    {
        BGMVolume = volume;
        if (volume <= 0.0001f)
        {
            mainMixer.SetFloat("BGMVolume", -80f);
        }
        else
        {
            // 3. 0이 아닐 때만 정상적으로 데시벨 계산
            mainMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);
        }

        PlayerPrefs.SetFloat(BGM_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = volume;
        if (volume <= 0.0001f)
        {
            mainMixer.SetFloat("SFXVolume", -80f);
        }
        else
        {
            // 3. 0이 아닐 때만 정상적으로 데시벨 계산
            mainMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        }
        PlayerPrefs.SetFloat(SFX_KEY, volume);
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        MouseSensitivity = sensitivity;
        PlayerPrefs.SetFloat(SENS_KEY, sensitivity);
    }

}
