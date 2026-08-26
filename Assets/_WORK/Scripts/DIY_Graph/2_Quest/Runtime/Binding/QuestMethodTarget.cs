namespace UniversalGraph
{
    /// <summary>Attribute가 붙은 Quest 메서드를 호출할 런타임 대상입니다.</summary>
    public enum QuestMethodTarget
    {
        /// <summary>현재 IQuestController 객체의 인스턴스 메서드를 호출합니다.</summary>
        Controller,

        /// <summary>Controller 인스턴스가 필요 없는 static 메서드를 호출합니다.</summary>
        Global
    }
}
