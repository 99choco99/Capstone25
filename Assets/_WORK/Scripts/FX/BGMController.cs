using UnityEngine;
using UnityEngine.SceneManagement;
using SoundEffectManager;

public class BGMController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float transitionDuration = 1.5f;

    private string currentKey;

    void Update()
    {
        Scene scene = SceneManager.GetActiveScene();
        string key = scene.name switch
        {
            SceneName.Login => BgmKeys.Login,
            SceneName.Main => BgmKeys.Main,
            SceneName.Combat => BgmKeys.Combat,
            _ => null
        };

        if (key == null)
        {
            currentKey = null;
            return;
        }

        if (currentKey == key) return;

        currentKey = key;
        SoundManager.Instance.PlayBGM(key, key == BgmKeys.Login ? 0f : transitionDuration * 0.5f);
    }
}
