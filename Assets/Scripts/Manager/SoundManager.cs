using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("사운드 데이터베이스")]
    [SerializeField] private SoundDatabase soundDB;

    [Header("오디오 소스 설정")]
    [SerializeField] private AudioSource bgmPlayer;
    [SerializeField] private AudioSource sfxPlayerPrefab;
    [SerializeField] private int sfxPoolSize = 15;

    private List<AudioSource> _sfxPlayers;
    private Dictionary<string, AudioSource> _loopingSfxPlayers;

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
        // 사운드 데이터베이스 초기화
        if (soundDB != null)
        {
            soundDB.Initialize();
        }


        _sfxPlayers = new List<AudioSource>();
        _loopingSfxPlayers = new Dictionary<string, AudioSource>();
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource sfxPlayer = Instantiate(sfxPlayerPrefab, transform);
            sfxPlayer.gameObject.SetActive(false);
            _sfxPlayers.Add(sfxPlayer);
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        bgmPlayer.clip = clip;
        bgmPlayer.loop = true;
        bgmPlayer.Play();
    }

    // PlaySFX는 이제 AudioClip이 아닌, 사운드 파일의 '이름(string)'을 받습니다.
    public void PlaySFX(string clipName, Vector3? position = null)
    {
        // 1. 데이터베이스에게 이름으로 오디오 클립을 물어봅니다.
        AudioClip clipToPlay = soundDB.GetClipByName(clipName);
        if (clipToPlay == null) return; // 클립이 없으면 재생하지 않음

        // 2. 사용 가능한 오디오 소스(재생기)를 찾습니다.
        AudioSource availablePlayer = GetAvailableSFXPlayer();
        if (availablePlayer == null) return; // 모든 재생기가 사용 중이면 재생하지 않음

        // 3. 찾은 재생기로 사운드를 재생합니다.
        availablePlayer.gameObject.SetActive(true);
        if (position.HasValue)
        {
            availablePlayer.transform.position = position.Value;
            availablePlayer.spatialBlend = 1.0f; // 3D
        }
        else
        {
            availablePlayer.spatialBlend = 0.0f; // 2D
        }

        availablePlayer.clip = clipToPlay;
        availablePlayer.Play();

        StartCoroutine(ReturnToPoolAfterPlay(availablePlayer));
    }

    public void PlayLoopingSFX(string clipName)
    {
        // 1. 이 사운드가 이미 재생 중인지 확인
        if (_loopingSfxPlayers.ContainsKey(clipName))
        {
            return; // 이미 재생 중이면 아무것도 안 함
        }

        AudioClip clipToPlay = soundDB.GetClipByName(clipName);
        if (clipToPlay == null) return;

        AudioSource availablePlayer = GetAvailableSFXPlayer();
        if (availablePlayer == null) return;

        // 2. 재생기를 설정하고 '반복(loop)' 옵션을 켬
        availablePlayer.gameObject.SetActive(true);
        availablePlayer.clip = clipToPlay;
        availablePlayer.loop = true; // 반복 재생 활성화
        availablePlayer.Play();

        // 3. '현재 재생 중' 목록에 추가하여 상태를 기록
        _loopingSfxPlayers.Add(clipName, availablePlayer);
    }

    public void StopLoopingSFX(string clipName)
    {
        // 1. 이 사운드가 재생 중인지 목록에서 확인
        if (_loopingSfxPlayers.TryGetValue(clipName, out AudioSource player))
        {
            // 2. 재생을 멈추고 풀에 반납
            player.Stop();
            player.loop = false;
            player.gameObject.SetActive(false);

            // 3. '현재 재생 중' 목록에서 제거
            _loopingSfxPlayers.Remove(clipName);
        }
    }

    private AudioSource GetAvailableSFXPlayer()
    {
        foreach (var player in _sfxPlayers)
        {
            if (!player.gameObject.activeSelf) return player;
        }
        return null;
    }

    private IEnumerator ReturnToPoolAfterPlay(AudioSource player)
    {
        yield return new WaitForSeconds(player.clip.length);
        player.gameObject.SetActive(false);
    }
}