using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Sound
{
    public string name; // 우리가 코드에서 사용할 '별명' (Key)
    public AudioClip clip; // 실제 오디오 파일 (Value)
}

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Scriptable Objects/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    // AudioClip 리스트 대신, 위에서 만든 Sound 클래스의 리스트를 사용합니다.
    [SerializeField]
    private List<Sound> sfxList;

    private Dictionary<string, AudioClip> _sfxDictionary;

    public void Initialize()
    {
        _sfxDictionary = new Dictionary<string, AudioClip>();

        // sfxList를 순회하며 딕셔너리를 채웁니다.
        foreach (var sound in sfxList)
        {
            // 이제 파일 이름(sound.clip.name)이 아닌, 우리가 직접 지정한 별명(sound.name)을 키로 사용합니다.
            if (!_sfxDictionary.ContainsKey(sound.name))
            {
                _sfxDictionary.Add(sound.name, sound.clip);
            }
        }
    }

    public AudioClip GetClipByName(string soundName)
    {
        if (_sfxDictionary == null)
        {
            Initialize();
        }

        if (_sfxDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            return clip;
        }

        Debug.LogWarning(soundName + " 이름의 사운드를 SoundDatabase에서 찾을 수 없습니다.");
        return null;
    }
}