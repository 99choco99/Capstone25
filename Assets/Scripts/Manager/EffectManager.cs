using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("이펙트 데이터베이스")]
    [SerializeField] private EffectDB effectDB;

    [Header("오브젝트 풀 설정")]
    [SerializeField] private int poolSize = 15; // 풀(Pool)의 기본 크기

    // 이펙트 종류별로 별도의 풀(Queue)을 관리합니다.
    private Dictionary<string, Queue<GameObject>> _effectPool;
    // 생성된 모든 이펙트 오브젝트를 추적하기 위한 리스트
    private List<GameObject> _pooledObjects;

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
            return;
        }
    }

    private void InitializePool()
    {
        _effectPool = new Dictionary<string, Queue<GameObject>>();
        _pooledObjects = new List<GameObject>();

        if (effectDB == null)
        {
            Debug.LogError("EffectDB가 할당되지 않았습니다!");
            return;
        }
        effectDB.Initialize();

        // 데이터베이스에 있는 모든 종류의 이펙트에 대해 풀을 생성합니다.
        foreach (var effectPrefab in effectDB.GetAllEffects()) // GetAllEffects() 메서드가 DB에 필요합니다.
        {
            Queue<GameObject> queue = new Queue<GameObject>();
            for (int i = 0; i < poolSize; i++)
            {
                GameObject effectInstance = Instantiate(effectPrefab.Prefab, transform.position,Quaternion.identity);
                effectInstance.transform.SetParent(transform);
                effectInstance.SetActive(false);
                _pooledObjects.Add(effectInstance); // 추적 리스트에 추가
                queue.Enqueue(effectInstance);
            }
            _effectPool.Add(effectPrefab.Name, queue);
        }
    }

    public GameObject PlayEffect(string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!_effectPool.ContainsKey(name))
        {
            Debug.LogWarning(name + " 이름의 이펙트 풀이 존재하지 않습니다.");
            return null;
        }

        Queue<GameObject> pool = _effectPool[name];

        // 1. 풀에서 사용 가능한 이펙트를 가져옵니다.
        GameObject effectInstance;
        if (pool.Count > 0)
        {
            effectInstance = pool.Dequeue();
            effectInstance.SetActive(true);
        }
        else // 만약 풀이 비었다면 새로 생성 (비상용)
        {
            GameObject prefab = effectDB.GetEffectByName(name);
            if (prefab == null) return null;

            effectInstance = Instantiate(prefab, transform);
            _pooledObjects.Add(effectInstance); // 새로 만든 것도 추적
            Debug.LogWarning(name + " 이펙트 풀이 부족하여 새로 생성합니다. Pool Size를 늘리는 것을 고려해보세요.");
        }

        effectInstance.transform.SetPositionAndRotation(position, rotation);

        if (parent != null)
        {
            effectInstance.transform.SetParent(parent);
        }

        // 2. 파티클 재생이 끝나면 자동으로 풀에 반환되도록 코루틴 실행
        StartCoroutine(ReturnToPoolAfterPlay(effectInstance, name));
        return effectInstance;
    }

    private IEnumerator ReturnToPoolAfterPlay(GameObject effectInstance, string name)
    {
        ParticleSystem particle = effectInstance.GetComponent<ParticleSystem>();
        if (particle == null)
        {
            Debug.LogError($"'{name}' 이펙트에 파티클 시스템이 없습니다!");
            // 파티클이 없으면 1초 뒤에 그냥 반납
            yield return new WaitForSeconds(1f);
            if (effectInstance.activeSelf)
                ReturnToPool(effectInstance, name);
            yield break;
        }

        // 파티클이 살아있는 동안 대기 (null 체크 추가)
        yield return new WaitWhile(() => particle != null && particle.IsAlive(true));

        ReturnToPool(effectInstance, name);
    }

    private void ReturnToPool(GameObject effectInstance, string name)
    {
        if (effectInstance != null && effectInstance.activeSelf)
        {
            effectInstance.SetActive(false);
            _effectPool[name].Enqueue(effectInstance);
        }
    }
}