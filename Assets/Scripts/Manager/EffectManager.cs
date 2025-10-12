using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("이펙트 프리팹 리스트")]
    [SerializeField] private List<Effect> effectPrefabs;

    // 오브젝트 풀. (이펙트 이름, 해당 이펙트의 풀 Queue)
    private Dictionary<string, Queue<GameObject>> _effectPool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 미리 정의된 모든 이펙트 프리팹에 대해 빈 풀(Queue)을 생성
    private void InitializePool()
    {
        _effectPool = new Dictionary<string, Queue<GameObject>>();
        foreach (var effect in effectPrefabs)
        {
            _effectPool.Add(effect.Name, new Queue<GameObject>());
        }
    }

    /// <summary>
    /// 지정된 위치와 방향으로 이펙트를 재생합니다.
    /// </summary>
    public void PlayEffect(string name, Vector3 position, Quaternion rotation)
    {
        // 1. 이펙트 프리팹 찾기
        GameObject effectPrefab = effectPrefabs.Find(e => e.Name == name)?.Prefab;
        if (effectPrefab == null)
        {
            Debug.LogWarning(name + " 이름의 이펙트 프리팹을 찾을 수 없습니다.");
            return;
        }

        GameObject effectInstance;
        Queue<GameObject> pool = _effectPool[name];

        // 2. 풀에 사용 가능한 이펙트가 있는지 확인
        if (pool.Count > 0)
        {
            effectInstance = pool.Dequeue(); // 풀에서 하나 꺼내옴
            effectInstance.SetActive(true);
        }
        else
        {
            effectInstance = Instantiate(effectPrefab); // 풀이 비어있으면 새로 생성
        }

        effectInstance.transform.SetPositionAndRotation(position, rotation);

        // 3. 파티클 재생이 끝나면 자동으로 풀에 반환되도록 코루틴 실행
        StartCoroutine(ReturnToPoolAfterPlay(effectInstance, name));
    }

    private IEnumerator ReturnToPoolAfterPlay(GameObject effectInstance, string name)
    {
        ParticleSystem particle = effectInstance.GetComponent<ParticleSystem>();

        // 파티클 시스템의 총 재생 시간만큼 기다림
        yield return new WaitForSeconds(particle.main.duration + particle.main.startLifetime.constantMax);

        effectInstance.SetActive(false);
        _effectPool[name].Enqueue(effectInstance); // 다시 풀에 넣음
    }


}