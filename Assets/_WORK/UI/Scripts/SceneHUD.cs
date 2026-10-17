using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>현재 씬에 맞는 안내 패널을 선택하고, 각 패널의 내용은 변경하지 않습니다.</summary>
public class SceneHUD : MonoBehaviour
{
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject controlsPanel;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        UpdatePanels(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        UpdatePanels(nextScene);
    }

    private void UpdatePanels(Scene scene)
    {
        bool showControls = scene.name == SceneName.Main;
        if (questPanel != null)
            questPanel.SetActive(!showControls);
        if (controlsPanel != null)
            controlsPanel.SetActive(showControls);
    }
}
