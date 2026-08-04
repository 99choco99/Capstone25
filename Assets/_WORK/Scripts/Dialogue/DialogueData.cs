using Newtonsoft.Json;
using System;
using System.Collections.Generic;


[System.Serializable]
public class DialogueLine
{
    public string speakerName;          // 화자 이름
    public string sentence;             // 대사 내용
    public string eventKey;             // 연출을 위한 키
    public List<DialogueChoice> choices; //선택지
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;         //버튼 속 글씨
    public string choiceEventKey;    //어떤 선택을 했는가?
    public string nextDialogueKey;       //다음 대화

    [JsonIgnore]
    public Action onChoiceSelected_Runtime;
}

public class QuestDialogueContext
{
    public string dialogueKey;
    public Action onSubmitAction;
    public string questTitle;
    public string prefix;
}
