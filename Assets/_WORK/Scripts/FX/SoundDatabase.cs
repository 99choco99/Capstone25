using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip; // 실제 오디오 파일
}

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Scriptable Objects/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    [SerializeField]
    private List<Sound> sfxList;
    private Dictionary<string, AudioClip> sfxDictionary;

    public void Initialize()
    {
        sfxDictionary = new Dictionary<string, AudioClip>();

        foreach (var sound in sfxList)
        {
            if (!sfxDictionary.ContainsKey(sound.name))
            {
                sfxDictionary.Add(sound.name, sound.clip);
            }
        }
    }

    public AudioClip GetAudio(string soundName)
    {
        if (sfxDictionary == null)
        {
            Initialize();
        }

        if (sfxDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            return clip;
        }

        Debug.LogWarning(soundName + ": 해당 사운드를 SoundDatabase에서 찾을 수 없습니다.");
        return null;
    }
}