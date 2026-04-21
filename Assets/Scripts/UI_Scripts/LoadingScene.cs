
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    static string nextScene;

    [SerializeField] Slider slider;

    public static async Awaitable LoadScene(string sceneName)
    {
        nextScene = sceneName;
        await SceneManager.LoadSceneAsync(SceneName.Loading);

        while(SceneManager.GetActiveScene().name != nextScene){
            await Awaitable.NextFrameAsync();
        }
    }

    private void Start()
    {
        slider.value = 0;
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
        while (!op.isDone)
        {
            yield return null;

            if (op.progress < 0.9f)
            {
                slider.value = Mathf.Lerp(slider.value, op.progress, timer);
                if (slider.value >= op.progress) timer = 0f;
            }
            else
            {
                timer += Time.unscaledDeltaTime;
                slider.value = Mathf.Lerp(0.9f, 1f, timer);
                if (slider.value >= 1f)
                {
                    slider.value = 1f;
                    yield return new WaitForSecondsRealtime(1f);

                    op.allowSceneActivation = true;
                    yield break;
                }
            }
        }
    }
}
