using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
	public delegate VisualElement DrawerFactory(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
		MethodParameterDescriptor parameter, object decodedValue);
}
