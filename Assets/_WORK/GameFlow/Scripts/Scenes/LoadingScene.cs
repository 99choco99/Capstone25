using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScene : MonoBehaviour
{
    static string nextScene;

    [SerializeField] Slider slider;
    [SerializeField] private TextMeshProUGUI sceneLabel;
    [SerializeField] private TextMeshProUGUI hintLabel;
    [SerializeField] private TextMeshProUGUI progressLabel;
    [SerializeField, Min(0.01f)] private float completionFillDuration = 1f;
    [SerializeField, Min(0f)] private float completionHoldDuration = 1f;

    public static async Awaitable LoadScene(string sceneName)
    {
        nextScene = sceneName;
        await SceneManager.LoadSceneAsync(SceneName.Loading);

        while (SceneManager.GetActiveScene().name != nextScene)
        {
            await Awaitable.NextFrameAsync();
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(nextScene))
            nextScene = SceneName.Main;
        sceneLabel.text = nextScene == SceneName.Login ? "타이틀"
            : nextScene == SceneName.Combat ? "무너진 회랑" : "고요한 행랑";
        hintLabel.text = nextScene == SceneName.Login
            ? "이어하기로 저장한 성장 기록을 이어갈 수 있습니다."
            : nextScene == SceneName.Combat
            ? "주변을 살피며 전진하세요. 완료한 의뢰는 길잡이에게 보고할 수 있습니다."
            : "길잡이에게 다가가 F로 대화하세요. P는 능력치, O는 설정입니다.";
        slider.value = 0;
        progressLabel.text = "0%";
        StartCoroutine(LoadSceneProcess());
    }

    IEnumerator LoadSceneProcess()
    {
        Debug.Log("로딩 시작");
        //System.GC.Collect();
        //yield return Resources.UnloadUnusedAssets();
        //Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.Low;
        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        float timer = 0f;
        int displayedProgress = 0;
        while (!op.isDone)
        {
            yield return null;

            if (op.progress < 0.9f)
            {
                slider.value = op.progress;
            }
            else
            {
                timer += Time.unscaledDeltaTime;
                slider.value = Mathf.Lerp(0.9f, 1f, timer / completionFillDuration);
                if (slider.value >= 1f)
                {
                    slider.value = 1f;
                    progressLabel.text = "100%";
                    yield return new WaitForSecondsRealtime(completionHoldDuration);

                    op.allowSceneActivation = true;
                    yield break;
                }
            }

            int progress = Mathf.RoundToInt(slider.normalizedValue * 100f);
            if (progress != displayedProgress)
            {
                displayedProgress = progress;
                progressLabel.text = $"{progress}%";
            }
        }
    }
}
