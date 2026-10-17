using UnityEngine;
using UnityEngine.SceneManagement;
using SoundEffectManager;

public class BGMController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float transitionDuration = 1.5f;

    private string currentKey;
    private string sceneKey;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        UpdateSceneKey(SceneManager.GetActiveScene());
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        UpdateSceneKey(nextScene);
    }

    private void UpdateSceneKey(Scene scene)
    {
        sceneKey = scene.name switch
        {
            SceneName.Login => BgmKeys.Login,
            SceneName.Main => BgmKeys.Main,
            SceneName.Combat => BgmKeys.Combat,
            _ => null
        };
    }

    void Update()
    {
        string key = sceneKey;
        if (key == null)
        {
            currentKey = null;
            return;
        }

        SoundManager soundManager = SoundManager.Instance;
        if (soundManager == null || !soundManager.isActiveAndEnabled)
        {
            currentKey = null;
            return;
        }

        if (currentKey == key)
            return;

        currentKey = key;
        soundManager.PlayBGM(key, key == BgmKeys.Login ? 0f : transitionDuration * 0.5f);
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        currentKey = null;
    }
}
