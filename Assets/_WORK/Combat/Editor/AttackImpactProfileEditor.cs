using UnityEditor;

[CustomEditor(typeof(AttackImpactProfile)), CanEditMultipleObjects]
public sealed class AttackImpactProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty knockbackOverride = serializedObject.FindProperty("OverrideKnockback");
        SerializedProperty reactionOverride = serializedObject.FindProperty("OverrideReactionSettings");
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            bool disabled = property.name == "m_Script"
                || property.name == "Knockback" && !knockbackOverride.hasMultipleDifferentValues && !knockbackOverride.boolValue
                || property.name == "Reaction" && !reactionOverride.hasMultipleDifferentValues && !reactionOverride.boolValue;
            using (new EditorGUI.DisabledScope(disabled))
                EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
