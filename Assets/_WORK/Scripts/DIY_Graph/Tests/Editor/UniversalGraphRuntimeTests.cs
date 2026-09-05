using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniversalGraph.Tests
{
    public sealed class UniversalGraphRuntimeTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();
        private static int attributedQuestActionAmount;
        private static bool attributedQuestActionFlag;
        private static int dialogueChoiceActionCount;
        private static Action<QuestExecutionContext> questRunAction;
        private static Func<QuestExecutionContext, bool> questRunCondition;
        private static int overloadedActionAmount;

        [OneTimeSetUp]
        public void RegisterTestMethods()
        {
            // Editor 전용 테스트 메서드는 운영 초기화에서 제외되므로 테스트에서만 등록합니다.
            foreach (Type invoker in new[] { typeof(DialogueMethodInvoker), typeof(QuestMethodInvoker) })
            {
                invoker.GetMethod("ResetStaticState", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                invoker.GetMethod("Initialize").Invoke(null, null);
                object[] arguments = { typeof(UniversalGraphRuntimeTests).Assembly };
                bool registered = (bool)invoker.GetMethod("TryRegisterGeneratedAssembly", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, arguments);
                if (!registered)
                {
                    string scanMethod = invoker == typeof(DialogueMethodInvoker) ? "ScanAssemblyByReflection" : "ScanAssembly";
                    invoker.GetMethod(scanMethod, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);
                }
            }
        }

        [OneTimeTearDown]
        public void ResetMethodInvokers()
        {
            foreach (Type invoker in new[] { typeof(DialogueMethodInvoker), typeof(QuestMethodInvoker) })
            {
                invoker.GetMethod("ResetStaticState", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }
        }

        [TearDown]
        public void TearDown()
        {
            questRunAction = null;
            questRunCondition = null;
            if (DialogueManager.Instance.IsConversationActive)
            {
                DialogueManager.Instance.CancelConversation();
            }

            foreach (UnityEngine.Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }
            createdObjects.Clear();
        }

        [TestCase(typeof(DialogueMethodInvoker), "actionRegistry")]
        [TestCase(typeof(QuestMethodInvoker), "Actions")]
        public void MethodInvokers_InitializeExcludesEditorOnlyTestAssembly(Type invoker, string registryName)
        {
            try
            {
                invoker.GetMethod("ResetStaticState", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                invoker.GetMethod("Initialize").Invoke(null, null);
                invoker.GetMethod("Initialize").Invoke(null, null);
                var registry = (System.Collections.IDictionary)invoker.GetField(registryName, BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(null);
                Assert.That(registry.Values.Cast<MethodDescriptor>().Any(
                    descriptor => descriptor.DeclaringType.Assembly == typeof(UniversalGraphRuntimeTests).Assembly), Is.False);
            }
            finally
            {
                RegisterTestMethods();
            }
        }

        [TestCase(typeof(DialogueMethodInvoker), "Dialogue", "tests.dialogue.choice-visible")]
        [TestCase(typeof(QuestMethodInvoker), "Quest", "tests.quest.choice-visible")]
        public void MethodInvokers_InvokeActionAndConditionWithOneApi(Type invoker, string label, string conditionKey)
        {
            MethodInfo invoke = invoker.GetMethod("TryInvokeMethod");
            var binding = new MethodBindingData { Key = "tests.invoker.action" };
            object[] arguments = { binding, null, MethodKind.Action, true };
            int actionCount = dialogueChoiceActionCount;

            Assert.That((bool)invoke.Invoke(null, arguments), Is.True);
            Assert.That(arguments[3], Is.False);
            Assert.That(dialogueChoiceActionCount, Is.EqualTo(actionCount + 1));

            binding.Key = conditionKey;
            arguments[2] = MethodKind.Condition;
            foreach (bool expected in new[] { false, true })
            {
                binding.Arguments = CreateDialogueArguments(
                    nameof(IsDialogueChoiceVisible), MethodKind.Condition, ("arg0", expected));
                Assert.That((bool)invoke.Invoke(null, arguments), Is.True);
                Assert.That(arguments[3], Is.EqualTo(expected));
            }

            binding.Key = "tests.invoker.action";
            binding.Arguments.Clear();
            arguments[2] = (MethodKind)(-1);
            LogAssert.Expect(LogType.Error, $"[{label}] 메서드 종류가 올바르지 않습니다.");
            Assert.That((bool)invoke.Invoke(null, arguments), Is.False);
            Assert.That(arguments[3], Is.False);
            Assert.That(dialogueChoiceActionCount, Is.EqualTo(actionCount + 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MethodDescriptorFactories_ResolveNonGenericOverload(bool quest)
        {
            var parameters = new[]
            {
                new GeneratedParameterRegistration("arg0", "amount", typeof(int).FullName, typeof(int).Assembly.GetName().Name)
            };
            object registration;
            Type factory;
            if (quest)
            {
                registration = new QuestGeneratedMethodRegistration(
                    MethodKind.Action, "tests.overload", QuestMethodTarget.Global,
                    typeof(UniversalGraphRuntimeTests).FullName, nameof(RecordOverloadedAction), true, parameters, null);
                factory = typeof(QuestMethodDescriptorFactory);
            }
            else
            {
                registration = new DialogueGeneratedMethodRegistration(
                    MethodKind.Action, "tests.overload", DialogueMethodOwner.Global,
                    typeof(UniversalGraphRuntimeTests).FullName, nameof(RecordOverloadedAction), true, parameters, null);
                factory = typeof(DialogueMethodDescriptorFactory);
            }

            object[] arguments = { typeof(UniversalGraphRuntimeTests).Assembly, registration, null, null };
            bool created = (bool)factory.GetMethod("TryCreateGenerated", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, arguments);
            Assert.That(created, Is.True, arguments[3] as string);
            var descriptor = (MethodDescriptor)arguments[2];
            Assert.That(descriptor.MethodInfo.IsGenericMethod, Is.False);
            overloadedActionAmount = 0;
            descriptor.MethodInfo.Invoke(null, new object[] { 23 });
            Assert.That(overloadedActionAmount, Is.EqualTo(23));
        }

        [Test]
        public void DialogueContainer_ResolvesOneNamedEntry()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData
            {
                Guid = "entry",
                EntryId = $"  {DialogueEntryNodeData.DefaultEntryId}  "
            };
            var line = new DialogueLineNodeData { Guid = "line" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            bool resolved = graph.FindEntryNode(
                $" {DialogueEntryNodeData.DefaultEntryId} ",
                out DialogueEntryNodeData result,
                out string error);

            Assert.That(resolved, Is.True, error);
            Assert.That(result, Is.SameAs(entry));
            Assert.That(entry.EntryId, Is.EqualTo(DialogueEntryNodeData.DefaultEntryId));

            bool wrongCaseResolved = graph.FindEntryNode(
                DialogueEntryNodeData.DefaultEntryId.ToLowerInvariant(),
                out _,
                out _);
            Assert.That(wrongCaseResolved, Is.False);
        }

        [Test]
        public void DialogueContainer_EntryLookupDoesNotValidateUnrelatedNodeData()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(new DialogueChoiceNodeData { Guid = "broken-choice", Choices = null });
            graph.NodeLinks = null;

            bool resolved = graph.FindEntryNode(
                DialogueEntryNodeData.DefaultEntryId,
                out DialogueEntryNodeData result,
                out string error);

            Assert.That(resolved, Is.True, error);
            Assert.That(result, Is.SameAs(entry));
        }

        [Test]
        public void DialogueManager_RejectsChoiceNodeWithoutChoices()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.name = "Broken Dialogue";
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueChoiceNodeData { Guid = "choice" });
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            LogAssert.Expect(
                LogType.Error,
                "[Dialogue] 대화를 시작하지 못했습니다. 대화 그래프 'Broken Dialogue'의 " +
                "Choice 노드 'choice'에 선택지가 없습니다.");

            bool started = DialogueManager.Instance.StartConversation(
                new DialogueEntryPoint(graph, DialogueEntryNodeData.DefaultEntryId),
                new DialogueExecutionContext(speaker, interactor));

            Assert.That(started, Is.False);
            Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
        }

        [Test]
        public void DialogueManager_StartsTextOnlyConversationWithoutExecutionContext()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry" };
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Hello" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "line"));

            bool started = DialogueManager.Instance.StartConversation(
                new DialogueEntryPoint(graph, DialogueEntryNodeData.DefaultEntryId));

            Assert.That(started, Is.True);
            Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(line));
        }

        [Test]
        public void MethodArgumentCodec_SupportsOnlyTheChosenGraphValueTypes()
        {
            Assert.That(MethodArgumentCodec.TryGetArgumentKind(typeof(int), out _), Is.True);
            Assert.That(MethodArgumentCodec.TryGetArgumentKind(typeof(float), out _), Is.True);
            Assert.That(MethodArgumentCodec.TryGetArgumentKind(typeof(ScriptableObject), out MethodArgumentKind unityKind), Is.True);
            Assert.That(unityKind, Is.EqualTo(MethodArgumentKind.UnityObject));
            Assert.That(MethodArgumentCodec.TryGetArgumentKind(typeof(long), out _), Is.False);
            Assert.That(MethodArgumentCodec.TryGetArgumentKind(typeof(double), out _), Is.False);
            Assert.That(MethodArgumentCodec.TryGetArgumentKind(typeof(object), out _), Is.False);
        }

        [Test]
        public void MethodArgumentCodec_EncodesAndDecodesAllSupportedValues()
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(AcceptAllSupportedArgumentTypes),
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(DialogueMethodDescriptorFactory.TryCreateFromReflection(
                    method,
                    MethodKind.Action,
                    "tests.dialogue.all-argument-types",
                    DialogueMethodOwner.Global,
                    out DialogueMethodDescriptor descriptor,
                    out string descriptorError),
                Is.True,
                descriptorError);

            DialogueContainer objectValue = CreateAsset<DialogueContainer>();
            object[] values =
            {
                "text",
                true,
                42,
                1.25f,
                QuestState.InProgress,
                objectValue
            };
            List<MethodArgumentData> arguments = MethodArgumentCodec.CreateDefaultArgumentData(descriptor);

            for (int i = 0; i < descriptor.SerializedParameters.Count; i++)
            {
                MethodParameterDescriptor parameterDescriptor = descriptor.SerializedParameters[i];
                MethodArgumentData argument = arguments[i];
                argument.ParameterId = "old-id";
                argument.TypeSignature = "old-type";
                argument.SerializedValue = "old-value";
                argument.ObjectValue = objectValue;

                Assert.That(MethodArgumentCodec.TryEncodeArgumentData(
                        argument,
                        parameterDescriptor,
                        values[i],
                        out string encodeError),
                    Is.True,
                    encodeError);
                Assert.That(argument.ParameterId, Is.EqualTo(parameterDescriptor.ParameterId));
                Assert.That(argument.TypeSignature, Is.EqualTo(parameterDescriptor.TypeSignature));

                if (parameterDescriptor.ArgumentKind == MethodArgumentKind.UnityObject)
                {
                    Assert.That(argument.SerializedValue, Is.Empty);
                    Assert.That(argument.ObjectValue, Is.SameAs(objectValue));
                }
                else
                {
                    Assert.That(argument.ObjectValue, Is.Null);
                }

                Assert.That(MethodArgumentCodec.TryDecodeArgumentData(
                        argument,
                        parameterDescriptor,
                        out object decodedValue,
                        out string decodeError),
                    Is.True,
                    decodeError);
                Assert.That(decodedValue, Is.EqualTo(values[i]));
            }
        }

        [Test]
        public void MethodArgumentCodec_DoesNotChangeDataWhenEncodingFails()
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsDialogueChoiceVisible),
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(DialogueMethodDescriptorFactory.TryCreateFromReflection(
                    method,
                    MethodKind.Condition,
                    "tests.dialogue.invalid-argument",
                    DialogueMethodOwner.Global,
                    out DialogueMethodDescriptor descriptor,
                    out string descriptorError),
                Is.True,
                descriptorError);

            MethodParameterDescriptor parameterDescriptor = descriptor.SerializedParameters[0];
            var argument = new MethodArgumentData
            {
                ParameterId = "old-id",
                TypeSignature = "old-type",
                SerializedValue = "old-value"
            };

            Assert.That(MethodArgumentCodec.TryEncodeArgumentData(
                    argument,
                    parameterDescriptor,
                    "not-a-boolean",
                    out string error),
                Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(argument.ParameterId, Is.EqualTo("old-id"));
            Assert.That(argument.TypeSignature, Is.EqualTo("old-type"));
            Assert.That(argument.SerializedValue, Is.EqualTo("old-value"));
            Assert.That(argument.ObjectValue, Is.Null);
        }

        [Test]
        public void WaitSignalNodeData_NormalizesSignalKeyWhenAssignedOrDeserialized()
        {
            var data = new DialogueWaitSignalNodeData { SignalKey = "  dialogue.finished  " };

            Assert.That(data.SignalKey, Is.EqualTo("dialogue.finished"));

            typeof(DialogueWaitSignalNodeData)
                .GetField("signalKey", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(data, "  dialogue.deserialized  ");

            Assert.That(data.SignalKey, Is.EqualTo("dialogue.deserialized"));
        }

        [Test]
        public void DialogueAndQuestDescriptors_UseTheCommonMethodContract()
        {
            MethodInfo dialogueMethod = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsDialogueChoiceVisible),
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo questMethod = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsAttributedQuestReady),
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(DialogueMethodDescriptorFactory.TryCreateFromReflection(
                    dialogueMethod,
                    MethodKind.Condition,
                    "tests.dialogue.choice-visible",
                    DialogueMethodOwner.Global,
                    out DialogueMethodDescriptor dialogueDescriptor,
                    out string dialogueError),
                Is.True,
                dialogueError);
            Assert.That(QuestMethodDescriptorFactory.TryCreateFromReflection(
                    questMethod,
                    MethodKind.Condition,
                    "tests.quest.is-ready",
                    QuestMethodTarget.Global,
                    out QuestMethodDescriptor questDescriptor,
                    out string questError),
                Is.True,
                questError);

            Assert.That(dialogueDescriptor, Is.InstanceOf<MethodDescriptor>());
            Assert.That(questDescriptor, Is.InstanceOf<MethodDescriptor>());
            Assert.That(dialogueDescriptor.SerializedParameters[0].ParameterId, Is.EqualTo("arg0"));
            Assert.That(dialogueDescriptor.SerializedParameters[0].DisplayName, Is.EqualTo("visible"));
            Assert.That(questDescriptor.SerializedParameters[0].ParameterId, Is.EqualTo("arg0"));
            Assert.That(questDescriptor.SerializedParameters[0].DisplayName, Is.EqualTo("required"));
            Assert.That(MethodArgumentCodec.CreateDefaultArgumentData(dialogueDescriptor), Has.Count.EqualTo(1));
            Assert.That(MethodArgumentCodec.CreateDefaultArgumentData(questDescriptor), Has.Count.EqualTo(1));
        }

        [Test]
        public void MethodDescriptorFactories_RejectUnknownKindAndOwnerValues()
        {
            MethodInfo dialogueMethod = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsDialogueChoiceVisible),
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo questMethod = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsAttributedQuestReady),
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(DialogueMethodDescriptorFactory.TryCreateFromReflection(
                dialogueMethod,
                (MethodKind)999,
                "tests.dialogue.invalid-kind",
                DialogueMethodOwner.Global,
                out _,
                out _), Is.False);
            Assert.That(DialogueMethodDescriptorFactory.TryCreateFromReflection(
                dialogueMethod,
                MethodKind.Condition,
                "tests.dialogue.invalid-owner",
                (DialogueMethodOwner)999,
                out _,
                out _), Is.False);
            Assert.That(QuestMethodDescriptorFactory.TryCreateFromReflection(
                questMethod,
                (MethodKind)999,
                "tests.quest.invalid-kind",
                QuestMethodTarget.Global,
                out _,
                out _), Is.False);
            Assert.That(QuestMethodDescriptorFactory.TryCreateFromReflection(
                questMethod,
                MethodKind.Condition,
                "tests.quest.invalid-target",
                (QuestMethodTarget)999,
                out _,
                out _), Is.False);
        }

        [Test]
        public void QuestQueries_ReturnsDialogueFromQuestStateWithoutGameTypes()
        {
            DialogueContainer dialogue = CreateAsset<DialogueContainer>();
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 10;
            var entry = new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc-7" };
            var condition = new QuestStateConditionNodeData
            {
                Guid = "condition",
                QuestId = 10,
                TargetState = QuestState.InProgress
            };
            var candidate = new DialogueCandidateNodeData
            {
                Guid = "candidate",
                EntryPoint = new DialogueEntryPoint(dialogue, "Default"),
                DisplayName = "Quest",
                Priority = 5
            };
            quest.Nodes.Add(entry);
            quest.Nodes.Add(condition);
            quest.Nodes.Add(candidate);
            quest.NodeLinks.Add(Link("entry", "Next", "condition"));
            quest.NodeLinks.Add(Link("condition", "True", "candidate"));

            var controller = new FakeQuestController();
            controller.QuestProgress.Add(10, new QuestProgress(quest) { state = QuestState.InProgress });

            QuestDefinitionRegistry.Initialize(new[] { quest });
            DialogueCandidate[] candidates = QuestQueries.GetDialogueCandidates(controller, "npc-7");

            Assert.That(candidates, Has.Length.EqualTo(1));
            Assert.That(candidates[0].EntryPoint.GraphAsset, Is.SameAs(dialogue));
            Assert.That(candidates[0].DisplayName, Is.EqualTo("Quest"));
            Assert.That(candidates[0].Priority, Is.EqualTo(5));
        }

        [Test]
        public void QuestOffer_RevalidatesPrerequisiteAndCreatesProgressWhenAccepted()
        {
            QuestContainer prerequisite = CreateAsset<QuestContainer>();
            prerequisite.QuestId = 40;
            prerequisite.Nodes.Add(new QuestStartNodeData { Guid = "prerequisite-start" });

            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 41;
            quest.questName = "Available Quest";
            var start = new QuestStartNodeData { Guid = "quest-start" };
            var objective = new QuestObjectiveNodeData
            {
                Guid = "objective",
                ObjectiveType = "Talk",
                TargetId = 1,
                RequiredAmount = 1
            };
            var interaction = new QuestInteractionEntryNodeData
            {
                Guid = "interaction",
                TargetId = "NPC"
            };
            var condition = new QuestStateConditionNodeData
            {
                Guid = "prerequisite-condition",
                QuestId = prerequisite.QuestId,
                TargetState = QuestState.TurnedIn
            };
            var available = new QuestOfferNodeData
            {
                Guid = "available-offer",
                Priority = 10,
                IsAvailable = true
            };
            var blocked = new QuestOfferNodeData
            {
                Guid = "blocked-offer",
                Priority = 10,
                IsAvailable = false,
                BlockReason = "선행 Quest 미완료"
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(objective);
            quest.Nodes.Add(interaction);
            quest.Nodes.Add(condition);
            quest.Nodes.Add(available);
            quest.Nodes.Add(blocked);
            quest.NodeLinks.Add(Link("quest-start", "Next", "objective"));
            quest.NodeLinks.Add(Link("interaction", "Next", "prerequisite-condition"));
            quest.NodeLinks.Add(Link("prerequisite-condition", "True", "available-offer"));
            quest.NodeLinks.Add(Link("prerequisite-condition", "False", "blocked-offer"));

            QuestDefinitionRegistry.Initialize(new[] { prerequisite, quest });
            var controller = new FakeQuestController();

            QuestOffer blockedOffer = QuestQueries.GetQuestOffers(controller, "NPC").Single();
            Assert.That(blockedOffer.QuestId, Is.EqualTo(quest.QuestId));
            Assert.That(blockedOffer.IsAvailable, Is.False);
            Assert.That(blockedOffer.BlockReason, Is.EqualTo("선행 Quest 미완료"));

            var prerequisiteProgress = new QuestProgress(prerequisite) { state = QuestState.TurnedIn };
            controller.QuestProgress.Add(prerequisite.QuestId, prerequisiteProgress);
            QuestOffer staleOffer = QuestQueries.GetQuestOffers(controller, "NPC").Single();
            Assert.That(staleOffer.IsAvailable, Is.True);

            prerequisiteProgress.state = QuestState.InProgress;
            Assert.That(QuestRunner.TryStartQuest(controller, staleOffer), Is.False);
            Assert.That(controller.QuestProgress.ContainsKey(quest.QuestId), Is.False);

            prerequisiteProgress.state = QuestState.TurnedIn;
            QuestOffer refreshedOffer = QuestQueries.GetQuestOffers(controller, "NPC").Single();
            Assert.That(refreshedOffer.IsAvailable, Is.True);
            Assert.That(QuestRunner.TryStartQuest(controller, refreshedOffer), Is.True);

            QuestProgress createdProgress = controller.QuestProgress[quest.QuestId];
            Assert.That(createdProgress, Is.Not.Null);
            Assert.That(createdProgress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(createdProgress.activeNodeGuids, Does.Contain("objective"));
        }

        [Test]
        public void QuestQueries_GetQuestOffersReturnsAllMatchesWithoutSelectingForTheGame()
        {
            QuestContainer firstQuest = CreateAsset<QuestContainer>();
            firstQuest.QuestId = 50;
            var firstStart = new QuestStartNodeData { Guid = "first-start" };
            var firstObjective = new QuestObjectiveNodeData
            {
                Guid = "first-objective",
                ObjectiveType = "Talk",
                TargetId = 1,
                RequiredAmount = 1
            };
            var firstInteraction = new QuestInteractionEntryNodeData
            {
                Guid = "first-interaction",
                TargetId = "NPC-MULTI"
            };
            var firstOffer = new QuestOfferNodeData
            {
                Guid = "first-offer",
                Priority = 5
            };
            firstQuest.Nodes.Add(firstStart);
            firstQuest.Nodes.Add(firstObjective);
            firstQuest.Nodes.Add(firstInteraction);
            firstQuest.Nodes.Add(firstOffer);
            firstQuest.NodeLinks.Add(Link("first-start", "Next", "first-objective"));
            firstQuest.NodeLinks.Add(Link("first-interaction", "Next", "first-offer"));

            QuestContainer secondQuest = CreateAsset<QuestContainer>();
            secondQuest.QuestId = 51;
            var secondStart = new QuestStartNodeData { Guid = "second-start" };
            var secondObjective = new QuestObjectiveNodeData
            {
                Guid = "second-objective",
                ObjectiveType = "Talk",
                TargetId = 2,
                RequiredAmount = 1
            };
            var secondInteraction = new QuestInteractionEntryNodeData
            {
                Guid = "second-interaction",
                TargetId = "NPC-MULTI"
            };
            var secondOffer = new QuestOfferNodeData
            {
                Guid = "second-offer",
                Priority = 10
            };
            secondQuest.Nodes.Add(secondStart);
            secondQuest.Nodes.Add(secondObjective);
            secondQuest.Nodes.Add(secondInteraction);
            secondQuest.Nodes.Add(secondOffer);
            secondQuest.NodeLinks.Add(Link("second-start", "Next", "second-objective"));
            secondQuest.NodeLinks.Add(Link("second-interaction", "Next", "second-offer"));

            QuestDefinitionRegistry.Initialize(new[] { firstQuest, secondQuest });
            var controller = new FakeQuestController();

            QuestOffer[] offers = QuestQueries.GetQuestOffers(controller, "NPC-MULTI");
            Assert.That(offers, Has.Length.EqualTo(2));
            Assert.That(offers.Select(offer => offer.QuestId),
                Is.EqualTo(new[] { firstQuest.QuestId, secondQuest.QuestId }));
            Assert.That(offers.Select(offer => offer.Priority),
                Is.EqualTo(new[] { 5, 10 }));

            firstOffer.Priority = secondOffer.Priority;
            QuestOffer[] samePriorityOffers = QuestQueries.GetQuestOffers(controller, "NPC-MULTI");
            Assert.That(samePriorityOffers, Has.Length.EqualTo(2));
            Assert.That(samePriorityOffers.Select(offer => offer.QuestId),
                Is.EqualTo(new[] { firstQuest.QuestId, secondQuest.QuestId }));
        }

        [Test]
        public void QuestRunner_CompletesObjectiveAndAdvancesToStateChange()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 20;
            var start = new QuestStartNodeData { Guid = "start" };
            var objective = new QuestObjectiveNodeData
            {
                Guid = "objective",
                ObjectiveType = "Kill",
                TargetId = 3,
                RequiredAmount = 3
            };
            var complete = new QuestStateChangeNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(objective);
            quest.Nodes.Add(complete);
            quest.NodeLinks.Add(Link("start", "Next", "objective"));
            quest.NodeLinks.Add(Link("objective", "Next", "complete"));

            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(quest.QuestId, progress);

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);
            Assert.That(progress.activeNodeGuids, Does.Contain("objective"));

            Assert.That(QuestRunner.AdvanceObjective(controller, quest.QuestId, "objective", 2), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.nodeProgressCounts["objective"], Is.EqualTo(2));
            QuestObjectiveProgress currentObjective = QuestQueries.GetCurrentObjectives(controller, quest.QuestId).Single();
            Assert.That(currentObjective.QuestId, Is.EqualTo(quest.QuestId));
            Assert.That(currentObjective.NodeGuid, Is.EqualTo("objective"));
            Assert.That(currentObjective.ObjectiveType, Is.EqualTo("Kill"));
            Assert.That(currentObjective.TargetId, Is.EqualTo(3));
            Assert.That(currentObjective.CurrentAmount, Is.EqualTo(2));
            Assert.That(currentObjective.RequiredAmount, Is.EqualTo(3));

            Assert.That(QuestRunner.AdvanceObjective(controller, quest.QuestId, "objective"), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(progress.activeNodeGuids, Does.Not.Contain("objective"));
            Assert.That(QuestQueries.GetCurrentObjectives(controller, quest.QuestId), Is.Empty);
        }

        [Test]
        public void QuestRewardNode_ExecutesWithoutChoosingTheQuestCompletionPolicy()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 23;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestRewardNodeData { Guid = "reward" });
            quest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective",
                ObjectiveType = "Continue",
                RequiredAmount = 1
            });
            quest.NodeLinks.Add(Link("start", "Next", "reward"));
            quest.NodeLinks.Add(Link("reward", "Next", "objective"));

            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[quest.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.completedNodeGuids, Does.Contain("reward"));
            Assert.That(progress.activeNodeGuids, Does.Contain("objective"));
        }

        [Test]
        public void QuestRunner_FailsQuestWhenActionCannotExecute()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 24;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestActionNodeData
            {
                Guid = "action",
                Action = new MethodBindingData { Key = "tests.quest.missing-action" }
            });
            quest.NodeLinks.Add(Link("start", "Next", "action"));
            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();
            LogAssert.Expect(LogType.Error, "[Quest] Action 'tests.quest.missing-action'이 등록되지 않았습니다.");

            bool started = QuestRunner.ForceStartQuest(controller, quest.QuestId);

            QuestProgress progress = controller.QuestProgress[quest.QuestId];
            Assert.That(started, Is.False);
            Assert.That(progress.state, Is.EqualTo(QuestState.Failed));
            Assert.That(progress.activeNodeGuids, Is.Empty);
        }

        [TestCase(MethodKind.Action, QuestState.Failed)]
        [TestCase(MethodKind.Action, QuestState.NotStarted)]
        [TestCase(MethodKind.Condition, QuestState.Failed)]
        [TestCase(MethodKind.Condition, QuestState.NotStarted)]
        public void QuestRunner_StopsOldFlowWhenMethodChangesQuestState(MethodKind kind, QuestState state)
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 1201;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            NodeBaseData stop = kind == MethodKind.Action
                ? new QuestActionNodeData { Guid = "stop", Action = new MethodBindingData { Key = "tests.quest.change-run" } }
                : new QuestConditionNodeData { Guid = "stop", Condition = new MethodBindingData { Key = "tests.quest.run-condition" } };
            quest.Nodes.Add(stop);
            quest.Nodes.Add(new QuestObjectiveNodeData { Guid = "unexpected-objective", RequiredAmount = 1 });
            quest.NodeLinks.Add(Link("start", "Next", "stop"));
            quest.NodeLinks.Add(Link("stop", kind == MethodKind.Action ? "Next" : "True", "unexpected-objective"));

            questRunAction = context => Assert.That(QuestRunner.SetQuestState(context.Controller, quest.QuestId, state), Is.True);
            questRunCondition = context =>
            {
                questRunAction(context);
                return true;
            };
            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[quest.QuestId];
            Assert.That(progress.state, Is.EqualTo(state));
            Assert.That(progress.activeNodeGuids, Is.Empty);
            Assert.That(progress.completedNodeGuids, Does.Not.Contain("stop"));
        }

        [Test]
        public void QuestRunner_ResetAndRestartInsideActionDoesNotResumeOldRun()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 1202;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestConditionNodeData { Guid = "route", Condition = new MethodBindingData { Key = "tests.quest.run-condition" } });
            quest.Nodes.Add(new QuestActionNodeData { Guid = "restart", Action = new MethodBindingData { Key = "tests.quest.change-run" } });
            quest.Nodes.Add(new QuestObjectiveNodeData { Guid = "old-objective", RequiredAmount = 1 });
            quest.Nodes.Add(new QuestObjectiveNodeData { Guid = "new-objective", RequiredAmount = 1 });
            quest.NodeLinks.Add(Link("start", "Next", "route"));
            quest.NodeLinks.Add(Link("route", "False", "restart"));
            quest.NodeLinks.Add(Link("route", "True", "new-objective"));
            quest.NodeLinks.Add(Link("restart", "Next", "old-objective"));

            bool restarted = false;
            questRunCondition = _ => restarted;
            questRunAction = context =>
            {
                restarted = true;
                Assert.That(QuestRunner.ResetQuest(context.Controller, quest.QuestId), Is.True);
                Assert.That(QuestRunner.ForceStartQuest(context.Controller, quest.QuestId), Is.True);
            };
            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[quest.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.activeNodeGuids, Is.EqualTo(new[] { "new-objective" }));
            Assert.That(progress.completedNodeGuids, Does.Not.Contain("restart"));
            Assert.That(controller.StatusChangedQuestIds, Has.Count.EqualTo(2));
        }

        [Test]
        public void QuestRunner_DoesNotStartNodesIfWaitingQuestStopsTheNewQuest()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 1203;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestObjectiveNodeData { Guid = "unexpected-objective", RequiredAmount = 1 });
            quest.NodeLinks.Add(Link("start", "Next", "unexpected-objective"));

            QuestContainer waiting = CreateAsset<QuestContainer>();
            waiting.QuestId = 1204;
            waiting.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            waiting.Nodes.Add(new QuestWaitForQuestNodeData { Guid = "wait", TargetQuestId = quest.QuestId, RequiredState = QuestState.InProgress });
            waiting.Nodes.Add(new QuestActionNodeData { Guid = "stop-other-quest", Action = new MethodBindingData { Key = "tests.quest.change-run" } });
            waiting.Nodes.Add(new QuestObjectiveNodeData { Guid = "waiting-objective", RequiredAmount = 1 });
            waiting.NodeLinks.Add(Link("start", "Next", "wait"));
            waiting.NodeLinks.Add(Link("wait", "Next", "stop-other-quest"));
            waiting.NodeLinks.Add(Link("stop-other-quest", "Next", "waiting-objective"));

            questRunAction = context => Assert.That(QuestRunner.SetQuestState(context.Controller, quest.QuestId, QuestState.Failed), Is.True);
            QuestDefinitionRegistry.Initialize(new[] { quest, waiting });
            var controller = new FakeQuestController();
            Assert.That(QuestRunner.ForceStartQuest(controller, waiting.QuestId), Is.True);

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);

            Assert.That(controller.QuestProgress[quest.QuestId].state, Is.EqualTo(QuestState.Failed));
            Assert.That(controller.QuestProgress[quest.QuestId].activeNodeGuids, Is.Empty);
            Assert.That(controller.QuestProgress[waiting.QuestId].activeNodeGuids, Is.EqualTo(new[] { "waiting-objective" }));
        }

        [Test]
        public void QuestDefinitionRegistry_RejectsLinksToMissingNodesDuringInitialization()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 21;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.NodeLinks.Add(Link("start", "Next", "missing"));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => QuestDefinitionRegistry.Initialize(new[] { quest }));

            Assert.That(exception.Message, Does.Contain("존재하지 않는 노드"));
        }

        [Test]
        public void QuestDefinitionRegistry_RejectsNonPositiveQuestId()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 0;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => QuestDefinitionRegistry.Initialize(new[] { quest }));

            Assert.That(exception.Message, Does.Contain("양수를 사용"));
        }

        [Test]
        public void QuestDefinitionRegistry_UsesCachedIndexUntilReinitialized()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 22;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "old-start" });
            quest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "old-objective",
                ObjectiveType = "Old"
            });
            quest.NodeLinks.Add(Link("old-start", "Next", "old-objective"));
            QuestDefinitionRegistry.Initialize(new[] { quest });

            quest.Nodes = new List<NodeBaseData>
            {
                new QuestStartNodeData { Guid = "new-start" },
                new QuestStateChangeNodeData
                {
                    Guid = "new-state",
                    NewState = QuestState.CanComplete
                }
            };
            quest.NodeLinks = new List<NodeLinkData>
            {
                Link("new-start", "Next", "new-state")
            };

            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(quest.QuestId, progress);

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.activeNodeGuids, Does.Contain("old-objective"));

            QuestDefinitionRegistry.Initialize(new[] { quest });
            var refreshedController = new FakeQuestController();
            var refreshedProgress = new QuestProgress(quest) { state = QuestState.NotStarted };
            refreshedController.QuestProgress.Add(quest.QuestId, refreshedProgress);

            Assert.That(QuestRunner.ForceStartQuest(refreshedController, quest.QuestId), Is.True);
            Assert.That(refreshedProgress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void QuestRunner_AndGateDerivesRequiredCountFromConnectedBranches()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 30;
            var start = new QuestStartNodeData { Guid = "start" };
            var first = new QuestObjectiveNodeData
            {
                Guid = "first",
                ObjectiveType = "Collect",
                TargetId = 1,
                RequiredAmount = 1
            };
            var second = new QuestObjectiveNodeData
            {
                Guid = "second",
                ObjectiveType = "Collect",
                TargetId = 2,
                RequiredAmount = 1
            };
#pragma warning disable CS0618
            var gate = new QuestAndGateNodeData { Guid = "gate" };
#pragma warning restore CS0618
            var complete = new QuestStateChangeNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(first);
            quest.Nodes.Add(second);
            quest.Nodes.Add(gate);
            quest.Nodes.Add(complete);
            quest.NodeLinks.Add(Link("start", "Next", "first"));
            quest.NodeLinks.Add(Link("start", "Next", "second"));
            quest.NodeLinks.Add(Link("first", "Next", "gate"));
            quest.NodeLinks.Add(Link("second", "Next", "gate"));
            quest.NodeLinks.Add(Link("gate", "Next", "complete"));

            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(quest.QuestId, progress);

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);
            QuestRunner.ReportObjectiveProgress(controller, "Collect", 1, 1);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));

            QuestRunner.ReportObjectiveProgress(controller, "Collect", 2, 1);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void QuestRunner_ReportsOneEventToAllMatchingParallelObjectives()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 36;
            var start = new QuestStartNodeData { Guid = "start" };
            var first = new QuestObjectiveNodeData
            {
                Guid = "first",
                ObjectiveType = "Collect",
                TargetId = 1,
                RequiredAmount = 1
            };
            var second = new QuestObjectiveNodeData
            {
                Guid = "second",
                ObjectiveType = "Collect",
                TargetId = 1,
                RequiredAmount = 1
            };
            var gate = new QuestAndGateNodeData { Guid = "gate" };
            var complete = new QuestStateChangeNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(first);
            quest.Nodes.Add(second);
            quest.Nodes.Add(gate);
            quest.Nodes.Add(complete);
            quest.NodeLinks.Add(Link("start", "Next", "first"));
            quest.NodeLinks.Add(Link("start", "Next", "second"));
            quest.NodeLinks.Add(Link("first", "Next", "gate"));
            quest.NodeLinks.Add(Link("second", "Next", "gate"));
            quest.NodeLinks.Add(Link("gate", "Next", "complete"));

            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);
            QuestRunner.ReportObjectiveProgress(controller, "Collect", 1, 1);

            QuestProgress progress = controller.QuestProgress[quest.QuestId];
            Assert.That(progress.nodeProgressCounts["first"], Is.EqualTo(1));
            Assert.That(progress.nodeProgressCounts["second"], Is.EqualTo(1));
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void QuestRunner_ProgressesMultipleQuestsIndependently()
        {
            QuestContainer firstQuest = CreateAsset<QuestContainer>();
            firstQuest.QuestId = 31;
            firstQuest.Nodes.Add(new QuestStartNodeData { Guid = "first-start" });
            firstQuest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "first-objective",
                ObjectiveType = "Kill",
                TargetId = 101,
                RequiredAmount = 1
            });
            firstQuest.Nodes.Add(new QuestStateChangeNodeData
            {
                Guid = "first-complete",
                NewState = QuestState.CanComplete
            });
            firstQuest.NodeLinks.Add(Link("first-start", "Next", "first-objective"));
            firstQuest.NodeLinks.Add(Link("first-objective", "Next", "first-complete"));

            QuestContainer secondQuest = CreateAsset<QuestContainer>();
            secondQuest.QuestId = 32;
            secondQuest.Nodes.Add(new QuestStartNodeData { Guid = "second-start" });
            secondQuest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "second-objective",
                ObjectiveType = "Kill",
                TargetId = 202,
                RequiredAmount = 1
            });
            secondQuest.Nodes.Add(new QuestStateChangeNodeData
            {
                Guid = "second-complete",
                NewState = QuestState.CanComplete
            });
            secondQuest.NodeLinks.Add(Link("second-start", "Next", "second-objective"));
            secondQuest.NodeLinks.Add(Link("second-objective", "Next", "second-complete"));

            QuestDefinitionRegistry.Initialize(new[] { firstQuest, secondQuest });
            var controller = new FakeQuestController();

            Assert.That(QuestRunner.ForceStartQuest(controller, firstQuest.QuestId), Is.True);
            Assert.That(QuestRunner.ForceStartQuest(controller, secondQuest.QuestId), Is.True);

            QuestProgress firstProgress = controller.QuestProgress[firstQuest.QuestId];
            QuestProgress secondProgress = controller.QuestProgress[secondQuest.QuestId];
            Assert.That(firstProgress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(secondProgress.state, Is.EqualTo(QuestState.InProgress));

            QuestRunner.ReportObjectiveProgress(controller, "Kill", 101, 1);

            Assert.That(firstProgress.state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(secondProgress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(secondProgress.activeNodeGuids, Does.Contain("second-objective"));
            Assert.That(secondProgress.nodeProgressCounts["second-objective"], Is.Zero);
        }

        [Test]
        public void QuestRunner_ResetsAndRestartsQuestWithCleanProgress()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 33;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective",
                ObjectiveType = "Collect",
                TargetId = 7,
                RequiredAmount = 3
            });
            quest.NodeLinks.Add(Link("start", "Next", "objective"));

            QuestDefinitionRegistry.Initialize(new[] { quest });
            var controller = new FakeQuestController();
            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[quest.QuestId];
            QuestRunner.ReportObjectiveProgress(controller, "Collect", 7, 1);
            progress.completedNodeGuids.Add("old-action");
            progress.completedGateInputs.Add("old-gate|old-input");

            Assert.That(QuestRunner.ResetQuest(controller, quest.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.NotStarted));
            Assert.That(progress.activeNodeGuids, Is.Empty);
            Assert.That(progress.nodeProgressCounts, Is.Empty);
            Assert.That(progress.completedNodeGuids, Is.Empty);
            Assert.That(progress.completedGateInputs, Is.Empty);

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.activeNodeGuids, Is.EquivalentTo(new[] { "objective" }));
            Assert.That(progress.nodeProgressCounts["objective"], Is.Zero);

            progress.state = QuestState.TurnedIn;
            Assert.That(QuestRunner.ResetQuest(controller, quest.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.NotStarted));
        }

        [Test]
        public void QuestWaitForQuest_DoesNotStartTargetAndUsesTheStateChosenByTheDesigner()
        {
            QuestContainer childQuest = CreateAsset<QuestContainer>();
            childQuest.QuestId = 34;
            childQuest.Nodes.Add(new QuestStartNodeData { Guid = "child-start" });
            childQuest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "child-objective",
                ObjectiveType = "Talk",
                TargetId = 10,
                RequiredAmount = 1
            });
            childQuest.Nodes.Add(new QuestStateChangeNodeData
            {
                Guid = "child-ready",
                NewState = QuestState.CanComplete
            });
            childQuest.NodeLinks.Add(Link("child-start", "Next", "child-objective"));
            childQuest.NodeLinks.Add(Link("child-objective", "Next", "child-ready"));

            QuestContainer parentQuest = CreateAsset<QuestContainer>();
            parentQuest.QuestId = 35;
            parentQuest.Nodes.Add(new QuestStartNodeData { Guid = "parent-start" });
            parentQuest.Nodes.Add(new QuestWaitForQuestNodeData
            {
                Guid = "sub-quest",
                TargetQuestId = childQuest.QuestId,
                RequiredState = QuestState.CanComplete
            });
            parentQuest.Nodes.Add(new QuestStateChangeNodeData
            {
                Guid = "parent-complete",
                NewState = QuestState.TurnedIn
            });
            parentQuest.NodeLinks.Add(Link("parent-start", "Next", "sub-quest"));
            parentQuest.NodeLinks.Add(Link("sub-quest", "Next", "parent-complete"));

            QuestDefinitionRegistry.Initialize(new[] { parentQuest, childQuest });
            var controller = new FakeQuestController();

            Assert.That(QuestRunner.ForceStartQuest(controller, parentQuest.QuestId), Is.True);
            Assert.That(controller.QuestProgress.ContainsKey(childQuest.QuestId), Is.False);
            Assert.That(QuestRunner.ForceStartQuest(controller, childQuest.QuestId), Is.True);
            Assert.That(QuestRunner.AdvanceObjective(controller, childQuest.QuestId, "child-objective"), Is.True);
            Assert.That(controller.QuestProgress[childQuest.QuestId].state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(controller.QuestProgress[parentQuest.QuestId].state, Is.EqualTo(QuestState.TurnedIn));
        }

        [Test]
        public void QuestSaveData_RoundTripsDictionaryAndFlowCollections()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 77;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective-a",
                ObjectiveType = "Collect",
                RequiredAmount = 10
            });
            quest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective-c",
                ObjectiveType = "Visit",
                RequiredAmount = 2
            });
            quest.Nodes.Add(new QuestActionNodeData { Guid = "action-1" });
            quest.Nodes.Add(new QuestAndGateNodeData { Guid = "gate-1" });
            quest.NodeLinks.Add(Link("start", "Next", "objective-a"));
            quest.NodeLinks.Add(Link("objective-c", "Next", "gate-1"));
            QuestDefinitionRegistry.Initialize(new[] { quest });

            var source = new FakeQuestController();
            source.QuestProgress.Add(quest.QuestId, new QuestProgress(quest)
            {
                state = QuestState.InProgress,
                activeNodeGuids = new List<string> { "objective-a" },
                nodeProgressCounts = new Dictionary<string, int>
                {
                    ["objective-a"] = 4,
                    ["objective-c"] = 2
                },
                completedNodeGuids = new List<string> { "objective-c", "action-1" },
                completedGateInputs = new List<string> { "gate-1|objective-c" }
            });

            string json = QuestSaveData.Capture(source).ToJson();
            Assert.That(json, Does.Not.Contain("currentObjectiveCount"));
            Assert.That(QuestSaveData.TryFromJson(json, out QuestSaveData parsed, out string parseError),
                Is.True,
                parseError);

            var target = new FakeQuestController();
            Assert.That(parsed.TryApplyTo(target, replaceExisting: true, out string restoreError),
                Is.True,
                restoreError);

            QuestProgress restored = target.QuestProgress[quest.QuestId];
            Assert.That(restored.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(restored.activeNodeGuids, Is.EquivalentTo(new[] { "objective-a" }));
            Assert.That(restored.nodeProgressCounts["objective-a"], Is.EqualTo(4));
            Assert.That(restored.nodeProgressCounts["objective-c"], Is.EqualTo(2));
            Assert.That(restored.completedNodeGuids, Does.Contain("objective-c"));
            Assert.That(restored.completedNodeGuids, Does.Contain("action-1"));
            Assert.That(restored.completedGateInputs, Does.Contain("gate-1|objective-c"));
        }

        [Test]
        public void QuestSaveData_RejectsUnknownNodeBeforeChangingController()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 82;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = 1 });
            quest.NodeLinks.Add(Link("start", "Next", "objective"));
            QuestDefinitionRegistry.Initialize(new[] { quest });

            var saveData = new QuestSaveData
            {
                quests = new List<QuestProgressSaveData>
                {
                    new()
                    {
                        questId = quest.QuestId,
                        definitionSchemaVersion = quest.SchemaVersion,
                        state = QuestState.InProgress,
                        activeNodeGuids = new List<string> { "missing-objective" }
                    }
                }
            };
            var target = new FakeQuestController();
            target.QuestProgress.Add(999, new QuestProgress { questId = 999 });

            bool applied = saveData.TryApplyTo(target, replaceExisting: true, out string error);

            Assert.That(applied, Is.False);
            Assert.That(error, Does.Contain("현재 정의에 없습니다"));
            Assert.That(target.QuestProgress.ContainsKey(999), Is.True);
        }

        [Test]
        public void QuestSaveData_RejectsRequiredCountForActiveObjective()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 83;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective",
                RequiredAmount = 3
            });
            quest.NodeLinks.Add(Link("start", "Next", "objective"));
            QuestDefinitionRegistry.Initialize(new[] { quest });

            var saveData = new QuestSaveData
            {
                quests = new List<QuestProgressSaveData>
                {
                    new()
                    {
                        questId = quest.QuestId,
                        definitionSchemaVersion = quest.SchemaVersion,
                        state = QuestState.InProgress,
                        activeNodeGuids = new List<string> { "objective" },
                        nodeProgressCounts = new List<QuestNodeProgressSaveData>
                        {
                            new() { nodeGuid = "objective", count = 3 }
                        }
                    }
                }
            };
            var target = new FakeQuestController();
            target.QuestProgress.Add(999, new QuestProgress { questId = 999 });

            bool applied = saveData.TryApplyTo(target, replaceExisting: true, out string error);

            Assert.That(applied, Is.False);
            Assert.That(error, Does.Contain("활성 Objective"));
            Assert.That(target.QuestProgress.ContainsKey(999), Is.True);
        }

        [Test]
        public void QuestRunner_NotifyRestoredQuestsNotifiesOnlyActiveStates()
        {
            QuestContainer inProgressQuest = CreateAsset<QuestContainer>();
            inProgressQuest.QuestId = 78;
            inProgressQuest.Nodes.Add(new QuestStartNodeData { Guid = "in-progress-start" });

            QuestContainer canCompleteQuest = CreateAsset<QuestContainer>();
            canCompleteQuest.QuestId = 79;
            canCompleteQuest.Nodes.Add(new QuestStartNodeData { Guid = "can-complete-start" });

            QuestContainer notStartedQuest = CreateAsset<QuestContainer>();
            notStartedQuest.QuestId = 80;
            notStartedQuest.Nodes.Add(new QuestStartNodeData { Guid = "not-started-start" });

            QuestContainer turnedInQuest = CreateAsset<QuestContainer>();
            turnedInQuest.QuestId = 81;
            turnedInQuest.Nodes.Add(new QuestStartNodeData { Guid = "turned-in-start" });

            QuestDefinitionRegistry.Initialize(new[]
            {
                inProgressQuest,
                canCompleteQuest,
                notStartedQuest,
                turnedInQuest
            });

            var controller = new FakeQuestController();
            controller.QuestProgress.Add(inProgressQuest.QuestId,
                new QuestProgress(inProgressQuest) { state = QuestState.InProgress });
            controller.QuestProgress.Add(canCompleteQuest.QuestId,
                new QuestProgress(canCompleteQuest) { state = QuestState.CanComplete });
            controller.QuestProgress.Add(notStartedQuest.QuestId,
                new QuestProgress(notStartedQuest) { state = QuestState.NotStarted });
            controller.QuestProgress.Add(turnedInQuest.QuestId,
                new QuestProgress(turnedInQuest) { state = QuestState.TurnedIn });

            QuestRunner.NotifyRestoredQuests(controller);

            Assert.That(controller.StatusChangedQuestIds,
                Is.EquivalentTo(new[] { inProgressQuest.QuestId, canCompleteQuest.QuestId }));
        }

        [Test]
        public void GraphAssetMigrator_UpgradesLegacySerializationAndIsIdempotent()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var choice = new DialogueChoiceData
            {
                PortName = string.Empty,
                SelectionAction = null,
                VisibilityCondition = null
            };
            var line = new DialogueLineNodeData
            {
                Guid = "line",
                EnterAction = null
            };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData> { choice }
            };
            graph.Nodes.Add(line);
            graph.Nodes.Add(choiceNode);
            graph.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = "line",
                StartPortName = "Next",
                TargetNodeGuid = "target",
                TargetPortName = string.Empty
            });

            Assert.That(GraphAssetMigrator.TryMigrate(
                    graph,
                    out GraphAssetMigrationResult first,
                    out string firstError),
                Is.True,
                firstError);
            Assert.That(first.FromVersion, Is.Zero);
            Assert.That(first.ToVersion, Is.EqualTo(GraphAssetMigrator.CurrentVersion));
            Assert.That(first.Changed, Is.True);
            Assert.That(line.EnterAction, Is.Not.Null);
            Assert.That(line.EnterAction.Arguments, Is.Not.Null);
            Assert.That(choice.SelectionAction, Is.Not.Null);
            Assert.That(choice.SelectionAction.Arguments, Is.Not.Null);
            Assert.That(choice.VisibilityCondition, Is.Not.Null);
            Assert.That(choice.VisibilityCondition.Arguments, Is.Not.Null);
            Assert.That(choice.PortName, Is.Not.Empty);
            Assert.That(graph.NodeLinks[0].TargetPortName, Is.EqualTo("Input"));

            Assert.That(GraphAssetMigrator.TryMigrate(
                    graph,
                    out GraphAssetMigrationResult second,
                    out string secondError),
                Is.True,
                secondError);
            Assert.That(second.Changed, Is.False);
        }

        [Test]
        public void GraphAssetMigrator_AppliesCommonAndQuestMigrationsToQuestContainer()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            var action = new QuestActionNodeData { Action = null };
            var condition = new QuestConditionNodeData { Condition = null };
            var reward = new QuestRewardNodeData { RewardAction = null };
            graph.Nodes.Add(action);
            graph.Nodes.Add(condition);
            graph.Nodes.Add(reward);
            graph.NodeLinks.Add(new NodeLinkData { TargetPortName = string.Empty });

            Assert.That(GraphAssetMigrator.TryMigrate(
                    graph,
                    out GraphAssetMigrationResult result,
                    out string error),
                Is.True,
                error);

            Assert.That(result.Changed, Is.True);
            Assert.That(action.Action, Is.Not.Null);
            Assert.That(action.Action.Arguments, Is.Not.Null);
            Assert.That(condition.Condition, Is.Not.Null);
            Assert.That(condition.Condition.Arguments, Is.Not.Null);
            Assert.That(reward.RewardAction, Is.Not.Null);
            Assert.That(reward.RewardAction.Arguments, Is.Not.Null);
            Assert.That(graph.NodeLinks[0].TargetPortName, Is.EqualTo("Input"));
        }

        [Test]
        public void GraphAssetMigrator_ConvertsLegacyReadyStateToNotStarted()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            var condition = new QuestStateConditionNodeData
            {
                TargetState = (QuestState)1
            };
            var stateChange = new QuestStateChangeNodeData
            {
                NewState = (QuestState)1
            };
            graph.Nodes.Add(condition);
            graph.Nodes.Add(stateChange);

            FieldInfo schemaField = typeof(GraphContainer).GetField(
                "schemaVersion",
                BindingFlags.Instance | BindingFlags.NonPublic);
            schemaField.SetValue(graph, 1);

            Assert.That(GraphAssetMigrator.TryMigrate(
                    graph,
                    out GraphAssetMigrationResult result,
                    out string error),
                Is.True,
                error);
            Assert.That(result.FromVersion, Is.EqualTo(1));
            Assert.That(result.ToVersion, Is.EqualTo(GraphAssetMigrator.CurrentVersion));
            Assert.That(condition.TargetState, Is.EqualTo(QuestState.NotStarted));
            Assert.That(stateChange.NewState, Is.EqualTo(QuestState.NotStarted));
        }

        [Test]
        public void GraphAssetMigrator_RunsCommonMigrationBeforeDomainMigration()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes = null;
            graph.NodeLinks = null;

            Assert.That(GraphAssetMigrator.TryMigrate(graph, out _, out string error), Is.True, error);
            Assert.That(graph.Nodes, Is.Not.Null);
            Assert.That(graph.NodeLinks, Is.Not.Null);
        }

        [Test]
        public void GraphAssetMigrator_RejectsFutureSchemaWithoutChangingIt()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            FieldInfo schemaField = typeof(GraphContainer).GetField(
                "schemaVersion",
                BindingFlags.Instance | BindingFlags.NonPublic);
            int futureVersion = GraphAssetMigrator.CurrentVersion + 1;
            schemaField.SetValue(graph, futureVersion);

            Assert.That(GraphAssetMigrator.TryMigrate(graph, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("미래 스키마"));
            Assert.That(graph.SchemaVersion, Is.EqualTo(futureVersion));
        }

        [Test]
        public void QuestSaveData_MigratesVersionOneAndRejectsFutureVersions()
        {
            const string versionOneJson =
                "{\"schemaVersion\":1,\"quests\":[{\"questId\":91,\"state\":2," +
                "\"currentNodeGuid\":\"objective\",\"currentObjectiveCount\":3," +
                "\"activeNodeGuids\":[\"objective\"],\"nodeProgressCounts\":[]," +
                "\"completedNodeGuids\":[],\"completedGateInputs\":[]}]}";

            Assert.That(QuestSaveData.TryFromJson(
                    versionOneJson,
                    out QuestSaveData migrated,
                    out string migrationError),
                Is.True,
                migrationError);
            Assert.That(migrated.schemaVersion, Is.EqualTo(QuestSaveData.CurrentSchemaVersion));
            Assert.That(migrated.quests.Single().definitionSchemaVersion,
                Is.EqualTo(GraphAssetMigrator.CurrentVersion));

            string futureJson = $"{{\"schemaVersion\":{QuestSaveData.CurrentSchemaVersion + 1},\"quests\":[]}}";
            Assert.That(QuestSaveData.TryFromJson(futureJson, out _, out string futureError), Is.False);
            Assert.That(futureError, Does.Contain("지원 버전"));
        }

        [Test]
        public void QuestSaveData_MigratesVersionTwoReadyStateToNotStarted()
        {
            const string versionTwoJson =
                "{\"schemaVersion\":2,\"quests\":[{\"questId\":92,\"definitionSchemaVersion\":1," +
                "\"state\":1,\"currentNodeGuid\":\"\",\"activeNodeGuids\":[]," +
                "\"nodeProgressCounts\":[],\"completedNodeGuids\":[],\"completedGateInputs\":[]}]}";

            Assert.That(QuestSaveData.TryFromJson(
                    versionTwoJson,
                    out QuestSaveData migrated,
                    out string error),
                Is.True,
                error);
            Assert.That(migrated.schemaVersion, Is.EqualTo(QuestSaveData.CurrentSchemaVersion));
            Assert.That(migrated.quests.Single().state, Is.EqualTo(QuestState.NotStarted));
        }

        [Test]
        public void QuestRunner_InvokesAttributedConditionAndTypedAction()
        {
            attributedQuestActionAmount = 0;
            attributedQuestActionFlag = false;

            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 88;
            var start = new QuestStartNodeData { Guid = "start" };
            var condition = new QuestConditionNodeData
            {
                Guid = "condition",
                Condition = new MethodBindingData
                {
                    Key = "tests.quest.is-ready",
                    Arguments = CreateQuestArguments(
                        nameof(IsAttributedQuestReady),
                        MethodKind.Condition,
                        ("arg0", 42))
                }
            };
            var action = new QuestActionNodeData
            {
                Guid = "action",
                Action = new MethodBindingData
                {
                    Key = "tests.quest.record-action",
                    Arguments = CreateQuestArguments(
                        nameof(RecordAttributedQuestAction),
                        MethodKind.Action,
                        ("arg0", 7),
                        ("arg1", true))
                }
            };
            var complete = new QuestStateChangeNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(condition);
            quest.Nodes.Add(action);
            quest.Nodes.Add(complete);
            quest.NodeLinks.Add(Link("start", "Next", "condition"));
            quest.NodeLinks.Add(Link("condition", "True", "action"));
            quest.NodeLinks.Add(Link("action", "Next", "complete"));

            QuestDefinitionRegistry.Initialize(new[] { quest });
            QuestMethodInvoker.Initialize();
            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(quest.QuestId, progress);

            Assert.That(QuestRunner.ForceStartQuest(controller, quest.QuestId), Is.True);

            Assert.That(attributedQuestActionAmount, Is.EqualTo(7));
            Assert.That(attributedQuestActionFlag, Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void DialogueManager_HidesChoicesWhoseConditionIsFalse()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var line = new DialogueLineNodeData
            {
                Guid = "line",
                DialogueText = "Choose"
            };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new()
                    {
                        PortName = "visible",
                        ChoiceText = "Visible",
                        VisibilityCondition = new MethodBindingData
                        {
                            Key = "tests.dialogue.choice-visible",
                            Arguments = CreateDialogueArguments(
                                nameof(IsDialogueChoiceVisible),
                                MethodKind.Condition,
                                ("arg0", true))
                        }
                    },
                    new()
                    {
                        PortName = "hidden",
                        ChoiceText = "Hidden",
                        VisibilityCondition = new MethodBindingData
                        {
                            Key = "tests.dialogue.choice-visible",
                            Arguments = CreateDialogueArguments(
                                nameof(IsDialogueChoiceVisible),
                                MethodKind.Condition,
                                ("arg0", false))
                        }
                    }
                }
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.Nodes.Add(choiceNode);
            graph.NodeLinks.Add(Link("entry", "Next", "line"));
            graph.NodeLinks.Add(Link("line", "Next", "choice"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            IReadOnlyList<DialogueChoiceData> shown = null;
            void CaptureChoices(IReadOnlyList<DialogueChoiceData> choices) => shown = choices;
            DialogueManager.Instance.ShowChoices += CaptureChoices;
            try
            {
                DialogueMethodInvoker.Initialize();
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor)),
                    Is.True);
                Assert.That(shown, Is.Null);
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.False);

                Assert.That(
                    DialogueManager.Instance.ContinueDialogue(DialogueManager.Instance.CurrentPromptId),
                    Is.True);

                Assert.That(shown, Is.Not.Null);
                Assert.That(shown.Select(choice => choice.PortName), Is.EqualTo(new[] { "visible" }));
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.True);
            }
            finally
            {
                DialogueManager.Instance.ShowChoices -= CaptureChoices;
            }
        }

        [Test]
        public void DialogueManager_UsesDefaultWhenEveryChoiceIsHidden()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new()
                    {
                        PortName = "hidden",
                        ChoiceText = "Hidden",
                        VisibilityCondition = new MethodBindingData
                        {
                            Key = "tests.dialogue.choice-visible",
                            Arguments = CreateDialogueArguments(
                                nameof(IsDialogueChoiceVisible),
                                MethodKind.Condition,
                                ("arg0", false))
                        }
                    }
                }
            };
            var defaultLine = new DialogueLineNodeData
            {
                Guid = "default",
                DialogueText = "Default"
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(defaultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", DialoguePortNames.Default, "default"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            DialogueLineNodeData shownLine = null;
            int choicesShownCount = 0;
            void CaptureLine(DialogueLineNodeData line) => shownLine = line;
            void CaptureChoices(IReadOnlyList<DialogueChoiceData> _) => choicesShownCount++;
            DialogueManager.Instance.ShowLine += CaptureLine;
            DialogueManager.Instance.ShowChoices += CaptureChoices;
            try
            {
                DialogueMethodInvoker.Initialize();
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor)),
                    Is.True);
                Assert.That(shownLine, Is.SameAs(defaultLine));
                Assert.That(choicesShownCount, Is.Zero);
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.False);
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
                DialogueManager.Instance.ShowChoices -= CaptureChoices;
            }
        }

        [Test]
        public void DialogueManager_ExecutesChoiceActionAndFollowsSelectedPort()
        {
            dialogueChoiceActionCount = 0;
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var selectedChoice = new DialogueChoiceData
            {
                PortName = "accept",
                ChoiceText = "Accept",
                SelectionAction = new MethodBindingData
                {
                    Key = "tests.dialogue.choice-action"
                }
            };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData> { selectedChoice }
            };
            var resultLine = new DialogueLineNodeData
            {
                Guid = "result",
                DialogueText = "Accepted"
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(resultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", "accept", "result"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            DialogueLineNodeData shownLine = null;
            void CaptureLine(DialogueLineNodeData line) => shownLine = line;
            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                DialogueMethodInvoker.Initialize();
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor)),
                    Is.True);
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.True);

                Assert.That(
                    DialogueManager.Instance.SelectChoice(DialogueManager.Instance.CurrentPromptId, selectedChoice),
                    Is.True);

                Assert.That(dialogueChoiceActionCount, Is.EqualTo(1));
                Assert.That(shownLine, Is.SameAs(resultLine));
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.False);
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
            }
        }

        [Test]
        public void DialogueManager_DoesNotOverwriteConversationStartedByCompletionCallback()
        {
            DialogueContainer firstGraph = CreateAsset<DialogueContainer>();
            var firstEntry = new DialogueEntryNodeData { Guid = "first-entry", EntryId = "Default" };
            var endingLine = new DialogueLineNodeData
            {
                Guid = "ending-line",
                DialogueText = "End",
                EnterAction = new MethodBindingData { Key = "tests.dialogue.end-current" }
            };
            firstGraph.Nodes.Add(firstEntry);
            firstGraph.Nodes.Add(endingLine);
            firstGraph.NodeLinks.Add(Link("first-entry", "Next", "ending-line"));

            DialogueContainer secondGraph = CreateAsset<DialogueContainer>();
            var secondEntry = new DialogueEntryNodeData { Guid = "second-entry", EntryId = "Default" };
            var waitSignal = new DialogueWaitSignalNodeData
            {
                Guid = "wait-signal",
                SignalKey = "continue"
            };
            var resultLine = new DialogueLineNodeData
            {
                Guid = "result-line",
                DialogueText = "Continued"
            };
            secondGraph.Nodes.Add(secondEntry);
            secondGraph.Nodes.Add(waitSignal);
            secondGraph.Nodes.Add(resultLine);
            secondGraph.NodeLinks.Add(Link("second-entry", "Next", "wait-signal"));
            secondGraph.NodeLinks.Add(Link("wait-signal", "Next", "result-line"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            var executionContext = new DialogueExecutionContext(speaker, interactor);
            DialogueLineNodeData shownLine = null;
            bool secondStarted = false;
            void CaptureLine(DialogueLineNodeData line) => shownLine = line;
            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                DialogueMethodInvoker.Initialize();
                bool firstStarted = DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(firstGraph, "Default"),
                    executionContext,
                    () =>
                    {
                        secondStarted = DialogueManager.Instance.StartConversation(
                            new DialogueEntryPoint(secondGraph, "Default"),
                            executionContext);
                        DialogueManager.Instance.SendSignal("continue");
                    });

                Assert.That(firstStarted, Is.True);
                Assert.That(secondStarted, Is.True);
                Assert.That(DialogueManager.Instance.IsConversationActive, Is.True);
                Assert.That(shownLine, Is.SameAs(resultLine));
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
            }
        }

        [Test]
        public void DialogueManager_SynchronousContinue_DoesNotReenterLineEvent()
        {
            const int lineCount = 16;
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var end = new DialogueEndNodeData { Guid = "end" };
            graph.Nodes.Add(entry);

            string previousGuid = entry.Guid;
            for (int i = 0; i < lineCount; i++)
            {
                var line = new DialogueLineNodeData
                {
                    Guid = $"line-{i}",
                    DialogueText = $"Line {i}"
                };
                graph.Nodes.Add(line);
                graph.NodeLinks.Add(Link(previousGuid, "Next", line.Guid));
                previousGuid = line.Guid;
            }
            graph.Nodes.Add(end);
            graph.NodeLinks.Add(Link(previousGuid, "Next", end.Guid));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            int callbackDepth = 0;
            int maxCallbackDepth = 0;
            int shownCount = 0;
            bool everyInputAccepted = true;

            void ContinueImmediately(DialogueLineNodeData _)
            {
                callbackDepth++;
                try
                {
                    maxCallbackDepth = Math.Max(maxCallbackDepth, callbackDepth);
                    shownCount++;
                    int promptId = DialogueManager.Instance.CurrentPromptId;
                    everyInputAccepted &= DialogueManager.Instance.ContinueDialogue(promptId);
                }
                finally
                {
                    callbackDepth--;
                }
            }

            DialogueManager.Instance.ShowLine += ContinueImmediately;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor)),
                    Is.True);

                Assert.That(everyInputAccepted, Is.True);
                Assert.That(shownCount, Is.EqualTo(lineCount));
                Assert.That(maxCallbackDepth, Is.EqualTo(1));
                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= ContinueImmediately;
            }
        }

        [Test]
        public void DialogueManager_RejectsStalePromptAndExposesCurrentLine()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var firstLine = new DialogueLineNodeData { Guid = "line-1", DialogueText = "First" };
            var secondLine = new DialogueLineNodeData { Guid = "line-2", DialogueText = "Second" };
            var end = new DialogueEndNodeData { Guid = "end" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(firstLine);
            graph.Nodes.Add(secondLine);
            graph.Nodes.Add(end);
            graph.NodeLinks.Add(Link("entry", "Next", "line-1"));
            graph.NodeLinks.Add(Link("line-1", "Next", "line-2"));
            graph.NodeLinks.Add(Link("line-2", "Next", "end"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            var renderedLines = new List<DialogueLineNodeData>();
            void CaptureLine(DialogueLineNodeData line) => renderedLines.Add(line);

            Assert.That(DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(graph, "Default"),
                    new DialogueExecutionContext(speaker, interactor)),
                Is.True);

            Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(firstLine));
            int firstPromptId = DialogueManager.Instance.CurrentPromptId;

            // UI가 늦게 연결되어도 현재 대사를 읽은 뒤 다음 변경부터 이벤트로 받을 수 있습니다.
            renderedLines.Add(DialogueManager.Instance.CurrentLine);
            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                Assert.That(DialogueManager.Instance.ContinueDialogue(firstPromptId), Is.True);
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(secondLine));
                int secondPromptId = DialogueManager.Instance.CurrentPromptId;
                Assert.That(secondPromptId, Is.Not.EqualTo(firstPromptId));

                Assert.That(DialogueManager.Instance.ContinueDialogue(firstPromptId), Is.False);
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(secondLine));
                Assert.That(DialogueManager.Instance.CurrentPromptId, Is.EqualTo(secondPromptId));
                Assert.That(renderedLines, Is.EqualTo(new[] { firstLine, secondLine }));

                Assert.That(DialogueManager.Instance.ContinueDialogue(secondPromptId), Is.True);
                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.CurrentLine, Is.Null);
                Assert.That(DialogueManager.Instance.CurrentPromptId, Is.Zero);
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
            }
        }

        [Test]
        public void DialogueManager_UnlinkedLineFaultsInsteadOfCompleting()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Line" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            Assert.That(DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(graph, "Default"),
                    new DialogueExecutionContext(speaker, interactor)),
                Is.True);

            LogAssert.Expect(LogType.Error, "[Dialogue] 노드 'line'의 출력 포트 'Next'에 연결선이 없습니다.");
            Assert.That(
                DialogueManager.Instance.ContinueDialogue(DialogueManager.Instance.CurrentPromptId),
                Is.True);
            Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
            Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Faulted));
        }

        [Test]
        public void DialogueManager_RejectsStaleChoiceWithMatchingPortName()
        {
            dialogueChoiceActionCount = 0;
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var firstChoice = new DialogueChoiceData { PortName = "accept", ChoiceText = "First" };
            var firstChoiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice-1",
                Choices = new List<DialogueChoiceData> { firstChoice }
            };
            var secondChoice = new DialogueChoiceData
            {
                PortName = "accept",
                ChoiceText = "Second",
                SelectionAction = new MethodBindingData { Key = "tests.dialogue.choice-action" }
            };
            var secondChoiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice-2",
                Choices = new List<DialogueChoiceData> { secondChoice }
            };
            var resultLine = new DialogueLineNodeData { Guid = "result", DialogueText = "Result" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(firstChoiceNode);
            graph.Nodes.Add(secondChoiceNode);
            graph.Nodes.Add(resultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice-1"));
            graph.NodeLinks.Add(Link("choice-1", "accept", "choice-2"));
            graph.NodeLinks.Add(Link("choice-2", "accept", "result"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            DialogueMethodInvoker.Initialize();
            Assert.That(DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(graph, "Default"),
                    new DialogueExecutionContext(speaker, interactor)),
                Is.True);

            int firstPromptId = DialogueManager.Instance.CurrentPromptId;
            Assert.That(DialogueManager.Instance.SelectChoice(firstPromptId, firstChoice), Is.True);
            int secondPromptId = DialogueManager.Instance.CurrentPromptId;
            Assert.That(secondPromptId, Is.Not.EqualTo(firstPromptId));

            Assert.That(DialogueManager.Instance.SelectChoice(firstPromptId, firstChoice), Is.False);
            Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.True);
            Assert.That(DialogueManager.Instance.CurrentPromptId, Is.EqualTo(secondPromptId));
            Assert.That(DialogueManager.Instance.CurrentChoices, Is.EqualTo(new[] { secondChoice }));
            Assert.That(dialogueChoiceActionCount, Is.Zero);

            Assert.That(DialogueManager.Instance.SelectChoice(secondPromptId, secondChoice), Is.True);
            Assert.That(dialogueChoiceActionCount, Is.EqualTo(1));
            Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(resultLine));
        }

        [Test]
        public void DialogueManager_SynchronousChoice_DoesNotReenterSelectionAction()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var firstChoice = new DialogueChoiceData { PortName = "next", ChoiceText = "First" };
            var secondChoice = new DialogueChoiceData { PortName = "next", ChoiceText = "Second" };
            var firstChoiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice-1",
                Choices = new List<DialogueChoiceData> { firstChoice }
            };
            var secondChoiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice-2",
                Choices = new List<DialogueChoiceData> { secondChoice }
            };
            var resultLine = new DialogueLineNodeData { Guid = "result", DialogueText = "Result" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(firstChoiceNode);
            graph.Nodes.Add(secondChoiceNode);
            graph.Nodes.Add(resultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice-1"));
            graph.NodeLinks.Add(Link("choice-1", "next", "choice-2"));
            graph.NodeLinks.Add(Link("choice-2", "next", "result"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            int callbackDepth = 0;
            int maxCallbackDepth = 0;
            int shownCount = 0;
            bool everyInputAccepted = true;

            void SelectImmediately(IReadOnlyList<DialogueChoiceData> choices)
            {
                callbackDepth++;
                try
                {
                    maxCallbackDepth = Math.Max(maxCallbackDepth, callbackDepth);
                    shownCount++;
                    int promptId = DialogueManager.Instance.CurrentPromptId;
                    everyInputAccepted &= DialogueManager.Instance.SelectChoice(promptId, choices[0]);
                }
                finally
                {
                    callbackDepth--;
                }
            }

            DialogueManager.Instance.ShowChoices += SelectImmediately;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor)),
                    Is.True);

                Assert.That(everyInputAccepted, Is.True);
                Assert.That(shownCount, Is.EqualTo(2));
                Assert.That(maxCallbackDepth, Is.EqualTo(1));
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(resultLine));
            }
            finally
            {
                DialogueManager.Instance.ShowChoices -= SelectImmediately;
            }
        }

        [Test]
        public void DialogueManager_CompletesOnceAndInvokesCallbackOnce()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var end = new DialogueEndNodeData { Guid = "end" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(end);
            graph.NodeLinks.Add(Link("entry", "Next", "end"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            int finishedCount = 0;
            int callbackCount = 0;
            DialogueEndReason? receivedReason = null;
            void CaptureFinished(DialogueEndReason reason)
            {
                finishedCount++;
                receivedReason = reason;
            }

            DialogueManager.Instance.ConversationEnd += CaptureFinished;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor),
                        () => callbackCount++),
                    Is.True);

                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
                Assert.That(receivedReason, Is.EqualTo(DialogueEndReason.Completed));
                Assert.That(finishedCount, Is.EqualTo(1));
                Assert.That(callbackCount, Is.EqualTo(1));

                DialogueManager.Instance.EndConversation();
                DialogueManager.Instance.CancelConversation();

                Assert.That(finishedCount, Is.EqualTo(1));
                Assert.That(callbackCount, Is.EqualTo(1));
            }
            finally
            {
                DialogueManager.Instance.ConversationEnd -= CaptureFinished;
            }
        }

        [Test]
        public void DialogueManager_CancelsOnceWithoutCompletionCallback()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Line" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            int finishedCount = 0;
            int callbackCount = 0;
            DialogueEndReason? receivedReason = null;
            void CaptureFinished(DialogueEndReason reason)
            {
                finishedCount++;
                receivedReason = reason;
            }

            DialogueManager.Instance.ConversationEnd += CaptureFinished;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor),
                        () => callbackCount++),
                    Is.True);

                DialogueManager.Instance.CancelConversation();
                DialogueManager.Instance.CancelConversation();
                DialogueManager.Instance.EndConversation();

                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Cancelled));
                Assert.That(receivedReason, Is.EqualTo(DialogueEndReason.Cancelled));
                Assert.That(finishedCount, Is.EqualTo(1));
                Assert.That(callbackCount, Is.Zero);
                Assert.That(DialogueManager.Instance.CurrentPromptId, Is.Zero);
                Assert.That(DialogueManager.Instance.CurrentLine, Is.Null);
                Assert.That(DialogueManager.Instance.CurrentChoices, Is.Empty);
            }
            finally
            {
                DialogueManager.Instance.ConversationEnd -= CaptureFinished;
            }
        }

        [Test]
        public void DialogueManager_DefersCompletionCallbackUntilStartEventReturns()
        {
            DialogueContainer firstGraph = CreateAsset<DialogueContainer>();
            var firstEntry = new DialogueEntryNodeData { Guid = "first-entry", EntryId = "Default" };
            var firstLine = new DialogueLineNodeData { Guid = "first-line", DialogueText = "First" };
            firstGraph.Nodes.Add(firstEntry);
            firstGraph.Nodes.Add(firstLine);
            firstGraph.NodeLinks.Add(Link("first-entry", "Next", "first-line"));

            DialogueContainer secondGraph = CreateAsset<DialogueContainer>();
            var secondEntry = new DialogueEntryNodeData { Guid = "second-entry", EntryId = "Default" };
            var secondLine = new DialogueLineNodeData { Guid = "second-line", DialogueText = "Second" };
            secondGraph.Nodes.Add(secondEntry);
            secondGraph.Nodes.Add(secondLine);
            secondGraph.NodeLinks.Add(Link("second-entry", "Next", "second-line"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            var executionContext = new DialogueExecutionContext(speaker, interactor);
            var order = new List<string>();
            bool endFirstConversation = true;
            bool secondStarted = false;
            int callbackCount = 0;

            void EndDuringStart()
            {
                if (!endFirstConversation)
                {
                    return;
                }

                endFirstConversation = false;
                order.Add("start-enter");
                DialogueManager.Instance.EndConversation();
                order.Add("start-exit");
            }

            void CaptureFinished(DialogueEndReason _) => order.Add("finished");

            DialogueManager.Instance.ConversationStart += EndDuringStart;
            DialogueManager.Instance.ConversationEnd += CaptureFinished;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(firstGraph, "Default"),
                        executionContext,
                        () =>
                        {
                            callbackCount++;
                            order.Add("completion");
                            secondStarted = DialogueManager.Instance.StartConversation(
                                new DialogueEntryPoint(secondGraph, "Default"),
                                executionContext);
                        }),
                    Is.True);

                Assert.That(order, Is.EqualTo(new[] { "start-enter", "finished", "start-exit", "completion" }));
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(secondStarted, Is.True);
                Assert.That(DialogueManager.Instance.IsConversationActive, Is.True);
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(secondLine));
            }
            finally
            {
                DialogueManager.Instance.ConversationStart -= EndDuringStart;
                DialogueManager.Instance.ConversationEnd -= CaptureFinished;
            }
        }

        [Test]
        public void DialogueManager_RecoversAfterLineSubscriberThrows()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Line" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            var executionContext = new DialogueExecutionContext(speaker, interactor);
            int finishedCount = 0;
            int callbackCount = 0;
            DialogueEndReason? receivedReason = null;

            void ThrowFromLine(DialogueLineNodeData _)
            {
                throw new InvalidOperationException("test line subscriber");
            }

            void CaptureFinished(DialogueEndReason reason)
            {
                finishedCount++;
                receivedReason = reason;
            }

            DialogueManager.Instance.ShowLine += ThrowFromLine;
            DialogueManager.Instance.ConversationEnd += CaptureFinished;
            try
            {
                LogAssert.Expect(
                    LogType.Error,
                    new System.Text.RegularExpressions.Regex(
                        "ShowLine 콜백 실행 중 예외가 발생했습니다.[\\s\\S]*InvalidOperationException: test line subscriber"));

                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        executionContext,
                        () => callbackCount++),
                    Is.True);

                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Faulted));
                Assert.That(receivedReason, Is.EqualTo(DialogueEndReason.Faulted));
                Assert.That(finishedCount, Is.EqualTo(1));
                Assert.That(callbackCount, Is.Zero);
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= ThrowFromLine;
                DialogueManager.Instance.ConversationEnd -= CaptureFinished;
            }

            Assert.That(DialogueManager.Instance.StartConversation(
                new DialogueEntryPoint(graph, "Default"),
                executionContext), Is.True);
            Assert.That(DialogueManager.Instance.IsConversationActive, Is.True);
            Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(line));
        }

        [Test]
        public void DialogueManager_FinishedSubscriberExceptionDoesNotStopRemainingHandlers()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var end = new DialogueEndNodeData { Guid = "end" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(end);
            graph.NodeLinks.Add(Link("entry", "Next", "end"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            int remainingHandlerCount = 0;
            int callbackCount = 0;

            void ThrowFromFinished(DialogueEndReason _)
            {
                throw new InvalidOperationException("test finished subscriber");
            }

            void CountFinished(DialogueEndReason _) => remainingHandlerCount++;

            DialogueManager.Instance.ConversationEnd += ThrowFromFinished;
            DialogueManager.Instance.ConversationEnd += CountFinished;
            try
            {
                LogAssert.Expect(
                    LogType.Error,
                    new System.Text.RegularExpressions.Regex(
                        "ConversationEnd 콜백 실행 중 예외가 발생했습니다.[\\s\\S]*InvalidOperationException: test finished subscriber"));

                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor),
                        () => callbackCount++),
                    Is.True);

                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
                Assert.That(remainingHandlerCount, Is.EqualTo(1));
                Assert.That(callbackCount, Is.EqualTo(1));
            }
            finally
            {
                DialogueManager.Instance.ConversationEnd -= ThrowFromFinished;
                DialogueManager.Instance.ConversationEnd -= CountFinished;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DialogueManager_WaitUsesSelectedTimeSource(bool useUnscaledTime)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var wait = new DialogueWaitNodeData
            {
                Guid = "wait",
                DurationSeconds = 1f,
                UseUnscaledTime = useUnscaledTime
            };
            var end = new DialogueEndNodeData { Guid = "end" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(wait);
            graph.Nodes.Add(end);
            graph.NodeLinks.Add(Link("entry", "Next", "wait"));
            graph.NodeLinks.Add(Link("wait", "Next", "end"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            int callbackCount = 0;
            Assert.That(DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(graph, "Default"),
                    new DialogueExecutionContext(speaker, interactor),
                    () => callbackCount++),
                Is.True);

            float firstScaledDelta = useUnscaledTime ? 10f : 0.4f;
            float firstUnscaledDelta = useUnscaledTime ? 0.4f : 10f;
            TickDialogueManager(firstScaledDelta, firstUnscaledDelta);

            Assert.That(DialogueManager.Instance.IsConversationActive, Is.True);
            Assert.That(callbackCount, Is.Zero);

            float secondScaledDelta = useUnscaledTime ? 10f : 0.7f;
            float secondUnscaledDelta = useUnscaledTime ? 0.7f : 10f;
            TickDialogueManager(secondScaledDelta, secondUnscaledDelta);
            TickDialogueManager(10f, 10f);
            DialogueManager.Instance.EndConversation();

            Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
            Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
            Assert.That(callbackCount, Is.EqualTo(1));
        }

        [Test]
        public void DialogueManager_ZeroWaitAdvancesImmediately()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var wait = new DialogueWaitNodeData
            {
                Guid = "wait",
                DurationSeconds = 0f
            };
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Line" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(wait);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "wait"));
            graph.NodeLinks.Add(Link("wait", "Next", "line"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            Assert.That(DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(graph, "Default"),
                    new DialogueExecutionContext(speaker, interactor)),
                Is.True);

            Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(line));
            Assert.That(DialogueManager.Instance.CurrentPromptId, Is.Not.Zero);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void DialogueManager_InvalidWaitDurationFaults(float duration)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var wait = new DialogueWaitNodeData
            {
                Guid = "wait",
                DurationSeconds = duration
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(wait);
            graph.NodeLinks.Add(Link("entry", "Next", "wait"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            int callbackCount = 0;
            DialogueEndReason? receivedReason = null;
            void CaptureFinished(DialogueEndReason reason) => receivedReason = reason;

            DialogueManager.Instance.ConversationEnd += CaptureFinished;
            try
            {
                LogAssert.Expect(
                    LogType.Error,
                    $"[Dialogue] Wait 노드 'wait'의 대기 시간 '{duration}'이(가) 올바르지 않습니다.");

                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor),
                        () => callbackCount++),
                    Is.True);

                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Faulted));
                Assert.That(receivedReason, Is.EqualTo(DialogueEndReason.Faulted));
                Assert.That(callbackCount, Is.Zero);
            }
            finally
            {
                DialogueManager.Instance.ConversationEnd -= CaptureFinished;
            }
        }

        [Test]
        public void DialogueManager_SignalMatchesOnceAndIgnoresWhenNotWaiting()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var waitSignal = new DialogueWaitSignalNodeData
            {
                Guid = "wait-signal",
                SignalKey = "Ready"
            };
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Line" };
            var end = new DialogueEndNodeData { Guid = "end" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(waitSignal);
            graph.Nodes.Add(line);
            graph.Nodes.Add(end);
            graph.NodeLinks.Add(Link("entry", "Next", "wait-signal"));
            graph.NodeLinks.Add(Link("wait-signal", "Next", "line"));
            graph.NodeLinks.Add(Link("line", "Next", "end"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            var executionContext = new DialogueExecutionContext(speaker, interactor);
            int shownCount = 0;
            int callbackCount = 0;
            void CaptureLine(DialogueLineNodeData _) => shownCount++;

            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        executionContext,
                        () => callbackCount++),
                    Is.True);
                Assert.That(DialogueManager.Instance.SendSignal("   "), Is.False);
                Assert.That(DialogueManager.Instance.SendSignal("ready"), Is.False);

                Assert.That(shownCount, Is.Zero);

                Assert.That(DialogueManager.Instance.SendSignal("  Ready  "), Is.True);
                Assert.That(DialogueManager.Instance.SendSignal("Ready"), Is.False);

                Assert.That(shownCount, Is.EqualTo(1));
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(line));

                Assert.That(
                    DialogueManager.Instance.ContinueDialogue(DialogueManager.Instance.CurrentPromptId),
                    Is.True);
                Assert.That(callbackCount, Is.EqualTo(1));

                Assert.That(DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(graph, "Default"),
                    executionContext), Is.True);

                DialogueManager.Instance.CancelConversation();
                Assert.That(DialogueManager.Instance.SendSignal("Ready"), Is.False);

                Assert.That(shownCount, Is.EqualTo(1));
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
            }
        }

        [Test]
        public void DialogueManager_ReturnsFalseWhenAlreadyBusy()
        {
            DialogueContainer activeGraph = CreateAsset<DialogueContainer>();
            var activeEntry = new DialogueEntryNodeData { Guid = "active-entry", EntryId = "Default" };
            var activeLine = new DialogueLineNodeData { Guid = "active-line", DialogueText = "Active" };
            activeGraph.Nodes.Add(activeEntry);
            activeGraph.Nodes.Add(activeLine);
            activeGraph.NodeLinks.Add(Link("active-entry", "Next", "active-line"));

            DialogueContainer requestedGraph = CreateAsset<DialogueContainer>();
            var requestedEntry = new DialogueEntryNodeData { Guid = "requested-entry", EntryId = "Default" };
            var requestedLine = new DialogueLineNodeData { Guid = "requested-line", DialogueText = "Requested" };
            requestedGraph.Nodes.Add(requestedEntry);
            requestedGraph.Nodes.Add(requestedLine);
            requestedGraph.NodeLinks.Add(Link("requested-entry", "Next", "requested-line"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            var executionContext = new DialogueExecutionContext(speaker, interactor);
            int callbackCount = 0;

            Assert.That(DialogueManager.Instance.StartConversation(
                new DialogueEntryPoint(activeGraph, "Default"),
                executionContext), Is.True);
            bool started = DialogueManager.Instance.StartConversation(
                new DialogueEntryPoint(requestedGraph, "Default"),
                executionContext,
                onComplete: () => callbackCount++);

            Assert.That(started, Is.False);
            Assert.That(callbackCount, Is.Zero);
            Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(activeLine));
        }

        [QuestAction("tests.quest.change-run", Target = QuestMethodTarget.Global)]
        internal static void ChangeQuestRun(QuestExecutionContext context)
        {
            questRunAction?.Invoke(context);
        }

        [QuestCondition("tests.quest.run-condition", Target = QuestMethodTarget.Global)]
        internal static bool EvaluateQuestRunCondition(QuestExecutionContext context)
        {
            return questRunCondition?.Invoke(context) ?? true;
        }

        [QuestCondition("tests.quest.is-ready", Target = QuestMethodTarget.Global)]
        internal static bool IsAttributedQuestReady(
            QuestExecutionContext context,
            int required)
        {
            return context.Progress?.state == QuestState.InProgress && required == 42;
        }

        [QuestAction("tests.quest.record-action", Target = QuestMethodTarget.Global)]
        internal static void RecordAttributedQuestAction(
            QuestExecutionContext context,
            int amount,
            bool flag)
        {
            attributedQuestActionAmount = context.Progress == null ? -1 : amount;
            attributedQuestActionFlag = flag;
        }

        [DialogueCondition("tests.dialogue.choice-visible", Owner = DialogueMethodOwner.Global)]
        [QuestCondition("tests.quest.choice-visible", Target = QuestMethodTarget.Global)]
        private static bool IsDialogueChoiceVisible(bool visible)
        {
            return visible;
        }

        [DialogueAction("tests.dialogue.choice-action", Owner = DialogueMethodOwner.Global)]
        private static void RecordDialogueChoiceAction()
        {
            dialogueChoiceActionCount++;
        }

        [DialogueAction("tests.dialogue.end-current", Owner = DialogueMethodOwner.Global)]
        private static void EndCurrentDialogue()
        {
            DialogueManager.Instance.EndConversation();
        }

        [DialogueAction("tests.invoker.action", Owner = DialogueMethodOwner.Global)]
        [QuestAction("tests.invoker.action", Target = QuestMethodTarget.Global)]
        private static void RecordInvokerAction()
        {
            dialogueChoiceActionCount++;
        }

        private static void RecordOverloadedAction(int amount)
        {
            overloadedActionAmount = amount;
        }

        private static void RecordOverloadedAction<T>(int amount)
        {
            throw new InvalidOperationException("제네릭 오버로드가 선택되면 안 됩니다.");
        }

        private static void AcceptAllSupportedArgumentTypes(
            string text,
            bool flag,
            int count,
            float ratio,
            QuestState state,
            DialogueContainer asset)
        {
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void TickDialogueManager(float scaledDeltaTime, float unscaledDeltaTime)
        {
            MethodInfo tickMethod = typeof(DialogueManager).GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tickMethod, Is.Not.Null);
            tickMethod.Invoke(DialogueManager.Instance, new object[] { scaledDeltaTime, unscaledDeltaTime });
        }

        private static List<MethodArgumentData> CreateQuestArguments(
            string methodName,
            MethodKind kind,
            params (string Id, object Value)[] values)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(QuestMethodDescriptorFactory.TryCreateFromReflection(
                    method,
                    kind,
                    kind == MethodKind.Action
                        ? "tests.quest.record-action"
                        : "tests.quest.is-ready",
                    QuestMethodTarget.Global,
                    out QuestMethodDescriptor descriptor,
                    out string error),
                Is.True,
                error);
            List<MethodArgumentData> arguments = MethodArgumentCodec.CreateDefaultArgumentData(descriptor);
            WriteArguments(arguments, descriptor.SerializedParameters, values);
            return arguments;
        }

        private static List<MethodArgumentData> CreateDialogueArguments(
            string methodName,
            MethodKind kind,
            params (string Id, object Value)[] values)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(DialogueMethodDescriptorFactory.TryCreateFromReflection(
                    method,
                    kind,
                    "tests.dialogue.choice-visible",
                    DialogueMethodOwner.Global,
                    out DialogueMethodDescriptor descriptor,
                    out string error),
                Is.True,
                error);
            List<MethodArgumentData> arguments = MethodArgumentCodec.CreateDefaultArgumentData(descriptor);
            WriteArguments(arguments, descriptor.SerializedParameters, values);
            return arguments;
        }

        private static void WriteArguments(
            IList<MethodArgumentData> arguments,
            IReadOnlyList<MethodParameterDescriptor> descriptors,
            IEnumerable<(string Id, object Value)> values)
        {
            foreach ((string id, object value) in values)
            {
                MethodParameterDescriptor descriptor = descriptors.Single(parameterDescriptor => parameterDescriptor.ParameterId == id);
                MethodArgumentData argument = arguments.Single(candidate => candidate.ParameterId == id);
                Assert.That(MethodArgumentCodec.TryEncodeArgumentData(
                        argument,
                        descriptor,
                        value,
                        out string error),
                    Is.True,
                    error);
            }
        }

        private static NodeLinkData Link(string source, string port, string target)
        {
            return new NodeLinkData
            {
                StartNodeGuid = source,
                StartPortName = port,
                TargetNodeGuid = target,
                TargetPortName = "Input"
            };
        }

        private sealed class FakeQuestController : IQuestController
        {
            public IDictionary<int, QuestProgress> QuestProgress { get; } = new Dictionary<int, QuestProgress>();
            public List<int> StatusChangedQuestIds { get; } = new();

            public void InvokeStatusChanged(QuestContainer container, QuestProgress progress)
            {
                StatusChangedQuestIds.Add(progress.questId);
            }
        }
    }
}
