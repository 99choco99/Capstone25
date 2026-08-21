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
			if (changeNotificationSuppressionDepth == 0 && (change.elementsToRemove != null || change.edgesToCreate != null || change.movedElements != null))
			{
				ScheduleGraphChangedNotification();
			}
			return change;
		}

		private void ScheduleGraphChangedNotification()
		{
			if (!isGraphChangedNotificationScheduled)
			{
				isGraphChangedNotificationScheduled = true;
				EditorApplication.delayCall += NotifyGraphChanged;
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
			if (isGraphChangedNotificationScheduled)
			{
				EditorApplication.delayCall -= NotifyGraphChanged;
				isGraphChangedNotificationScheduled = false;
			}
		}

		public void FlushPendingChangeNotification()
		{
			if (isGraphChangedNotificationScheduled)
			{
				EditorApplication.delayCall -= NotifyGraphChanged;
				NotifyGraphChanged();
			}
		}

		public GraphNode CreateNode(Vector2 position, GraphNodeEditorRegistry.Registration registration)
		{
			try
			{
				if (currentContainer == null)
				{
					throw new InvalidOperationException("?믪눘? ?紐꾩춿??GraphContainer????곷선????몃빍??");
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
				Debug.LogError((object)("[Flow Graph] '" + (registration?.MenuPath ?? "Unknown") + "' ?紐껊굡??" + $"??밴쉐??? 筌륁궢六??щ빍??\n{arg}"));
				return null;
			}
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			((GraphView)this).BuildContextualMenu(evt);
			Vector2 val = VisualElementExtensions.LocalToWorld((VisualElement)this, ((MouseEventBase<ContextualMenuPopulateEvent>)(object)evt).localMousePosition);
			Vector2 canvasPos = VisualElementExtensions.WorldToLocal(((GraphView)this).contentViewContainer, val);
			foreach (GraphNodeEditorRegistry.Registration registration in GraphNodeEditorRegistry.GetRegistrations(currentContainer))
			{
				GraphNodeEditorRegistry.Registration capturedRegistration = registration;
				evt.menu.AppendAction(capturedRegistration.MenuPath, (Action<DropdownMenuAction>)delegate
				{
					CreateNode(canvasPos, capturedRegistration);
				}, DropdownMenuAction.Status.Normal);
			}
		}

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
		{
			List<Port> compatiblePorts = new List<Port>();
			base.ports.ForEach((Action<Port>)delegate(Port port)
			{
				if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
				{
					compatiblePorts.Add(port);
				}
			});
			return compatiblePorts;
		}
	}
}




