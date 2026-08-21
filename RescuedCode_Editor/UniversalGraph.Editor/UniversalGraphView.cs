using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
	public class UniversalGraphView : GraphView
	{
		private int changeNotificationSuppressionDepth;

		private bool isGraphChangedNotificationScheduled;

		private GraphContainer currentContainer;

		public event Action OnGraphChanged;

		public event Action<GraphNode> OnNodeSelected;

		public UniversalGraphView()
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Expected O, but got Unknown
			//IL_0029: Expected O, but got Unknown
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Expected O, but got Unknown
			//IL_003a: Expected O, but got Unknown
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			//IL_004b: Expected O, but got Unknown
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Expected O, but got Unknown
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Expected O, but got Unknown
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Expected O, but got Unknown
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Expected O, but got Unknown
			((GraphView)this).SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
			VisualElementExtensions.AddManipulator((VisualElement)this, (IManipulator)new ContentDragger());
			VisualElementExtensions.AddManipulator((VisualElement)this, (IManipulator)new SelectionDragger());
			VisualElementExtensions.AddManipulator((VisualElement)this, (IManipulator)new RectangleSelector());
			((GraphView)this).graphViewChanged = new GraphViewChanged(OnGraphViewChanged);
			GridBackground val = new GridBackground();
			((VisualElement)this).Insert(0, (VisualElement)val);
			VisualElementExtensions.StretchToParentSize((VisualElement)val);
		}

		internal void NotifyNodeSelected(GraphNode node)
		{
			this.OnNodeSelected?.Invoke(node);
		}

		public void SetContainer(GraphContainer container)
		{
			currentContainer = container;
		}

		private GraphViewChange OnGraphViewChanged(GraphViewChange change)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			if (changeNotificationSuppressionDepth == 0 && (change.elementsToRemove != null || change.edgesToCreate != null || change.movedElements != null))
			{
				ScheduleGraphChangedNotification();
			}
			return change;
		}

		private void ScheduleGraphChangedNotification()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Expected O, but got Unknown
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Expected O, but got Unknown
			if (!isGraphChangedNotificationScheduled)
			{
				isGraphChangedNotificationScheduled = true;
				EditorApplication.delayCall = (CallbackFunction)Delegate.Combine((Delegate)(object)EditorApplication.delayCall, (Delegate)new CallbackFunction(NotifyGraphChanged));
			}
		}

		private void NotifyGraphChanged()
		{
			isGraphChangedNotificationScheduled = false;
			this.OnGraphChanged?.Invoke();
		}

		public void ExecuteWithoutChangeNotification(Action action)
		{
			changeNotificationSuppressionDepth++;
			try
			{
				action?.Invoke();
			}
			finally
			{
				changeNotificationSuppressionDepth--;
			}
		}

		public void CancelPendingChangeNotification()
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Expected O, but got Unknown
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Expected O, but got Unknown
			if (isGraphChangedNotificationScheduled)
			{
				EditorApplication.delayCall = (CallbackFunction)Delegate.Remove((Delegate)(object)EditorApplication.delayCall, (Delegate)new CallbackFunction(NotifyGraphChanged));
				isGraphChangedNotificationScheduled = false;
			}
		}

		public void FlushPendingChangeNotification()
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Expected O, but got Unknown
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Expected O, but got Unknown
			if (isGraphChangedNotificationScheduled)
			{
				EditorApplication.delayCall = (CallbackFunction)Delegate.Remove((Delegate)(object)EditorApplication.delayCall, (Delegate)new CallbackFunction(NotifyGraphChanged));
				NotifyGraphChanged();
			}
		}

		public GraphNode CreateNode(Vector2 position, GraphNodeEditorRegistry.Registration registration)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Expected O, but got Unknown
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Expected O, but got Unknown
			try
			{
				if ((Object)currentContainer == (Object)null)
				{
					throw new InvalidOperationException("癒쇱? ?몄쭛??GraphContainer瑜??댁뼱???⑸땲??");
				}
				List<NodeBaseData> existingNodes = (from node in GraphViewExtensions.GetNodes<GraphNode>((GraphView)this)
					where node.Data != null
					select node.Data).ToList();
				GraphNodeCreationContext context = new GraphNodeCreationContext(position, existingNodes);
				GraphNode graphNode = GraphNodeEditorRegistry.CreateNewNode(currentContainer, registration, context);
				((GraphView)this).AddElement((GraphElement)graphNode);
				ScheduleGraphChangedNotification();
				return graphNode;
			}
			catch (Exception arg)
			{
				Debug.LogError((object)("[Flow Graph] '" + (registration?.MenuPath ?? "Unknown") + "' ?몃뱶瑜?" + $"?앹꽦?섏? 紐삵뻽?듬땲??\n{arg}"));
				return null;
			}
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Expected O, but got Unknown
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			((GraphView)this).BuildContextualMenu(evt);
			Vector2 val = VisualElementExtensions.LocalToWorld((VisualElement)this, ((MouseEventBase<ContextualMenuPopulateEvent>)(object)evt).localMousePosition);
			Vector2 canvasPos = VisualElementExtensions.WorldToLocal(((GraphView)this).contentViewContainer, val);
			foreach (GraphNodeEditorRegistry.Registration registration in GraphNodeEditorRegistry.GetRegistrations(currentContainer))
			{
				GraphNodeEditorRegistry.Registration capturedRegistration = registration;
				evt.menu.AppendAction(capturedRegistration.MenuPath, (Action<DropdownMenuAction>)delegate
				{
					//IL_0012: Unknown result type (might be due to invalid IL or missing references)
					CreateNode(canvasPos, capturedRegistration);
				}, (Status)1);
			}
		}

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
		{
			List<Port> compatiblePorts = new List<Port>();
			base.ports.ForEach((Action<Port>)delegate(Port port)
			{
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_0029: Unknown result type (might be due to invalid IL or missing references)
				if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
				{
					compatiblePorts.Add(port);
				}
			});
			return compatiblePorts;
		}
	}
}
