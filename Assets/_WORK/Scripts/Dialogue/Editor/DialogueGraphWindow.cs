using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class DialogueGraphWindow : EditorWindow
{
    private DialogueGraphView graphView;
    private DialogueContainer currentContainer; // 현재 열려있는 다이얼로그 에셋

    [MenuItem("Window/Dialogue Graph")]
    public static void OpenDialogueGraphWindow()
    {
        var window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    // 🚀 더블클릭 이벤트 가로채기 (상용 에셋 방식)
    [UnityEditor.Callbacks.OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceId)
    {
        // 더블클릭한 파일이 DialogueContainer인지 확인
        if (EditorUtility.InstanceIDToObject(instanceId) is DialogueContainer container)
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.LoadDialogue(container);
            return true; // 우리가 이 파일을 열었다고 유니티에 보고
        }
        return false;
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    private void OnDisable()
    {
        if (graphView != null)
        {
            rootVisualElement.Remove(graphView);
        }
    }

    private void ConstructGraphView()
    {
        graphView = new DialogueGraphView { name = "Dialogue Graph" };

        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        // 저장 버튼 (이제 파일 이름 텍스트 칸은 필요 없습니다!)
        Button saveDataButton = new(() => SaveData()) { text = "Save Data" };
        toolbar.Add(saveDataButton);

        rootVisualElement.Add(toolbar);
    }

    private void SaveData()
    {
        if (currentContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "열려있는 다이얼로그 파일이 없습니다!", "OK");
            return;
        }

        var utility = new GraphUtility(graphView);
        utility.SaveGraph(currentContainer);
    }


    private void LoadDialogue(DialogueContainer container)
    {
        currentContainer = container;

        var utility = new GraphUtility(graphView);
        utility.LoadGraph(container);
    }
}
