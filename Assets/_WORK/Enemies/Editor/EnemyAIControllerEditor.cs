using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAIController))]
public class EnemyAIControllerEditor : Editor
{
    private Editor dataEditor;
    private EnemyAttackData previewAttack;
    private Transform distanceTarget;
    private bool showSettings;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EnemyStats stats = ((EnemyAIController)target).GetComponent<EnemyStats>();
        if (stats == null || stats.Data == null)
            return;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Enemy Data", stats.Data, typeof(EnemyData), false);
            if (Application.isPlaying)
            {
                EnemyAIController ai = (EnemyAIController)target;
                EditorGUILayout.ObjectField("예약한 공격", ai.ReservedAttack, typeof(EnemyAttackData), false);
                EditorGUILayout.FloatField("공격 준비 남은 시간", ai.PreparationTimeRemaining);
                EditorGUILayout.Toggle("후퇴 후 예약 공격 준비 중", ai.IsPreparingRetreat);
                EditorGUILayout.ObjectField("마지막 시작 패턴", ai.LastStartedPattern, typeof(EnemyAttackData), false);
                foreach (EnemyAttackRole role in System.Enum.GetValues(typeof(EnemyAttackRole)))
                {
                    if (role == EnemyAttackRole.None)
                        continue;
                    EditorGUILayout.IntField($"실제 시작 횟수 · {role}", ai.GetExecutedPatternCount(role));
                }
            }
        }

        showSettings = EditorGUILayout.Foldout(showSettings, "적 정책 설정", true);
        if (showSettings)
        {
            EditorGUILayout.HelpBox("같은 EnemyData를 사용하는 적들에게 함께 적용됩니다.", MessageType.Info);
            CreateCachedEditor(stats.Data, null, ref dataEditor);
            dataEditor.OnInspectorGUI();
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("공격 거리 자료 다시 측정"))
                CombatReachBaker.Rebuild();
        previewAttack = (EnemyAttackData)EditorGUILayout.ObjectField(
            "거리 확인할 공격",
            previewAttack,
            typeof(EnemyAttackData),
            false);
        DrawMeasuredReach((EnemyAIController)target, previewAttack);
        distanceTarget = (Transform)EditorGUILayout.ObjectField("거리 측정 대상", distanceTarget, typeof(Transform), true);
        EditorGUILayout.HelpBox(
            "빨강: 최소 접근 거리 / 노랑: 공격 시작 허용 범위 / 초록: 거리 조정 목표\n견제 중에는 현재 간격에서 횡이동하고, 선택한 공격의 허용 범위에 들어오면 공격합니다. 흰 원은 무기 판정 위치이며, 판정의 활성 여부와 무관하게 표시됩니다.",
            MessageType.None);
        if (GUI.changed)
            SceneView.RepaintAll();
    }

    private static void DrawMeasuredReach(EnemyAIController ai, EnemyAttackData preview)
    {
        EnemyAttackData attack = preview != null ? preview : ai.ReservedAttack;
        if (attack == null)
            return;
        var database = AssetDatabase.LoadAssetAtPath<CombatReachDatabase>(CombatReachBaker.AssetPath);
        var actor = database != null && database.version == CombatReachDatabase.FormatVersion
            ? database.Find(ai.GetComponent<EnemyStats>().Data, ai.transform) : null;
        var reach = actor?.attacks.Find(entry => entry.data == attack);
        if (reach == null)
        {
            EditorGUILayout.HelpBox("현재 캐릭터/무기의 측정 자료가 없습니다. 공격 거리 자료를 다시 측정하세요.", MessageType.Info);
            return;
        }
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("측정한 원본 모션", reach.clip, typeof(AnimationClip), false);
            foreach (var band in reach.bands)
                EditorGUILayout.Vector2Field("정지 대상 타격 가능 구간", new Vector2(band.min, band.max));
        }
        if (reach.bands.Count == 0)
            EditorGUILayout.HelpBox(
                "고정 방향의 원본 모션 측정에서 정면 타격 구간이 없습니다. 후퇴 동작인지, 판정 프레임과 회전 추적이 필요한 공격인지 확인하세요.",
                MessageType.Warning);
        EditorGUILayout.HelpBox(
            "측정값은 등록된 대상 캡슐과 원본 모션 기준이며 회전 추적·보조 전진을 제외한 검토 자료입니다. 실제 공격 시작 거리는 AttackData의 허용 범위를 사용합니다.",
            MessageType.None);
    }

    private void OnSceneGUI()
    {
        EnemyAIController ai = (EnemyAIController)target;
        EnemyStats stats = ai.GetComponent<EnemyStats>();
        if (stats == null || stats.Data == null)
            return;

        Vector3 origin = ai.transform.position;
        EnemyData data = stats.Data;
        DrawRange(origin, data.minimumApproachDistance, Color.red, "접근 제한");

        EnemyAttackData attack = previewAttack;
        if (attack == null && Application.isPlaying)
            attack = ai.ReservedAttack != null ? ai.ReservedAttack : ai.GetComponent<EnemyCombat>().CurrentAttack;
        if (attack != null)
        {
            DrawRange(origin, attack.StartMinDistance, Color.yellow, "시작 최소");
            DrawRange(origin, attack.StartMaxDistance, Color.yellow, "시작 최대");
            DrawRange(origin, attack.PreferredStartDistance, Color.green, "공격 목표");
        }

        Transform targetTransform = distanceTarget;
        if (targetTransform == null && Application.isPlaying && Player.LocalPlayer != null)
            targetTransform = Player.LocalPlayer.transform;
        if (targetTransform != null)
        {
            Vector3 position = targetTransform.position;
            position.y = origin.y;
            Handles.color = Color.white;
            Handles.DrawDottedLine(origin, position, 4f);
            Handles.Label(
                (origin + position) * 0.5f + Vector3.up * 0.2f,
                $"중심 간격: {Vector3.Distance(origin, position):F2}");
        }

        Handles.color = Color.white;
        foreach (Weapon weapon in ai.GetComponentsInChildren<Weapon>())
        {
            using SerializedObject serializedWeapon = new(weapon);
            SerializedProperty points = serializedWeapon.FindProperty("hitPoints");
            float radius = serializedWeapon.FindProperty("hitCheckRadius").floatValue;
            for (int i = 0; i < points.arraySize; i++)
            {
                Transform point = points.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (point == null)
                    continue;
                Handles.DrawWireDisc(point.position, Vector3.up, radius);
                Handles.DrawWireDisc(point.position, Vector3.right, radius);
            }
        }
    }

    private static void DrawRange(Vector3 origin, float radius, Color color, string label)
    {
        Handles.color = color;
        Handles.DrawWireDisc(origin, Vector3.up, radius);
        Handles.Label(origin + Vector3.forward * radius, $"{label}: {radius:F2}");
    }

    private void OnDisable()
    {
        if (dataEditor != null)
            DestroyImmediate(dataEditor);
    }
}
