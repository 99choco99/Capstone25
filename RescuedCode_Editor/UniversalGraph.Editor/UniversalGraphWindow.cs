using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
	public class UniversalGraphWindow : EditorWindow
	{
		private UniversalGraphView graphView;

		private NodeInspector inspectorPanel;

		[SerializeField]
		private GraphContainer currentContainer;

		private GraphContainer loadedContainer;

		private bool isLoading = false;

		public static void Open(GraphContainer container)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Expected O, but got Unknown
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Expected O, but got Unknown
			if ((Object)container == (Object)null)
			{
				throw new ArgumentNullException("container");
			}
			UniversalGraphWindow window = EditorWindow.GetWindow<UniversalGraphWindow>();
			((EditorWindow)window).titleContent = new GUIContent("Flow Graph - " + ((Object)container).name);
			window.LoadData(container);
		}

		[OnOpenAsset(1)]
		public static bool OnOpenAsset(int entityId)
		{
			if (EditorUtility.InstanceIDToObject(entityId) is GraphContainer container)
			{
				Open(container);
				return true;
			}
			return false;
		}

		private void OnEnable()
		{
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Expected O, but got Unknown
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Expected O, but got Unknown
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Expected O, but got Unknown
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Expected O, but got Unknown
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			string[] array = AssetDatabase.FindAssets("DialogueGraphStyle t:StyleSheet");
			if (array.Length != 0)
			{
				string text = AssetDatabase.GUIDToAssetPath(array[0]);
				StyleSheet val = AssetDatabase.LoadAssetAtPath<StyleSheet>(text);
				if ((Object)val != (Object)null)
				{
					VisualElementStyleSheetSet styleSheets = ((EditorWindow)this).rootVisualElement.styleSheets;
					((VisualElementStyleSheetSet)(ref styleSheets)).Add(val);
				}
			}
			Construct();
			((CallbackEventHandler)((EditorWindow)this).rootVisualElement).RegisterCallback<KeyDownEvent>((EventCallback<KeyDownEvent>)OnKeyDown, (TrickleDown)0);
			Undo.undoRedoPerformed = (UndoRedoCallback)Delegate.Combine((Delegate)(object)Undo.undoRedoPerformed, (Delegate)new UndoRedoCallback(OnUndoRedo));
			if ((Object)currentContainer != (Object)null)
			{
				LoadData(currentContainer, flushPendingChanges: false);
			}
		}

		private void OnDisable()
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Expected O, but got Unknown
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected O, but got Unknown
			Undo.undoRedoPerformed = (UndoRedoCallback)Delegate.Remove((Delegate)(object)Undo.undoRedoPerformed, (Delegate)new UndoRedoCallback(OnUndoRedo));
			if (graphView != null)
			{
				if (CanSaveLoadedGraph())
				{
					graphView.FlushPendingChangeNotification();
				}
				else
				{
					graphView.CancelPendingChangeNotification();
				}
				graphView.OnGraphChanged -= RecordGraphStructureChange;
				graphView.OnNodeSelected -= UpdateInspector;
				graphView.CancelPendingChangeNotification();
				((VisualElement)graphView).RemoveFromHierarchy();
			}
			SaveData();
			AssetDatabase.SaveAssets();
			((CallbackEventHandler)((EditorWindow)this).rootVisualElement).UnregisterCallback<KeyDownEvent>((EventCallback<KeyDownEvent>)OnKeyDown, (TrickleDown)0);
		}

		private void Construct()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Expected O, but got Unknown
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Expected O, but got Unknown
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Expected O, but got Unknown
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Expected O, but got Unknown
			((EditorWindow)this).rootVisualElement.Clear();
			TwoPaneSplitView val = new TwoPaneSplitView(1, 500f, (TwoPaneSplitViewOrientation)0);
			VisualElementExtensions.StretchToParentSize((VisualElement)val);
			((EditorWindow)this).rootVisualElement.Add((VisualElement)val);
			UniversalGraphView universalGraphView = new UniversalGraphView();
			((VisualElement)universalGraphView).name = "Flow Graph";
			graphView = universalGraphView;
			((VisualElement)val).Add((VisualElement)graphView);
			((Focusable)graphView).focusable = true;
			graphView.OnGraphChanged += RecordGraphStructureChange;
			graphView.OnNodeSelected += UpdateInspector;
			inspectorPanel = new NodeInspector(ApplyGraphEdit);
			((VisualElement)val).Add((VisualElement)inspectorPanel);
		}

		public void UpdateInspector(Node selectedNode)
		{
			inspectorPanel.UpdateInspector(selectedNode);
		}

		private void OnKeyDown(KeyDownEvent evt)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			if ((int)((KeyboardEventBase<KeyDownEvent>)(object)evt).keyCode == 115 && ((KeyboardEventBase<KeyDownEvent>)(object)evt).actionKey)
			{
				SaveData();
				((EventBase)evt).StopPropagation();
			}
		}

		private void ApplyGraphEdit(string undoName, Action edit)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			if (!isLoading && CanSaveLoadedGraph())
			{
				if (edit == null)
				{
					throw new ArgumentNullException("edit");
				}
				Undo.RegisterCompleteObjectUndo((Object)currentContainer, undoName);
				graphView.ExecuteWithoutChangeNotification(edit);
				GraphSerializer.SaveGraphToMemory(graphView, currentContainer);
			}
		}

		private void RecordGraphStructureChange()
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Expected O, but got Unknown
			if (!isLoading && CanSaveLoadedGraph())
			{
				Undo.RegisterCompleteObjectUndo((Object)currentContainer, "Change Graph");
				GraphSerializer.SaveGraphToMemory(graphView, currentContainer);
			}
		}

		private void OnUndoRedo()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Expected O, but got Unknown
			if ((Object)currentContainer != (Object)null)
			{
				graphView.CancelPendingChangeNotification();
				inspectorPanel.UpdateInspector(null);
				LoadData(currentContainer, flushPendingChanges: false);
			}
		}

		private void SaveData()
		{
			if (CanSaveLoadedGraph())
			{
				graphView?.FlushPendingChangeNotification();
				GraphSerializer.SaveGraphToMemory(graphView, currentContainer);
				AssetDatabase.SaveAssets();
			}
		}

		private void LoadData(GraphContainer container, bool flushPendingChanges = true)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0119: Expected O, but got Unknown
			if ((Object)container == (Object)null)
			{
				return;
			}
			if (flushPendingChanges && CanSaveLoadedGraph())
			{
				graphView.FlushPendingChangeNotification();
			}
			else
			{
				graphView.CancelPendingChangeNotification();
			}
			GraphContainer graphContainer = currentContainer;
			isLoading = true;
			try
			{
				graphView.CancelPendingChangeNotification();
				graphView.ExecuteWithoutChangeNotification(delegate
				{
					GraphSerializer.LoadGraph(graphView, container);
				});
				currentContainer = container;
				loadedContainer = container;
				graphView.SetContainer(container);
				RefreshInspectorFromSelection();
			}
			catch (Exception arg)
			{
				currentContainer = graphContainer;
				loadedContainer = null;
				graphView.SetContainer(null);
				Debug.LogError((object)("[Flow Graph] '" + ((Object)container).name + "'??遺덈윭?ㅼ? 紐삵뻽?듬땲?? ?먮낯 ?먯뀑?\u0080 蹂\u0080寃쏀븯吏\u0080 ?딆븯?쇰ŉ, ?깃났?곸쑝濡??ㅼ떆 ?닿린 ?꾧퉴吏\u0080 " + $"??View???\u0080?μ쓣 李⑤떒?⑸땲??\n{arg}"), (Object)container);
			}
			finally
			{
				isLoading = false;
			}
		}

		private void RefreshInspectorFromSelection()
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Expected O, but got Unknown
			GraphNode[] array = ((GraphView)graphView).selection.OfType<GraphNode>().ToArray();
			inspectorPanel.UpdateInspector((Node)((array.Length == 1) ? array[0] : null));
		}

		private bool CanSaveLoadedGraph()
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Expected O, but got Unknown
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Expected O, but got Unknown
			//IL_0037: Expected O, but got Unknown
			return graphView != null && (Object)currentContainer != (Object)null && (Object)loadedContainer == (Object)currentContainer;
		}
	}
}
