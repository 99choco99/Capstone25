using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
	public abstract class GraphNode : Node
	{
		public abstract NodeBaseData Data { get; }

		public abstract Type DataType { get; }

		public virtual Vector2 DefaultSize => new Vector2(200f, 150f);

		public abstract void BindNodeData(NodeBaseData data);

		public abstract NodeBaseData CreateNewData(GraphNodeCreationContext context);

		public virtual VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected O, but got Unknown
			return new VisualElement();
		}
	}
	public abstract class GraphNode<T> : GraphNode where T : NodeBaseData, new()
	{
		public T TypeData { get; private set; }

		public override NodeBaseData Data => TypeData;

		public override Type DataType => typeof(T);

		public override void BindNodeData(NodeBaseData data)
		{
			if (TypeData != null)
			{
				throw new InvalidOperationException("'" + ((object)this).GetType().FullName + "'?먮뒗 NodeData瑜???踰덈쭔 Bind?????덉뒿?덈떎.");
			}
			if (!(data is T val))
			{
				throw new ArgumentException("'" + ((object)this).GetType().FullName + "'?\u0080 " + typeof(T).FullName + "留?Bind?????덉?留?" + (data?.GetType().FullName ?? "null") + "???꾨떖?먯뒿?덈떎.", "data");
			}
			ValidateDataForView(val);
			TypeData = val;
			((VisualElement)this).viewDataKey = data.Guid;
			Draw();
		}

		protected virtual void ValidateDataForView(T data)
		{
		}

		public sealed override NodeBaseData CreateNewData(GraphNodeCreationContext context)
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			T val = new T();
			InitializeNewData(val, context);
			val.Guid = Guid.NewGuid().ToString();
			val.Position = context.Position;
			return val;
		}

		protected virtual void InitializeNewData(T data, GraphNodeCreationContext context)
		{
		}

		protected abstract void Draw();

		public override void OnSelected()
		{
			((GraphElement)this).OnSelected();
			((VisualElement)this).GetFirstAncestorOfType<UniversalGraphView>()?.NotifyNodeSelected(this);
		}
	}
}
