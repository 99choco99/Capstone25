using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	public delegate VisualElement DrawerFactory(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter);
}
