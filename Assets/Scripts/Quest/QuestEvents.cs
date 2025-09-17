using System;
using UnityEngine;

// 이벤트를 한곳에 모아 관리하는 정적(static) 클래스
public static class QuestEvents
{
    // 퀘스트 시작
    public static event Action<int> OnQuestStarted;
    // 퀘스트 업데이트 (ID, 현재 진행도)
    public static event Action<int, int> OnQuestProgress;
    // 퀘스트 완료
    public static event Action<int> OnQuestCompleted;
    // 퀘스트 해금
    public static event Action<QuestData> OnQuestUnlocked;

    // 이벤트 발생 메서드 (내부적으로 호출)
    public static void QuestStarted(int questId) => OnQuestStarted?.Invoke(questId);
    public static void QuestProgress(int questId, int progress) => OnQuestProgress?.Invoke(questId, progress);
    public static void QuestCompleted(int questId) => OnQuestCompleted?.Invoke(questId);
    public static void QuestUnlocked(QuestData quest) => OnQuestUnlocked?.Invoke(quest);
}