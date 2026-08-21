using System.ComponentModel;

namespace UniversalGraph
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class DialogueGeneratedParameterRegistration
	{
		public string ParameterId { get; }

		public string DisplayName { get; }

		public string TypeMetadataName { get; }

		public string TypeAssemblyName { get; }

		public DialogueGeneratedParameterRegistration(string parameterId, string displayName, string typeMetadataName, string typeAssemblyName)
		{
			ParameterId = parameterId;
			DisplayName = displayName;
			TypeMetadataName = typeMetadataName;
			TypeAssemblyName = typeAssemblyName;
		}
	}
}
