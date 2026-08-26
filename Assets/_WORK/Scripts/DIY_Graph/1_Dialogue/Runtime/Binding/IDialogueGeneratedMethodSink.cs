using System.ComponentModel;

namespace UniversalGraph
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IDialogueGeneratedMethodSink
	{
		void Add(DialogueGeneratedMethodRegistration registration);
	}
}
