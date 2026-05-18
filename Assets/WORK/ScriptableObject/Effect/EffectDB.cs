using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Effect
{
    public string Name;
    public GameObject Prefab;
}



[CreateAssetMenu(fileName = "EffectDB", menuName = "Scriptable Objects/EffectDB")]
public class EffectDB : ScriptableObject
{
    [SerializeField]
    private List<Effect> effectList;

    private Dictionary<string, GameObject> _effectDictionary;

    public void Initialize()
    {
        _effectDictionary = new Dictionary<string, GameObject>();
        foreach (var effect in effectList)
        {
            if (!_effectDictionary.ContainsKey(effect.Name))
            {
                _effectDictionary.Add(effect.Name, effect.Prefab);
            }
        }
    }

    public GameObject GetEffectByName(string effectName)
    {
        if (_effectDictionary == null)
        {
            Initialize();
        }

        if (_effectDictionary.TryGetValue(effectName, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogWarning(effectName + " 이름의 이펙트를 EffectDatabase에서 찾을 수 없습니다.");
        return null;
    }

    public IEnumerable<Effect> GetAllEffects()
    {
        return effectList;
    }
}
