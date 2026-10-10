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
        if (stats == null || stats.Data == null) return;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Enemy Data", stats.Data, typeof(EnemyData), false);
        }

        showSettings = EditorGUILayout.Foldout(showSettings, "적 정책 설정", true);
        if (showSettings)
        {
            EditorGUILayout.HelpBox("같은 EnemyData를 사용하는 적들에게 함께 적용됩니다.", MessageType.Info);
            CreateCachedEditor(stats.Data, null, ref dataEditor);
            dataEditor.OnInspectorGUI();
        }

        EditorGUILayout.Space();
        previewAttack = (EnemyAttackData)EditorGUILayout.ObjectField("거리 확인할 공격", previewAttack, typeof(EnemyAttackData), false);
        distanceTarget = (Transform)EditorGUILayout.ObjectField("거리 측정 대상", distanceTarget, typeof(Transform), true);
        EditorGUILayout.HelpBox("파랑: 유지 간격 / 빨강: 최소 접근 거리 / 노랑: 공격 선택 거리\n노란 원은 실제 명중을 보장하지 않습니다. 흰 원은 무기 판정 위치이며, 판정의 활성 여부와 무관하게 표시됩니다.", MessageType.None);
        if (GUI.changed) SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        EnemyAIController ai = (EnemyAIController)target;
        EnemyStats stats = ai.GetComponent<EnemyStats>();
        if (stats == null || stats.Data == null) return;

        Vector3 origin = ai.transform.position;
        EnemyData data = stats.Data;
        DrawRange(origin, data.strafeDistance, Color.cyan, "유지");
        DrawRange(origin, data.minimumApproachDistance, Color.red, "접근 제한");

        EnemyAttackData attack = previewAttack;
        if (attack == null && Application.isPlaying)
            attack = ai.GetComponent<EnemyCombat>().CurrentAttack;
        if (attack != null)
            DrawRange(origin, attack.MaxDistance, Color.yellow, attack.name);

        Transform targetTransform = distanceTarget;
        if (targetTransform == null && Application.isPlaying && Player.LocalPlayer != null)
            targetTransform = Player.LocalPlayer.transform;
        if (targetTransform != null)
        {
            Vector3 position = targetTransform.position;
            position.y = origin.y;
            Handles.color = Color.white;
            Handles.DrawDottedLine(origin, position, 4f);
            Handles.Label((origin + position) * 0.5f + Vector3.up * 0.2f, $"중심 간격: {Vector3.Distance(origin, position):F2}");
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
                if (point == null) continue;
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
        if (dataEditor != null) DestroyImmediate(dataEditor);
    }
}
