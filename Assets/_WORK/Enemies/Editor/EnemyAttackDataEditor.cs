using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(EnemyAttackData)), CanEditMultipleObjects]
public class EnemyAttackDataEditor : Editor
{
    private const string ControllerPath = "Assets/_WORK/Enemies/Animations/Controllers/EnemyController.controller";
    private const string SlotFolder = "Assets/_WORK/Enemies/Animations/Combat/AttackSlots/";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if ((property.name == "activeStartFrame"
                || property.name == "recoveryStartFrame")
                && AnyTargetHasHitWindows())
            {
                DrawAutomaticFrame(property.name == "activeStartFrame");
                continue;
            }
            if (property.name == "animationStateName")
            {
                DrawAttackSlot(property);
                continue;
            }
            if (property.name == "impactProfile")
            {
                EditorGUILayout.PropertyField(
                    property,
                    new GUIContent("반응 프리셋 (선택)", "특수한 밀림·연출 수치가 필요할 때 기존 공유 프리셋을 지정합니다."));
                continue;
            }
            if (property.name == "LandingSoundKey" || property.name == "LandingEffectKey")
            {
                string label = property.name == "LandingSoundKey" ? "착지 소리 키 (빈 값 = 없음)" : "착지 이펙트 키 (빈 값 = 없음)";
                EditorGUILayout.PropertyField(property, new GUIContent(label));
                continue;
            }
            using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
    }

    private bool AnyTargetHasHitWindows()
    {
        foreach (Object item in targets)
            if (item is EnemyAttackData attack && attack.HasHitWindow && attack.HasSeparateHitWindows)
                return true;
        return false;
    }

    private void DrawAutomaticFrame(bool start)
    {
        var attack = (EnemyAttackData)target;
        float value = start ? attack.ActiveStartFrame : attack.RecoveryStartFrame;
        bool mixed = false;
        foreach (Object item in targets)
        {
            var other = (EnemyAttackData)item;
            if (!Mathf.Approximately(value, start ? other.ActiveStartFrame : other.RecoveryStartFrame))
                mixed = true;
        }

        bool previousMixedValue = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = mixed;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.FloatField(new GUIContent(start ? "타격 판정 시작 (자동)" : "회수 시작 (자동)",
                "무기별 타격 구간의 최초 시작과 최종 종료로 계산합니다. 무기별 타격 구간에서 수정하세요."), value);
        EditorGUI.showMixedValue = previousMixedValue;
    }

    private static void DrawAttackSlot(SerializedProperty property)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorGUILayout.PropertyField(property);
            return;
        }

        var names = new List<string>();
        var labels = new List<GUIContent>();
        foreach (var child in controller.layers[0].stateMachine.states)
        {
            if (child.state.motion is not AnimationClip clip || !AssetDatabase.GetAssetPath(clip).StartsWith(SlotFolder))
                continue;
            names.Add(child.state.name);
            labels.Add(new GUIContent(GetRoleLabel(child.state.name) + " (" + child.state.name + ")"));
        }
        int selected = names.IndexOf(property.stringValue);
        if (selected < 0 || property.hasMultipleDifferentValues)
        {
            names.Insert(0, property.stringValue);
            labels.Insert(
                0,
                new GUIContent(property.hasMultipleDifferentValues
                ? "여러 상태 선택됨"
                : "현재 값: " + property.stringValue));
            selected = 0;
        }

        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int next = EditorGUILayout.Popup(
            new GUIContent("공통 공격 상태", "적별 Override에서 같은 이름의 슬롯에 모션을 지정합니다."),
            selected,
            labels.ToArray());
        if (EditorGUI.EndChangeCheck())
            property.stringValue = names[next];
        EditorGUI.showMixedValue = false;
    }

    private static string GetRoleLabel(string name) => name switch
    {
        "SingleAttack" => "단일 공격",
        "ShortComboOpening" => "짧은 연속 공격 시작",
        "ShortComboFinish" => "짧은 연속 공격 마무리",
        "ComboOpening" => "연속 공격 시작",
        "ComboFollow" => "연속 공격 후속",
        "ComboFollow2" => "연속 공격 두 번째 후속",
        "ComboFinish" => "연속 공격 마무리",
        "FlurryOpening" => "난격 시작",
        "FlurryFollow" => "난격 후속",
        "FlurryFollow2" => "난격 두 번째 후속",
        "FlurryFinish" => "난격 마무리",
        "ApproachAttack" => "멀어졌을 때 접근 공격",
        "RushAttack" => "돌진 공격",
        "AdvancingComboOpening" => "전진 연속 공격 시작",
        "AdvancingComboFollow" => "전진 연속 공격 후속",
        "AdvancingComboFinish" => "전진 연속 공격 마무리",
        "EvasiveAttack" => "회피 공격",
        "CounterAttack" => "반격",
        "CounterFollow" => "반격 후속",
        "CounterComboAttack" => "연계 가능한 반격",
        "PerilousPrepare" => "위협 공격 준비",
        "PerilousHeavyAttack" => "위협 강공격",
        "PerilousAttack" => "위협 공격",
        "PerilousFinish" => "위협 공격 후속 마무리",
        "DelayedAttack" => "엇박자 공격",
        "Retreat" => "후퇴",
        "ReenterAttack" => "후퇴 후 재진입 공격",
        _ => name
    };
}
