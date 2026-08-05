using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphWindow : EditorWindow
{
    private DialogueGraphView graphView;        // 현재 열려있는 그래프
    private DialogueContainer currentContainer; // 현재 열려있는 컨테이너
    private bool isLoading = false;             // 데이터를 불러오는 중인지 체크하는 안전장치



    //========================= 창 여는 법 window or 더블클릭 ==================

    [MenuItem("Window/Dialogue Graph")]
    public static void OpenDialogueGraphWindow()
    {
        //파일 저장 창 띄우기
        string path = EditorUtility.SaveFilePanelInProject("새 다이얼로그 파일 생성", "NewDialogue", "asset", "새 다이얼로그 에셋을 저장할 위치를 선택하세요.");
        
        if (string.IsNullOrEmpty(path)) 
            return; 

        // 에셋을 만들고 디스크에 저장
        DialogueContainer newContainer = CreateInstance<DialogueContainer>();
        AssetDatabase.CreateAsset(newContainer, path);
        AssetDatabase.SaveAssets();

        //창을 띄우고 Load
        DialogueGraphWindow window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window.LoadData(newContainer);
    }

    //에셋 더블클릭시 이벤트
    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int entityId)
    {
        // 에셋의 entityId 를 가져와서 container에 넣기
        if (EditorUtility.EntityIdToObject(entityId) is DialogueContainer container)
        {
            DialogueGraphWindow window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.LoadData(container);
            return true; // 우리가 이 파일을 열었다고 유니티에 보고
        }
        return false;
    }

    //====================== 윈도우 on/off시 =====================

    private void OnEnable()
    {
        ConstructGraphView();
        
        // 단축키 이벤트 등록
        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
        
        // 유니티 내장 Undo 이벤트 구독
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo; // 구독 해제

        if (graphView != null)
        {
            graphView.OnGraphChanged -= Record;

            rootVisualElement.Remove(graphView);
        }
        SaveData();
        AssetDatabase.SaveAssets(); // 창 닫을 때 디스크에 확실히 저장
        rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
    }

    //=================== 기능들 ============================


    /// <summary>
    /// 그래프 뷰 생성
    /// </summary>
    private void ConstructGraphView()
    {
        graphView = new DialogueGraphView { name = "Dialogue Graph" };

        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
        
        // GraphView가 키보드 입력을 받을 수 있도록 포커스 가능 상태로 설정
        graphView.focusable = true;

        graphView.OnGraphChanged += Record;
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.S && evt.actionKey)
        {
            SaveData();
            evt.StopPropagation();      // 이벤트 소모 (다른 단축키 중복 실행 방지)
        }
    }

    /// <summary>
    /// 작업 임시 저장
    /// </summary>
    private void Record()
    {
        if (isLoading) return; // 로딩하면서 그래프 변경을 감지해서 방어책으로 추가

        Undo.RecordObject(currentContainer, "그래프 변경");
        DialogueGraphSerializer.SaveGraphToMemory(graphView, currentContainer);
    }

    /// <summary>
    /// 작업 철회
    /// </summary>
    private void OnUndoRedo()
    {
        if (currentContainer != null)
            LoadData(currentContainer);
    }

    /// <summary>
    /// 그래프를 디스크에 완전 저장
    /// </summary>
    private void SaveData()
    {
        DialogueGraphSerializer.SaveGraphToMemory(graphView, currentContainer);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 그래프 불러오기
    /// </summary>
    private void LoadData(DialogueContainer container)
    {
        isLoading = true;
        
        currentContainer = container;
        DialogueGraphSerializer.LoadGraph(graphView, container);

        isLoading = false;
    }
}
