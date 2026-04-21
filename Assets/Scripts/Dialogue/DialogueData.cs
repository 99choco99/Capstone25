using System.Collections.Generic;


[System.Serializable]
public class DialogueLine
{
    public string speakerName; // 화자 이름
    public string sentence;    // 대사 내용
    public string action;      // NPC 행동
    public List<DialogueChoice> choices; //선택지
}

[System.Serializable]
public class DialogueChoice
{
    public string text;         //버튼 속 글씨
    public string action;       //버튼 누르면 실행할 명령어
    public string nextId;       //다음 대화
}

