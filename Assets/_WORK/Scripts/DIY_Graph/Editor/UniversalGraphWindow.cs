using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
    /// <summary>
    /// Hosts a graph canvas and node inspector for any <see cref="GraphContainer"/> asset.
    /// It is responsible for editor persistence and Undo; graph serialization lives in
    /// <see cref="GraphSerializer"/>.
    /// </summary>
    public class UniversalGraphWindow : EditorWindow
    {
        private UniversalGraphView graphView;
        private NodeInspector inspectorPanel;

        [SerializeField]
        private GraphContainer currentContainer;

        // Set only after a successful load. This prevents a failed load from overwriting an asset.
        private GraphContainer loadedContainer;
        private bool isLoading;

        /// <summary>Opens a graph asset in the shared graph editor window.</summary>
        public static void Open(GraphContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            UniversalGraphWindow window = GetWindow<UniversalGraphWindow>();
            window.titleContent = new GUIContent($"Flow Graph - {container.name}");
            window.LoadData(container);
        }

        /// <summary>Opens any graph container when the asset is double-clicked.</summary>
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
            Construct();
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            Undo.undoRedoPerformed += OnUndoRedo;

            if (currentContainer != null)
            {
                LoadData(currentContainer, flushPendingChanges: false);
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);

            SaveData();
            if (graphView == null)
            {
                return;
            }

            graphView.CancelPendingChangeNotification();
            graphView.OnGraphChanged -= RecordGraphStructureChange;
            graphView.OnNodeSelected -= UpdateInspector;
            graphView.RemoveFromHierarchy();
        }

        /// <summary>Builds the canvas and inspector UI for this editor window instance.</summary>
        private void Construct()
        {
            rootVisualElement.Clear();

            var splitView = new TwoPaneSplitView(1, 500f, TwoPaneSplitViewOrientation.Horizontal);
            splitView.StretchToParentSize();
            rootVisualElement.Add(splitView);

            graphView = new UniversalGraphView
            {
                name = "Flow Graph",
                focusable = true
            };
            graphView.OnGraphChanged += RecordGraphStructureChange;
            graphView.OnNodeSelected += UpdateInspector;
            splitView.Add(graphView);

            inspectorPanel = new NodeInspector(ApplyGraphEdit);
            splitView.Add(inspectorPanel);
        }

        /// <summary>Refreshes the inspector for the currently selected graph node.</summary>
        public void UpdateInspector(Node selectedNode)
        {
            inspectorPanel?.UpdateInspector(selectedNode);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.S || !evt.actionKey)
            {
                return;
            }

            SaveData();
            evt.StopPropagation();
        }

        /// <summary>
        /// Applies a field edit as one Undo operation, then serializes the current view into the asset.
        /// Structural graph changes use <see cref="RecordGraphStructureChange"/> instead.
        /// </summary>
        private void ApplyGraphEdit(string undoName, Action edit)
        {
            if (edit == null)
            {
                throw new ArgumentNullException(nameof(edit));
            }

            if (isLoading || !CanSaveLoadedGraph())
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(currentContainer, string.IsNullOrWhiteSpace(undoName) ? "Edit Graph" : undoName);
            graphView.ExecuteWithoutChangeNotification(edit);
            GraphSerializer.SaveGraphToMemory(graphView, currentContainer);
            EditorUtility.SetDirty(currentContainer);
        }

        /// <summary>Records a graph structure edit such as creating, deleting, or reconnecting a node.</summary>
        private void RecordGraphStructureChange()
        {
            if (isLoading || !CanSaveLoadedGraph())
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(currentContainer, "Change Graph Structure");
            GraphSerializer.SaveGraphToMemory(graphView, currentContainer);
            EditorUtility.SetDirty(currentContainer);
        }

        /// <summary>
        /// Reloads the graph after Undo or Redo and restores a single selected node when it still exists.
        /// This keeps the inspector usable after value edits.
        /// </summary>
        private void OnUndoRedo()
        {
            if (currentContainer == null || graphView == null)
            {
                return;
            }

            string selectedNodeGuid = GetSingleSelectedNodeGuid();
            graphView.CancelPendingChangeNotification();
            LoadData(currentContainer, flushPendingChanges: false);
            RestoreSelection(selectedNodeGuid);
        }

        /// <summary>Flushes the view to the loaded asset and marks the asset dirty for Unity persistence.</summary>
        private void SaveData()
        {
            if (!CanSaveLoadedGraph())
            {
                return;
            }

            graphView.FlushPendingChangeNotification();
            GraphSerializer.SaveGraphToMemory(graphView, currentContainer);
            EditorUtility.SetDirty(currentContainer);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Rebuilds the visual graph from an asset. The previous asset remains protected if loading fails.
        /// </summary>
        private void LoadData(GraphContainer container, bool flushPendingChanges = true)
        {
            if (container == null || graphView == null)
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

            GraphContainer previousContainer = currentContainer;
            isLoading = true;
            try
            {
                graphView.CancelPendingChangeNotification();
                graphView.ExecuteWithoutChangeNotification(() => GraphSerializer.LoadGraph(graphView, container));
                currentContainer = container;
                loadedContainer = container;
                graphView.SetContainer(container);
                RefreshInspectorFromSelection();
            }
            catch (Exception exception)
            {
                currentContainer = previousContainer;
                loadedContainer = null;
                graphView.SetContainer(null);
                inspectorPanel?.UpdateInspector(null);
                Debug.LogError($"[Flow Graph] Failed to load '{container.name}'. The asset was not modified.\n{exception}", container);
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>Synchronizes the inspector with a single canvas selection, otherwise clears it.</summary>
        private void RefreshInspectorFromSelection()
        {
            GraphNode[] selectedNodes = graphView.selection.OfType<GraphNode>().ToArray();
            inspectorPanel?.UpdateInspector(selectedNodes.Length == 1 ? selectedNodes[0] : null);
        }

        private string GetSingleSelectedNodeGuid()
        {
            GraphNode[] selectedNodes = graphView.selection.OfType<GraphNode>().ToArray();
            return selectedNodes.Length == 1 ? selectedNodes[0].Data?.Guid : null;
        }

        private void RestoreSelection(string nodeGuid)
        {
            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                RefreshInspectorFromSelection();
                return;
            }

            GraphNode node = graphView.nodes.OfType<GraphNode>()
                .FirstOrDefault(candidate => string.Equals(candidate.Data?.Guid, nodeGuid, StringComparison.Ordinal));
            if (node == null)
            {
                RefreshInspectorFromSelection();
                return;
            }

            graphView.ClearSelection();
            graphView.AddToSelection(node);
            inspectorPanel?.UpdateInspector(node);
        }

        /// <summary>Returns whether the current visual graph is known to have loaded from the selected asset.</summary>
        private bool CanSaveLoadedGraph()
        {
            return graphView != null && currentContainer != null && ReferenceEquals(loadedContainer, currentContainer);
        }
    }
}
