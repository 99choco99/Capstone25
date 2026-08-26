using System.ComponentModel;

namespace UniversalGraph
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IDialogueGeneratedMethodProvider
	{
		void Collect(IDialogueGeneratedMethodSink sink);
	}
}
