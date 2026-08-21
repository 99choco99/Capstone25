using System;

namespace UniversalGraph.Editor
{
	public sealed class NodeInspectorContext
	{
		private readonly Action<string, Action> applyEdit;

		public NodeInspectorContext(Action<string, Action> applyEdit)
		{
			this.applyEdit = applyEdit ?? throw new ArgumentNullException("applyEdit");
		}

		public void ApplyEdit(string undoName, Action edit)
		{
			if (edit == null)
			{
				throw new ArgumentNullException("edit");
			}
			applyEdit(undoName, edit);
		}
	}
}
