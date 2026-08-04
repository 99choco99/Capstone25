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

    private Dictionary<string, Queue<GameObject>> effectPool;
    private List<GameObject> effectObjects;

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
        effectPool = new Dictionary<string, Queue<GameObject>>();
        effectObjects = new List<GameObject>();

        if (effectDB == null)
        {
            Debug.LogError("EffectDB가 할당되지 않았습니다!");
            return;
        }
        effectDB.Initialize();

        foreach (var effectPrefab in effectDB.GetAllEffects())
        {
            Queue<GameObject> queue = new Queue<GameObject>();
            for (int i = 0; i < poolSize; i++)
            {
                GameObject effectInstance = Instantiate(effectPrefab.Prefab, transform.position,Quaternion.identity);
                effectInstance.transform.SetParent(transform);
                effectInstance.SetActive(false);
                effectObjects.Add(effectInstance); // 추적 리스트에 추가
                queue.Enqueue(effectInstance);
            }
            effectPool.Add(effectPrefab.Name, queue);
        }
    }

    public GameObject PlayEffect(string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!effectPool.ContainsKey(name))
        {
            Debug.LogWarning(name + " 이름의 이펙트 풀이 존재하지 않습니다.");
            return null;
        }

        Queue<GameObject> pool = effectPool[name];

        GameObject effectInstance = null;

        while (pool.Count > 0)
        {
            effectInstance = pool.Dequeue();

            if ((effectInstance as UnityEngine.Object) != null)
            {
                break;
            }
            else
            {
                effectInstance = null;
            }
        }

        if (effectInstance == null)
        {
            GameObject prefab = effectDB.GetEffectByName(name);
            if (prefab == null) return null;

            effectInstance = Instantiate(prefab, transform);
            effectObjects.Add(effectInstance);
        }

        Transform effectParent = parent != null ? parent : transform;
        effectInstance.transform.SetParent(effectParent, worldPositionStays: false);
        effectInstance.transform.SetPositionAndRotation(position, rotation);

        // 파티클을 켜기 전에 위치를 먼저 확정해야 풀에서 직전에 사용한 위치에
        // 첫 입자가 한 프레임 보이는 현상을 막을 수 있습니다.
        effectInstance.SetActive(true);

        // 일부 에셋은 루트가 아니라 자식 ParticleSystem만 사용하고 Play On Awake도 꺼져 있습니다.
        // 풀에서 재사용할 때 모든 자식의 이전 입자를 지운 뒤 명시적으로 다시 재생합니다.
        ParticleSystem rootParticle = effectInstance.GetComponent<ParticleSystem>();
        if (rootParticle != null)
        {
            rootParticle.Clear(withChildren: true);
            rootParticle.Play(withChildren: true);
        }

        StartCoroutine(ReturnToPoolAfterPlay(effectInstance, name));
        return effectInstance;
    }

    private IEnumerator ReturnToPoolAfterPlay(GameObject effectInstance, string name)
    {

        if ((effectInstance as UnityEngine.Object) == null)
        {
            yield break; // 시작할 때 이미 파괴됨
        }

        ParticleSystem particle = effectInstance.GetComponent<ParticleSystem>();
        if (particle == null)
        {
            Debug.LogError($"'{name}' 이펙트에 파티클 시스템이 없습니다!");
            yield return new WaitForSeconds(1f);

            if ((effectInstance as UnityEngine.Object) != null && effectInstance.activeSelf)
                ReturnToPool(effectInstance, name);

            yield break;
        }

        yield return new WaitWhile(() => (particle as UnityEngine.Object) != null && particle.IsAlive(true));

        ReturnToPool(effectInstance, name);
    }

    private void ReturnToPool(GameObject effectInstance, string name)
    {
        if ((effectInstance as UnityEngine.Object) == null)
            return;

        // Stop Action이 Disable인 파티클은 재생이 끝나며 스스로 비활성화됩니다.
        // activeSelf를 반환 조건으로 쓰면 이런 이펙트가 풀에서 영구 이탈하므로,
        // 살아 있는 인스턴스는 현재 활성 여부와 무관하게 반드시 큐로 되돌립니다.
        effectInstance.SetActive(false);
        effectInstance.transform.SetParent(transform);
        effectPool[name].Enqueue(effectInstance);
    }
}
