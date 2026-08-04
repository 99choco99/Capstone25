using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("사운드 데이터베이스")]
    [SerializeField] private SoundDatabase soundDB;

    [Header("오디오 소스 설정")]
    [Tooltip("bgm 플레이어")]
    [SerializeField] private AudioSource bgmPlayer;
    [Tooltip("효과음 플레이어")]
    [SerializeField] private AudioSource sfxPlayerPrefab;

    private List<AudioSource> sfxPlayers;

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
            return;
        }
    }

    private void InitializeManager()
    {
        if (soundDB != null)
        {
            soundDB.Initialize();
        }

        sfxPlayers = new List<AudioSource>();
    }

    public void PlayBGM(string clipName)
    {
        if (soundDB == null || bgmPlayer == null) return;

        AudioClip clip = soundDB.GetAudio(clipName);
        if (clip == null) return;
        bgmPlayer.clip = clip;
        bgmPlayer.loop = true;
        bgmPlayer.Play();
    }

    public void PlaySFX(string clipName)
    {
        PlaySFXInternal(clipName, transform.position, spatial: false);
    }

    /// <summary>
    /// 전투가 일어난 월드 위치에서 3D 효과음을 재생합니다.
    /// 카메라와 거리가 멀어질수록 자연스럽게 작아지므로 공격 주체를 귀로도 구분할 수 있습니다.
    /// </summary>
    public void PlaySFXAtPoint(string clipName, Vector3 worldPosition)
    {
        PlaySFXInternal(clipName, worldPosition, spatial: true);
    }

    private void PlaySFXInternal(string clipName, Vector3 worldPosition, bool spatial)
    {
        if (soundDB == null) return;

        AudioClip clip = soundDB.GetAudio(clipName);
        if (clip == null) return;

        //AudioPlayer 찾기
        AudioSource availablePlayer = GetAvailablePlayer();
        if (availablePlayer == null) return;

        availablePlayer.gameObject.SetActive(true);
        availablePlayer.transform.position = spatial ? worldPosition : transform.position;
        availablePlayer.spatialBlend = spatial ? 1f : 0f;
        availablePlayer.rolloffMode = AudioRolloffMode.Linear;
        availablePlayer.minDistance = 1.5f;
        availablePlayer.maxDistance = 25f;
        availablePlayer.clip = clip;
        availablePlayer.Play();

        StartCoroutine(ReturnToPoolAfterPlay(availablePlayer));
    }

    /// <summary>
    /// 현재 사용 가능한 오디오 플레이어 가져오기
    /// </summary>
    private AudioSource GetAvailablePlayer()
    {
        foreach (var player in sfxPlayers)
        {
            if (player != null && !player.gameObject.activeSelf)
                return player;
        }
        return CreateNewSFXPlayer();
    }

    /// <summary>
    /// 오디오 플레이어 부족하면 새로 추가
    /// </summary>
    private AudioSource CreateNewSFXPlayer()
    {
        if (sfxPlayerPrefab == null)
        {
            Debug.LogWarning("SoundManager: SFX Player Prefab이 연결되지 않았습니다.", this);
            return null;
        }

        AudioSource newPlayer = Instantiate(sfxPlayerPrefab, transform);
        newPlayer.gameObject.SetActive(false);
        sfxPlayers.Add(newPlayer);
        return newPlayer;
    }

    /// <summary>
    /// 플레이어 사용 후 반납
    /// </summary>
    private IEnumerator ReturnToPoolAfterPlay(AudioSource player)
    {
        yield return new WaitWhile(() => player != null && player.isPlaying);

        if (player != null)
        {
            // 다음 사용이 2D 효과음일 수도 있으므로 풀에 돌려보내기 전에 공간 설정을 초기화합니다.
            player.Stop();
            player.clip = null;
            player.spatialBlend = 0f;
            player.transform.position = transform.position;
            player.gameObject.SetActive(false);
        }
    }
}
