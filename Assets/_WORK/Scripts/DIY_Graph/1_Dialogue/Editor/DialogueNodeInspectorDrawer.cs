using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Builds the inspector controls for a dialogue node and its choices.</summary>
    internal static class DialogueNodeInspectorDrawer
    {
        /// <summary>Creates the complete inspector for the selected dialogue node.</summary>
        public static VisualElement Create(DialogueNode selectedNode, NodeInspectorContext context)
        {
            var root = new VisualElement();
            var title = new Label("Dialogue Node");
            title.AddToClassList("inspector-title");
            root.Add(title);

            root.Add(CreateSpeakerField(selectedNode, context));
            root.Add(CreateDialogueField(selectedNode, context));
            root.Add(DialogueMethodBindingEditor.Create(
                context,
                "On Enter Action",
                DialogueMethodKind.Action,
                new DialogueMethodBindingAccessor(
                    () => selectedNode.TypeData.EventKey,
                    key => selectedNode.TypeData.EventKey = key,
                    () => selectedNode.TypeData.EventParam,
                    parameter => selectedNode.TypeData.EventParam = parameter,
                    () => selectedNode.TypeData.EventArguments,
                    arguments => selectedNode.TypeData.EventArguments = arguments)));
            root.Add(CreateChoicesField(selectedNode, context));
            return root;
        }

        /// <summary>Creates the speaker name field and updates the canvas preview after edits.</summary>
        private static TextField CreateSpeakerField(DialogueNode selectedNode, NodeInspectorContext context)
        {
            var field = new TextField("Speaker")
            {
                value = selectedNode.TypeData.SpeakerName ?? string.Empty
            };
            field.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit("Change dialogue speaker", () =>
                {
                    selectedNode.TypeData.SpeakerName = change.newValue;
                    selectedNode.RefreshPreview();
                });
            });
            return field;
        }

        /// <summary>Creates the main line field and updates the canvas preview after edits.</summary>
        private static TextField CreateDialogueField(DialogueNode selectedNode, NodeInspectorContext context)
        {
            var field = new TextField("Dialogue")
            {
                value = selectedNode.TypeData.DialogueText ?? string.Empty,
                multiline = true
            };
            field.AddToClassList("dialogue-field");
            field.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit("Change dialogue text", () =>
                {
                    selectedNode.TypeData.DialogueText = change.newValue;
                    selectedNode.RefreshPreview();
                });
            });
            return field;
        }

        /// <summary>Creates the repeatable choice editor and keeps choice ports synchronized with data.</summary>
        private static VisualElement CreateChoicesField(DialogueNode selectedNode, NodeInspectorContext context)
        {
            var root = new VisualElement();
            var title = new Label("Choices");
            title.AddToClassList("choice-title");
            root.Add(title);

            var choicesContainer = new VisualElement();
            root.Add(choicesContainer);

            var addButton = new Button(() =>
            {
                context.ApplyEdit("Add dialogue choice", () =>
                {
                    var data = selectedNode.TypeData;
                    data.Choices ??= new List<DialogueChoiceData>();
                    var choice = new DialogueChoiceData
                    {
                        PortName = Guid.NewGuid().ToString(),
                        ChoiceText = "New Choice",
                        ChoiceEventKey = string.Empty,
                        ChoiceEventParam = string.Empty,
                        ChoiceEventArguments = new List<DialogueArgumentData>()
                    };
                    data.Choices.Add(choice);
                    selectedNode.AddChoicePort(choice);
                    RedrawChoices();
                });
            })
            {
                text = "+ Add Choice"
            };
            addButton.AddToClassList("add-choice-btn");
            root.Add(addButton);

            RedrawChoices();
            return root;

            void RedrawChoices()
            {
                choicesContainer.Clear();
                if (selectedNode.TypeData.Choices == null)
                {
                    return;
                }

                foreach (DialogueChoiceData choice in selectedNode.TypeData.Choices)
                {
                    if (choice != null)
                    {
                        choicesContainer.Add(CreateChoiceBox(selectedNode, choice, context, RedrawChoices));
                    }
                }
            }
        }

        /// <summary>Creates the text, action, and deletion controls for a single choice.</summary>
        private static Box CreateChoiceBox(
            DialogueNode selectedNode,
            DialogueChoiceData choice,
            NodeInspectorContext context,
            Action redrawChoices)
        {
            var box = new Box();
            box.AddToClassList("choice-box");

            var textField = new TextField("Text")
            {
                value = choice.ChoiceText ?? string.Empty,
                multiline = true
            };
            textField.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit("Change choice text", () => choice.ChoiceText = change.newValue);
            });
            box.Add(textField);

            box.Add(DialogueMethodBindingEditor.Create(
                context,
                "On Select Action",
                DialogueMethodKind.Action,
                new DialogueMethodBindingAccessor(
                    () => choice.ChoiceEventKey,
                    key => choice.ChoiceEventKey = key,
                    () => choice.ChoiceEventParam,
                    parameter => choice.ChoiceEventParam = parameter,
                    () => choice.ChoiceEventArguments,
                    arguments => choice.ChoiceEventArguments = arguments)));

            var deleteButton = new Button(() =>
            {
                context.ApplyEdit("Delete dialogue choice", () =>
                {
                    selectedNode.RemoveChoicePort(choice.PortName);
                    selectedNode.TypeData.Choices.Remove(choice);
                    redrawChoices?.Invoke();
                });
            })
            {
                text = "Delete Choice"
            };
            box.Add(deleteButton);
            return box;
        }
    }
}
