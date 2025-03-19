using Unity.Netcode.Components;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="PlayerCubeController"/> component.
/// </summary>
[CustomEditor(typeof(PlayerCubeController), true)]
public class PlayerCubeControllerEditor : NetworkTransformEditor
{
    private SerializedProperty m_Speed;
    private SerializedProperty m_ApplyVerticalInputToZAxis;

    public override void OnEnable()
    {
        m_Speed = serializedObject.FindProperty(nameof(PlayerCubeController.Speed));
        m_ApplyVerticalInputToZAxis = serializedObject.FindProperty(nameof(PlayerCubeController.ApplyVerticalInputToZAxis));
        base.OnEnable();
    }

    private void DisplayPlayerCubeControllerProperties()
    {
        EditorGUILayout.PropertyField(m_Speed);
        EditorGUILayout.PropertyField(m_ApplyVerticalInputToZAxis);
    }

    public override void OnInspectorGUI()
    {
        var playerCubeController = target as PlayerCubeController;
        void SetExpanded(bool expanded) { playerCubeController.PlayerCubeControllerPropertiesVisible = expanded; };
        DrawFoldOutGroup<PlayerCubeController>(playerCubeController.GetType(), DisplayPlayerCubeControllerProperties, playerCubeController.PlayerCubeControllerPropertiesVisible, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif


public class PlayerCubeController : NetworkTransform
{
#if UNITY_EDITOR
    // These bool properties ensure that any expanded or collapsed property views
    // within the inspector view will be saved and restored the next time the
    // asset/prefab is viewed.
    public bool PlayerCubeControllerPropertiesVisible;
#endif
    public float Speed = 10;
    public bool ApplyVerticalInputToZAxis;
    private Vector3 m_Motion;

}
