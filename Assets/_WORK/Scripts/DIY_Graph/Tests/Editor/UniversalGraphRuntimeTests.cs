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
        private static Action<string> dialogueRunAction;
        private static Action<QuestExecutionContext> questRunAction;
        private static Func<QuestExecutionContext, bool> questRunCondition;
        private static int overloadedActionAmount;
        private static object[] invokedArgumentValues;
        private static DialogueExecutionContext invokedDialogueContext;

        [OneTimeSetUp]
        public void RegisterTestMethods()
        {
            // Editor 전용 테스트 메서드는 운영 초기화에서 제외되므로 테스트에서만 등록합니다.
            foreach (Type invoker in new[] { typeof(DialogueMethodInvoker), typeof(QuestMethodInvoker) })
            {
                invoker.GetMethod("ResetStaticState", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                invoker.GetMethod("Initialize").Invoke(null, null);
                object[] arguments = { typeof(UniversalGraphRuntimeTests).Assembly };
                invoker.GetMethod("ScanAssembly", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);
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
            dialogueRunAction = null;
            questRunAction = null;
            questRunCondition = null;
            invokedArgumentValues = null;
            invokedDialogueContext = null;
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
        [TestCase(typeof(QuestMethodInvoker), "actionRegistry")]
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
            MethodInfo invoke = invoker.GetMethod("InvokeMethod");
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

        [TestCase(typeof(DialogueMethodInvoker))]
        [TestCase(typeof(QuestMethodInvoker))]
        public void MethodInvokers_InvokeNormalizedBindingWithoutChangingArguments(Type invoker)
        {
            var binding = JsonUtility.FromJson<MethodBindingData>("{\"key\":\"  tests.invoker.all-types  \"}");
            DialogueContainer asset = CreateAsset<DialogueContainer>();
            binding.Arguments = CreateDialogueArguments(
                nameof(AcceptAllSupportedArgumentTypes), MethodKind.Action,
                ("arg0", "text"), ("arg1", true), ("arg2", 42),
                ("arg3", 1.25f), ("arg4", QuestState.InProgress), ("arg5", asset));
            List<MethodArgumentData> savedArguments = binding.Arguments;
            string before = JsonUtility.ToJson(binding);
            object[] arguments = { binding, null, MethodKind.Action, false };

            Assert.That(binding.Key, Is.EqualTo("tests.invoker.all-types"));
            Assert.That((bool)invoker.GetMethod("InvokeMethod").Invoke(null, arguments), Is.True);
            Assert.That(invokedArgumentValues,
                Is.EqualTo(new object[] { "text", true, 42, 1.25f, QuestState.InProgress, asset }));
            Assert.That(binding.Arguments, Is.SameAs(savedArguments));
            Assert.That(JsonUtility.ToJson(binding), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MethodDescriptorFactories_AcceptNonGenericOverloadAndRejectGenericDeclaration(bool quest)
        {
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            MethodInfo create = factory.GetMethod("CreateDescriptor");
            foreach (MethodInfo method in typeof(UniversalGraphRuntimeTests)
                         .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                         .Where(method => method.Name == nameof(RecordOverloadedAction)))
            {
                object[] arguments = { method, MethodKind.Action, "tests.overload", owner, null, null };
                bool created = (bool)create.Invoke(null, arguments);
                Assert.That(created, Is.EqualTo(!method.IsGenericMethod), arguments[5] as string);
                if (!created)
                {
                    Assert.That(arguments[5], Is.Not.Null.And.Not.Empty);
                    continue;
                }

                var descriptor = (MethodDescriptor)arguments[4];
                Assert.That(descriptor.MethodInfo, Is.SameAs(method));
                overloadedActionAmount = 0;
                descriptor.MethodInfo.Invoke(null, new object[] { 23 });
                Assert.That(overloadedActionAmount, Is.EqualTo(23));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MethodDescriptorFactories_RejectConstructedGenericMethod(bool quest)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == nameof(RecordOverloadedAction) && candidate.IsGenericMethodDefinition)
                .MakeGenericMethod(typeof(int));

            Assert.That(method.ContainsGenericParameters, Is.False);
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] arguments = { method, MethodKind.Action, "tests.closed-generic", owner, null, null };

            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.False);
            Assert.That(arguments[4], Is.Null);
            Assert.That(arguments[5], Is.Not.Null.And.Not.Empty);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void MethodDescriptorFactories_RequireClosedDeclaringType(bool quest, bool closedType)
        {
            Type declaringType = closedType ? typeof(FactoryGenericOwner<int>) : typeof(FactoryGenericOwner<>);
            MethodInfo method = declaringType.GetMethod("Execute");
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] arguments = { method, MethodKind.Action, "tests.generic-owner", owner, null, null };

            Assert.That(method.IsGenericMethod, Is.False);
            Assert.That(method.ContainsGenericParameters, Is.EqualTo(!closedType));
            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.EqualTo(closedType));
            if (closedType)
            {
                Assert.That(((MethodDescriptor)arguments[4]).DeclaringType, Is.EqualTo(declaringType));
                Assert.That(arguments[5], Is.Null);
            }
            else
            {
                Assert.That(arguments[4], Is.Null);
                Assert.That(arguments[5], Is.Not.Null.And.Not.Empty);
            }
        }

        [TestCase(false, nameof(FactoryAsyncAction))]
        [TestCase(true, nameof(FactoryAsyncAction))]
        [TestCase(false, nameof(FactoryOptionalAction))]
        [TestCase(true, nameof(FactoryOptionalAction))]
        [TestCase(false, nameof(FactoryParamsAction))]
        [TestCase(true, nameof(FactoryParamsAction))]
        public void MethodDescriptorFactories_RejectUnsupportedCallingForms(bool quest, string methodName)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] arguments = { method, MethodKind.Action, "tests.unsupported-call", owner, null, null };

            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.False);
            Assert.That(arguments[4], Is.Null);
            Assert.That(arguments[5], Is.Not.Null.And.Not.Empty);
        }

        [TestCase(false, nameof(AcceptLongFlagsArgument))]
        [TestCase(true, nameof(AcceptLongFlagsArgument))]
        [TestCase(false, nameof(AcceptULongFlagsArgument))]
        [TestCase(true, nameof(AcceptULongFlagsArgument))]
        public void MethodDescriptorFactories_Reject64BitFlagsArguments(bool quest, string methodName)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Type parameterType = method.GetParameters()[0].ParameterType;
            Assert.That(MethodArgumentCodec.GetArgumentKind(parameterType, out _), Is.False);

            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] arguments = { method, MethodKind.Action, "tests.unsupported-flags", owner, null, null };

            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.False);
            Assert.That(arguments[4], Is.Null);
            Assert.That(arguments[5], Is.Not.Null.And.Not.Empty);
        }

        [TestCase(false, nameof(AcceptIntFlagsArgument), FactoryIntFlags.Low | FactoryIntFlags.High)]
        [TestCase(true, nameof(AcceptIntFlagsArgument), FactoryIntFlags.Low | FactoryIntFlags.High)]
        [TestCase(false, nameof(AcceptOrdinaryEnumArgument), QuestState.InProgress)]
        [TestCase(true, nameof(AcceptOrdinaryEnumArgument), QuestState.InProgress)]
        [TestCase(false, nameof(AcceptLongEnumArgument), FactoryLongEnum.High)]
        [TestCase(true, nameof(AcceptLongEnumArgument), FactoryLongEnum.High)]
        [TestCase(false, nameof(AcceptULongEnumArgument), FactoryULongEnum.High)]
        [TestCase(true, nameof(AcceptULongEnumArgument), FactoryULongEnum.High)]
        public void MethodDescriptorFactories_Accept32BitFlagsAndOrdinaryEnums(bool quest, string methodName, object expected)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Type parameterType = method.GetParameters()[0].ParameterType;
            Assert.That(MethodArgumentCodec.GetArgumentKind(parameterType, out MethodArgumentKind kind), Is.True);
            Assert.That(kind, Is.EqualTo(MethodArgumentKind.Enum));

            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] arguments = { method, MethodKind.Action, "tests.supported-enum", owner, null, null };

            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.True, arguments[5] as string);
            var descriptor = (MethodDescriptor)arguments[4];
            Assert.That(descriptor.SerializedParameters, Has.Count.EqualTo(1));
            List<MethodArgumentData> argumentData = MethodArgumentCodec.CreateDefaultArgumentData(descriptor);
            Assert.That(MethodArgumentCodec.TryEncodeArgumentData(
                argumentData[0], descriptor.SerializedParameters[0], expected, out string error), Is.True, error);
            Assert.That(MethodArgumentCodec.TryDecodeAllArgumentData(
                argumentData, descriptor, out object[] restored, out error), Is.True, error);
            Assert.That(restored[0], Is.EqualTo(expected));
        }

        [TestCase(false, MethodKind.Action)]
        [TestCase(false, MethodKind.Condition)]
        [TestCase(true, MethodKind.Action)]
        [TestCase(true, MethodKind.Condition)]
        public void MethodBindings_TreatNoneAsAnOrdinaryRegisteredKey(bool quest, MethodKind kind)
        {
            string methodName = kind == MethodKind.Action ? nameof(RecordNoneKeyAction) : nameof(EvaluateNoneKeyCondition);
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] factoryArguments = { method, kind, "None", owner, null, null };

            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, factoryArguments), Is.True);
            Assert.That(((MethodDescriptor)factoryArguments[4]).Key, Is.EqualTo("None"));
            Assert.That(factoryArguments[5], Is.Null);

            var binding = new MethodBindingData { Key = "  None  " };
            Type invoker = quest ? typeof(QuestMethodInvoker) : typeof(DialogueMethodInvoker);
            object[] arguments = { binding, null, kind, false };
            int previousCount = dialogueChoiceActionCount;

            Assert.That(binding.HasKey, Is.True);
            Assert.That((bool)invoker.GetMethod("InvokeMethod").Invoke(null, arguments), Is.True);
            Assert.That((bool)arguments[3], Is.EqualTo(kind == MethodKind.Condition));
            Assert.That(dialogueChoiceActionCount, Is.EqualTo(previousCount + (kind == MethodKind.Action ? 1 : 0)));
        }

        [TestCase(false, null)]
        [TestCase(false, "")]
        [TestCase(false, " \t\r\n ")]
        [TestCase(true, null)]
        [TestCase(true, "")]
        [TestCase(true, " \t\r\n ")]
        public void MethodDescriptorFactories_StillRejectEmptyKeys(bool quest, string key)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(nameof(RecordInvokerAction), BindingFlags.Static | BindingFlags.NonPublic);
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] arguments = { method, MethodKind.Action, key, owner, null, null };

            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.False);
            Assert.That(arguments[4], Is.Null);
            Assert.That(arguments[5], Is.Not.Null.And.Not.Empty);
        }

        [TestCase(false, MethodKind.Action, nameof(IsDialogueChoiceVisible))]
        [TestCase(true, MethodKind.Action, nameof(IsDialogueChoiceVisible))]
        [TestCase(false, MethodKind.Condition, nameof(RecordInvokerAction))]
        [TestCase(true, MethodKind.Condition, nameof(RecordInvokerAction))]
        public void MethodDescriptorFactories_RejectReturnTypeThatDoesNotMatchKind(bool quest, MethodKind kind, string methodName)
        {
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = { method, kind, "tests.invalid-return", owner, null, null };

            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.False);
            Assert.That(arguments[4], Is.Null);
            Assert.That(arguments[5], Is.Not.Null.And.Not.Empty);
        }

        [TestCase(typeof(DialogueMethodInvoker))]
        [TestCase(typeof(QuestMethodInvoker))]
        public void MethodInvokers_ReflectPrivateStaticMethodWithAllSupportedValues(Type invoker)
        {
            DialogueContainer asset = CreateAsset<DialogueContainer>();
            var binding = new MethodBindingData
            {
                Key = "tests.invoker.all-types",
                Arguments = CreateDialogueArguments(
                    nameof(AcceptAllSupportedArgumentTypes), MethodKind.Action,
                    ("arg0", "text"), ("arg1", true), ("arg2", 42),
                    ("arg3", 1.25f), ("arg4", QuestState.InProgress), ("arg5", asset))
            };

            object[] arguments = { binding, null, MethodKind.Action, false };
            Assert.That((bool)invoker.GetMethod("InvokeMethod").Invoke(null, arguments), Is.True);
            Assert.That(invokedArgumentValues,
                Is.EqualTo(new object[] { "text", true, 42, 1.25f, QuestState.InProgress, asset }));
        }

        [Test]
        public void DialogueMethodInvoker_InjectsContextWithoutSavedArgument()
        {
            var context = new DialogueExecutionContext(CreateGameObject("speaker"), CreateGameObject("interactor"));
            var binding = new MethodBindingData { Key = "tests.dialogue.context" };

            Assert.That(DialogueMethodInvoker.InvokeMethod(binding, context, MethodKind.Action, out _), Is.True);
            Assert.That(binding.Arguments, Is.Empty);
            Assert.That(invokedDialogueContext, Is.SameAs(context));
        }

        [TestCase(DialogueMethodOwner.Speaker)]
        [TestCase(DialogueMethodOwner.Interactor)]
        public void DialogueMethodInvoker_UsesRequestedComponentInstance(DialogueMethodOwner owner)
        {
            GameObject parent = CreateGameObject("parent");
            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            speaker.transform.SetParent(parent.transform);
            interactor.transform.SetParent(parent.transform);
            GameObject target = owner == DialogueMethodOwner.Speaker ? speaker : interactor;
            target.transform.SetSiblingIndex(1);

            MethodInfo method = typeof(Transform).GetMethod(nameof(Transform.SetSiblingIndex), new[] { typeof(int) });
            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
                method, MethodKind.Action, "tests.dialogue.instance", owner,
                out DialogueMethodDescriptor descriptor, out string error), Is.True, error);
            typeof(DialogueMethodInvoker).GetMethod("RegisterDescriptor", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { descriptor });
            var binding = new MethodBindingData
            {
                Key = descriptor.Key,
                Arguments = MethodArgumentCodec.CreateDefaultArgumentData(descriptor)
            };
            WriteArguments(binding.Arguments, descriptor.SerializedParameters, new[] { ("arg0", (object)0) });

            try
            {
                Assert.That(DialogueMethodInvoker.InvokeMethod(
                    binding, new DialogueExecutionContext(speaker, interactor), MethodKind.Action, out _), Is.True);
                Assert.That(target.transform.GetSiblingIndex(), Is.Zero);
            }
            finally
            {
                RegisterTestMethods();
            }
        }

        [Test]
        public void QuestMethodInvoker_StillRejectsNullControllerWithAnError()
        {
            MethodInfo method = typeof(FakeQuestController).GetMethod("RecordContext", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
                method, MethodKind.Action, "tests.null-controller", QuestMethodOwner.Controller,
                out QuestMethodDescriptor descriptor, out string error), Is.True, error);
            MethodInfo getMethodOwner = typeof(QuestMethodInvoker).GetMethod("GetMethodOwnerInstance", BindingFlags.Static | BindingFlags.NonPublic);

            LogAssert.Expect(LogType.Error,
                $"[Quest] Controller Action 'tests.null-controller'에는 '{typeof(FakeQuestController).FullName}' 타입이 필요하지만, 현재 Controller 타입은 'null'입니다.");
            Assert.That(getMethodOwner.Invoke(null, new object[] { descriptor, null }), Is.Null);
        }

        [Test]
        public void QuestMethodInvoker_UsesControllerInstanceAndInjectsContext()
        {
            var controller = new FakeQuestController();
            QuestContainer container = CreateAsset<QuestContainer>();
            var context = new QuestExecutionContext(controller, container, null, new QuestActionNodeData());
            MethodInfo method = typeof(FakeQuestController).GetMethod("RecordContext", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
                method, MethodKind.Action, "tests.quest.instance", QuestMethodOwner.Controller,
                out QuestMethodDescriptor descriptor, out string error), Is.True, error);
            var binding = new MethodBindingData
            {
                Key = descriptor.Key,
                Arguments = MethodArgumentCodec.CreateDefaultArgumentData(descriptor)
            };
            WriteArguments(binding.Arguments, descriptor.SerializedParameters, new[] { ("arg0", (object)17) });

            Assert.That(binding.Arguments, Has.Count.EqualTo(1));
            Assert.That(QuestMethodInvoker.InvokeMethod(binding, context, MethodKind.Action, out _), Is.True);
            Assert.That(controller.InvokedContext, Is.SameAs(context));
            Assert.That(controller.InvokedAmount, Is.EqualTo(17));
        }

        [TestCase(typeof(DialogueMethodInvoker))]
        [TestCase(typeof(QuestMethodInvoker))]
        public void MethodInvokers_ReportOriginalReflectionExceptionAndReturnFalse(Type invoker)
        {
            var binding = new MethodBindingData { Key = "tests.invoker.throw" };
            object[] arguments = { binding, null, MethodKind.Action, true };
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("tests\\.invoker\\.throw[\\s\\S]*InvalidOperationException: reflection invocation test"));

            Assert.That((bool)invoker.GetMethod("InvokeMethod").Invoke(null, arguments), Is.False);
            Assert.That(arguments[3], Is.False);
        }

        [TestCase(typeof(DialogueMethodInvoker), "actionRegistry")]
        [TestCase(typeof(QuestMethodInvoker), "actionRegistry")]
        public void MethodInvokers_RejectDuplicateKeyInsteadOfChoosingFirstMethod(Type invoker, string registryName)
        {
            const string key = "tests.invoker.duplicate";
            object owner = invoker == typeof(DialogueMethodInvoker)
                ? (object)DialogueMethodOwner.Global : QuestMethodOwner.Global;
            MethodInfo register = invoker.GetMethod("RegisterMethod", BindingFlags.Static | BindingFlags.NonPublic);
            string[] methodNames = { nameof(RecordInvokerAction), nameof(RecordDialogueChoiceAction), nameof(ThrowInvokerException) };
            for (int i = 0; i < methodNames.Length; i++)
            {
                if (i == 1)
                {
                    LogAssert.Expect(LogType.Error,
                        new System.Text.RegularExpressions.Regex("중복된 Action 키 'tests\\.invoker\\.duplicate'"));
                }

                MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(methodNames[i], BindingFlags.Static | BindingFlags.NonPublic);
                register.Invoke(null, new object[] { method, MethodKind.Action, key, owner });
                var registry = (System.Collections.IDictionary)invoker.GetField(registryName, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Assert.That(registry.Contains(key), Is.EqualTo(i == 0));
            }
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

        [TestCase(0)]
        [TestCase(GraphAssetMigrator.CurrentVersion + 1)]
        public void DialogueManager_RejectsUnsupportedSchemaWithoutStartingConversation(int version)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.name = "Unsupported Dialogue";
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Hello" };
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "line"));

            FieldInfo schemaField = typeof(GraphContainer).GetField("schemaVersion", BindingFlags.Instance | BindingFlags.NonPublic);
            schemaField.SetValue(graph, version);
            int startCount = 0;
            Action onStart = () => startCount++;
            DialogueManager.Instance.ConversationStart += onStart;
            try
            {
                LogAssert.Expect(LogType.Error,
                    $"[Dialogue] 대화를 시작하지 못했습니다. 그래프 'Unsupported Dialogue'의 스키마 버전 {version}은 지원하지 않습니다.");
                var entryPoint = new DialogueEntryPoint(graph, DialogueEntryNodeData.DefaultEntryId);
                Assert.That(DialogueManager.Instance.StartConversation(entryPoint), Is.False);
                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(graph.SchemaVersion, Is.EqualTo(version));
                Assert.That(startCount, Is.Zero);

                schemaField.SetValue(graph, GraphAssetMigrator.CurrentVersion);
                Assert.That(DialogueManager.Instance.StartConversation(entryPoint), Is.True);
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(line));
                Assert.That(startCount, Is.EqualTo(1));
            }
            finally
            {
                DialogueManager.Instance.ConversationStart -= onStart;
            }
        }

        [Test]
        public void MethodArgumentCodec_SupportsOnlyTheChosenGraphValueTypes()
        {
            Assert.That(MethodArgumentCodec.GetArgumentKind(typeof(int), out _), Is.True);
            Assert.That(MethodArgumentCodec.GetArgumentKind(typeof(float), out _), Is.True);
            Assert.That(MethodArgumentCodec.GetArgumentKind(typeof(ScriptableObject), out MethodArgumentKind unityKind), Is.True);
            Assert.That(unityKind, Is.EqualTo(MethodArgumentKind.UnityObject));
            Assert.That(MethodArgumentCodec.GetArgumentKind(typeof(long), out _), Is.False);
            Assert.That(MethodArgumentCodec.GetArgumentKind(typeof(double), out _), Is.False);
            Assert.That(MethodArgumentCodec.GetArgumentKind(typeof(object), out _), Is.False);
        }

        [Test]
        public void MethodArgumentCodec_EncodesAndDecodesAllSupportedValues()
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(AcceptAllSupportedArgumentTypes),
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
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

                Assert.That(MethodArgumentCodec.DecodeArgumentData(
                        argument,
                        parameterDescriptor,
                        out object decodedValue,
                        out string decodeError),
                    Is.True,
                    decodeError);
                Assert.That(decodedValue, Is.EqualTo(values[i]));
            }
        }

        [TestCase(false, 1.23456789f)]
        [TestCase(true, 1.23456789f)]
        [TestCase(false, -98765.4321f)]
        [TestCase(true, -98765.4321f)]
        [TestCase(false, float.Epsilon)]
        [TestCase(true, float.Epsilon)]
        [TestCase(false, float.MaxValue)]
        [TestCase(true, float.MaxValue)]
        public void MethodArgumentCodec_PreservesExactFloatRoundTrip(bool quest, float original)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(AcceptAllSupportedArgumentTypes), BindingFlags.Static | BindingFlags.NonPublic);
            Type factory = quest ? typeof(QuestMethodDescriptorFactory) : typeof(DialogueMethodDescriptorFactory);
            object owner = quest ? (object)QuestMethodOwner.Global : DialogueMethodOwner.Global;
            object[] arguments = { method, MethodKind.Action, "tests.float-round-trip", owner, null, null };
            Assert.That((bool)factory.GetMethod("CreateDescriptor").Invoke(null, arguments), Is.True, arguments[5] as string);

            var descriptor = (MethodDescriptor)arguments[4];
            MethodParameterDescriptor parameter = descriptor.SerializedParameters.Single(
                candidate => candidate.ParameterType == typeof(float));
            var argument = new MethodArgumentData();
            Assert.That(MethodArgumentCodec.TryEncodeArgumentData(
                argument, parameter, original, out string error), Is.True, error);
            Assert.That(MethodArgumentCodec.DecodeArgumentData(
                argument, parameter, out object restored, out error), Is.True, error);
            Assert.That(BitConverter.GetBytes((float)restored), Is.EqualTo(BitConverter.GetBytes(original)),
                "Stored text: " + argument.SerializedValue);
        }

        [Test]
        public void MethodArgumentCodec_DoesNotChangeDataWhenEncodingFails()
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsDialogueChoiceVisible),
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
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

        [TestCase("  tests.binding.action  ", "tests.binding.action")]
        [TestCase(" \t\r\n ", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void MethodBindingData_NormalizesAssignedKeyWithoutChangingArguments(string assignedKey, string expectedKey)
        {
            var argument = new MethodArgumentData
            {
                ParameterId = "arg0",
                TypeSignature = "unchanged-type",
                SerializedValue = "  unchanged value  ",
                ObjectValue = CreateAsset<DialogueContainer>()
            };
            var arguments = new List<MethodArgumentData> { argument };
            var binding = new MethodBindingData { Arguments = arguments };
            string before = JsonUtility.ToJson(argument);

            binding.Key = assignedKey;

            Assert.That(binding.Key, Is.EqualTo(expectedKey));
            Assert.That(binding.Arguments, Is.SameAs(arguments));
            Assert.That(binding.Arguments[0], Is.SameAs(argument));
            Assert.That(JsonUtility.ToJson(argument), Is.EqualTo(before));
        }

        [Test]
        public void MethodBindingData_NormalizesDeserializedKeyWithoutChangingArguments()
        {
            var binding = JsonUtility.FromJson<MethodBindingData>(
                "{\"key\":\"  tests.binding.deserialized  \",\"Arguments\":[{\"ParameterId\":\"arg0\",\"TypeSignature\":\"unchanged-type\",\"SerializedValue\":\"  unchanged value  \"}]}");

            Assert.That(binding.Key, Is.EqualTo("tests.binding.deserialized"));
            Assert.That(binding.Arguments, Has.Count.EqualTo(1));
            Assert.That(binding.Arguments[0].ParameterId, Is.EqualTo("arg0"));
            Assert.That(binding.Arguments[0].TypeSignature, Is.EqualTo("unchanged-type"));
            Assert.That(binding.Arguments[0].SerializedValue, Is.EqualTo("  unchanged value  "));
        }

        [Test]
        public void MethodBindingData_SerializesAssignedKeyInNormalizedForm()
        {
            var binding = new MethodBindingData { Key = "  tests.binding.serialized  " };

            string json = JsonUtility.ToJson(binding);

            Assert.That(json, Does.Contain("\"key\":\"tests.binding.serialized\""));
            Assert.That(JsonUtility.FromJson<MethodBindingData>(json).Key, Is.EqualTo(binding.Key));
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
        public void NewGraphData_CreatesUniqueStableIdentifiers()
        {
            NodeBaseData[] nodes = { new DialogueLineNodeData(), new QuestObjectiveNodeData() };
            DialogueChoiceData[] choices = { new(), new() };
            string[] ids = { nodes[0].Guid, nodes[1].Guid, choices[0].PortName, choices[1].PortName };

            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            foreach (string id in ids)
            {
                Assert.That(Guid.TryParse(id, out _), Is.True);
            }

            Assert.That(nodes[0].Guid, Is.EqualTo(ids[0]));
            Assert.That(choices[0].PortName, Is.EqualTo(ids[2]));
        }

        [Test]
        public void GraphIdentifiers_SurviveSerializationWithoutBeingRegenerated()
        {
            var node = new DialogueChoiceNodeData();
            node.Choices.Add(new DialogueChoiceData { ChoiceText = "Choice" });

            var restored = JsonUtility.FromJson<DialogueChoiceNodeData>(JsonUtility.ToJson(node));

            Assert.That(restored.Guid, Is.EqualTo(node.Guid));
            Assert.That(restored.Choices.Single().PortName, Is.EqualTo(node.Choices.Single().PortName));
        }

        [TestCase(null)]
        [TestCase("")]
        public void GraphIdentifiers_ExplicitInvalidSavedValuesAreNotReplaced(string id)
        {
            var node = new DialogueChoiceNodeData { Guid = id };
            node.Choices.Add(new DialogueChoiceData { PortName = id });

            var restored = JsonUtility.FromJson<DialogueChoiceNodeData>(JsonUtility.ToJson(node));

            Assert.That(string.IsNullOrEmpty(restored.Guid), Is.True);
            Assert.That(string.IsNullOrEmpty(restored.Choices.Single().PortName), Is.True);
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase(" \t\r\n ", false)]
        [TestCase("  missing.method  ", true)]
        public void MethodBinding_HasKeyOnlyChecksWhetherAKeyWasEntered(string key, bool expected)
        {
            var binding = new MethodBindingData { Key = key };
            var restored = JsonUtility.FromJson<MethodBindingData>(JsonUtility.ToJson(binding));

            Assert.That(binding.HasKey, Is.EqualTo(expected));
            Assert.That(restored.HasKey, Is.EqualTo(expected));
            Assert.That(restored.Arguments, Is.Empty);
        }

        [TestCase("  item.give  ", "item.give")]
        [TestCase("  item give  ", "item give")]
        [TestCase(" \t\r\n ", "")]
        [TestCase(null, "")]
        public void MethodAttributes_NormalizeKeysInTheirConstructors(string key, string expected)
        {
            Assert.That(new DialogueActionAttribute(key).Key, Is.EqualTo(expected));
            Assert.That(new DialogueConditionAttribute(key).Key, Is.EqualTo(expected));
            Assert.That(new QuestActionAttribute(key).Key, Is.EqualTo(expected));
            Assert.That(new QuestConditionAttribute(key).Key, Is.EqualTo(expected));
        }

        [TestCase("  quest.key  ", "quest.key")]
        [TestCase("  quest key  ", "quest key")]
        [TestCase(" \t\r\n ", "")]
        [TestCase(null, "")]
        public void QuestKeyData_NormalizesAssignmentsBeforeSaving(string key, string expected)
        {
            var objective = new QuestObjectiveNodeData
            {
                EventKey = key,
                TargetId = 7,
                ObjectiveDescription = "  Keep these spaces  "
            };
            var entry = new QuestInteractionEntryNodeData { TargetId = key };

            Assert.That(objective.EventKey, Is.EqualTo(expected));
            Assert.That(entry.TargetId, Is.EqualTo(expected));
            Assert.That(JsonUtility.ToJson(objective), Does.Contain($"\"eventKey\":\"{expected}\""));
            Assert.That(JsonUtility.ToJson(entry), Does.Contain($"\"targetId\":\"{expected}\""));
            Assert.That(objective.TargetId, Is.EqualTo(7));
            Assert.That(objective.ObjectiveDescription, Is.EqualTo("  Keep these spaces  "));
        }

        [Test]
        public void QuestKeyData_NormalizesDeserializedKeysWithoutChangingDescriptions()
        {
            var objective = JsonUtility.FromJson<QuestObjectiveNodeData>(
                "{\"eventKey\":\"  Collect  \",\"TargetId\":7,\"ObjectiveDescription\":\"  Keep these spaces  \"}");
            var entry = JsonUtility.FromJson<QuestInteractionEntryNodeData>("{\"targetId\":\"  village chief  \"}");

            Assert.That(objective.EventKey, Is.EqualTo("Collect"));
            Assert.That(entry.TargetId, Is.EqualTo("village chief"));
            Assert.That(objective.TargetId, Is.EqualTo(7));
            Assert.That(objective.ObjectiveDescription, Is.EqualTo("  Keep these spaces  "));
        }

        [Test]
        public void QuestObjective_MatchesNormalizedStoredAndReportedEventKeys()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 963;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective", EventKey = "  Collect  ", TargetId = 7, RequiredAmount = 1
            });
            container.Nodes.Add(new QuestFlowEndNodeData { Guid = "end", NewState = QuestState.CanComplete });
            container.NodeLinks.Add(Link("start", QuestPortNames.Next, "objective"));
            container.NodeLinks.Add(Link("objective", QuestPortNames.Next, "end"));
            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestManager.ProcessObjectivesByEvent(controller, "  Collect  ", 7, 1);
            Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.CanComplete));
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

            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
                    dialogueMethod,
                    MethodKind.Condition,
                    "tests.dialogue.choice-visible",
                    DialogueMethodOwner.Global,
                    out DialogueMethodDescriptor dialogueDescriptor,
                    out string dialogueError),
                Is.True,
                dialogueError);
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
                    questMethod,
                    MethodKind.Condition,
                    "tests.quest.is-ready",
                    QuestMethodOwner.Global,
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

            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
                dialogueMethod,
                (MethodKind)999,
                "tests.dialogue.invalid-kind",
                DialogueMethodOwner.Global,
                out _,
                out _), Is.False);
            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
                dialogueMethod,
                MethodKind.Condition,
                "tests.dialogue.invalid-owner",
                (DialogueMethodOwner)999,
                out _,
                out _), Is.False);
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
                questMethod,
                (MethodKind)999,
                "tests.quest.invalid-kind",
                QuestMethodOwner.Global,
                out _,
                out _), Is.False);
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
                questMethod,
                MethodKind.Condition,
                "tests.quest.invalid-target",
                (QuestMethodOwner)999,
                out _,
                out _), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestCustomCondition_StartsAllObjectivesOnTheSelectedPort(bool result)
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 99101;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestConditionNodeData
            {
                Guid = "condition",
                Condition = new MethodBindingData { Key = "tests.quest.run-condition" }
            });
            container.Nodes.Add(new QuestFlowEndNodeData { Guid = "end" });
            container.NodeLinks.Add(Link("start", QuestPortNames.Next, "condition"));
            foreach (string port in new[] { QuestPortNames.True, QuestPortNames.False })
            {
                foreach (int i in new[] { 1, 2 })
                {
                    string guid = port + i;
                    container.Nodes.Add(new QuestObjectiveNodeData { Guid = guid, EventKey = guid });
                    container.NodeLinks.Add(Link("condition", port, guid));
                    container.NodeLinks.Add(Link(guid, QuestPortNames.Next, "end"));
                }
            }
            questRunCondition = _ => result;
            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            string selectedPort = result ? QuestPortNames.True : QuestPortNames.False;
            Assert.That(controller.QuestProgress[container.QuestId].ActiveNodeGuids,
                Is.EquivalentTo(new[] { selectedPort + 1, selectedPort + 2 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestCustomCondition_CollectsAllDialogueCandidatesOnTheSelectedPort(bool result)
        {
            DialogueContainer dialogue = CreateAsset<DialogueContainer>();
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 99102;
            container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            container.Nodes.Add(new QuestConditionNodeData
            {
                Guid = "condition",
                Condition = new MethodBindingData { Key = "tests.quest.run-condition" }
            });
            container.NodeLinks.Add(Link("entry", QuestPortNames.Next, "condition"));
            foreach (string port in new[] { QuestPortNames.True, QuestPortNames.False })
            {
                foreach (int i in new[] { 1, 2 })
                {
                    string guid = port + i;
                    container.Nodes.Add(new DialogueCandidateNodeData
                    {
                        Guid = guid,
                        DisplayName = guid,
                        EntryPoint = new DialogueEntryPoint(dialogue, DialogueEntryNodeData.DefaultEntryId)
                    });
                    container.NodeLinks.Add(Link("condition", port, guid));
                }
            }
            questRunCondition = _ => result;
            QuestManager.Initialize(new[] { container });

            DialogueCandidate[] candidates = QuestManager.GetDialogueCandidates(new FakeQuestController(), "npc");

            string selectedPort = result ? QuestPortNames.True : QuestPortNames.False;
            Assert.That(candidates.Select(candidate => candidate.DisplayName),
                Is.EquivalentTo(new[] { selectedPort + 1, selectedPort + 2 }));
        }

        [TestCase("Quest")]
        [TestCase("  Quest  ")]
        [TestCase("")]
        [TestCase(null)]
        public void QuestManager_ReturnsDialogueFromQuestStateWithoutGameTypes(string displayName)
        {
            DialogueContainer dialogue = CreateAsset<DialogueContainer>();
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 10;
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
                DisplayName = displayName,
                Priority = 5
            };
            container.Nodes.Add(entry);
            container.Nodes.Add(condition);
            container.Nodes.Add(candidate);
            container.NodeLinks.Add(Link("entry", "Next", "condition"));
            container.NodeLinks.Add(Link("condition", "True", "candidate"));

            var controller = new FakeQuestController();
            controller.QuestProgress.Add(10, new QuestProgress(container) { state = QuestState.InProgress });

            QuestManager.Initialize(new[] { container });
            DialogueCandidate[] candidates = QuestManager.GetDialogueCandidates(controller, "npc-7");

            Assert.That(candidates, Has.Length.EqualTo(1));
            Assert.That(candidates[0], Is.Not.SameAs(candidate));
            Assert.That(candidates[0].EntryPoint.Container, Is.SameAs(dialogue));
            Assert.That(candidates[0].DisplayName, Is.EqualTo(displayName));
            Assert.That(candidates[0].Priority, Is.EqualTo(5));

            // 원본을 수정해도 이미 반환한 후보는 유지되고, 다시 조회할 때 새 값이 반영됩니다.
            candidate.EntryPoint = new DialogueEntryPoint(dialogue, "Updated");
            candidate.DisplayName = "Updated";
            candidate.Priority = 9;
            Assert.That(candidates[0].EntryPoint.EntryId, Is.EqualTo("Default"));
            Assert.That(candidates[0].DisplayName, Is.EqualTo(displayName));
            Assert.That(candidates[0].Priority, Is.EqualTo(5));

            DialogueCandidate updatedCandidate = QuestManager.GetDialogueCandidates(controller, "npc-7")[0];
            Assert.That(updatedCandidate, Is.Not.SameAs(candidates[0]));
            Assert.That(updatedCandidate.EntryPoint.EntryId, Is.EqualTo("Updated"));
            Assert.That(updatedCandidate.DisplayName, Is.EqualTo("Updated"));
            Assert.That(updatedCandidate.Priority, Is.EqualTo(9));
        }

        [Test]
        public void QuestSuggestion_RevalidatesPrerequisiteAndCreatesProgressWhenAccepted()
        {
            QuestContainer prerequisiteContainer = CreateAsset<QuestContainer>();
            prerequisiteContainer.QuestId = 40;
            prerequisiteContainer.Nodes.Add(new QuestStartNodeData { Guid = "prerequisite-start" });

            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 41;
            container.questName = "Available Quest";
            var start = new QuestStartNodeData { Guid = "quest-start" };
            var objective = new QuestObjectiveNodeData
            {
                Guid = "objective",
                EventKey = "Talk",
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
                QuestId = prerequisiteContainer.QuestId,
                TargetState = QuestState.TurnedIn
            };
            var availableNodeData = new QuestSuggestionNodeData
            {
                Guid = "available-candidate",
                Priority = 10,
                IsAvailable = true
            };
            var blockedNodeData = new QuestSuggestionNodeData
            {
                Guid = "blocked-candidate",
                Priority = 10,
                IsAvailable = false,
                BlockReason = "선행 Quest 미완료"
            };
            container.Nodes.Add(start);
            container.Nodes.Add(objective);
            container.Nodes.Add(interaction);
            container.Nodes.Add(condition);
            container.Nodes.Add(availableNodeData);
            container.Nodes.Add(blockedNodeData);
            container.NodeLinks.Add(Link("quest-start", "Next", "objective"));
            container.NodeLinks.Add(Link("interaction", "Next", "prerequisite-condition"));
            container.NodeLinks.Add(Link("prerequisite-condition", "True", "available-candidate"));
            container.NodeLinks.Add(Link("prerequisite-condition", "False", "blocked-candidate"));

            QuestManager.Initialize(new[] { prerequisiteContainer, container });
            var controller = new FakeQuestController();

            QuestSuggestion blockedSuggestion = QuestManager.GetQuestSuggestions(controller, "NPC").Single();
            Assert.That(blockedSuggestion.QuestId, Is.EqualTo(container.QuestId));
            Assert.That(blockedSuggestion.IsAvailable, Is.False);
            Assert.That(blockedSuggestion.BlockReason, Is.EqualTo("선행 Quest 미완료"));

            var prerequisiteProgress = new QuestProgress(prerequisiteContainer) { state = QuestState.TurnedIn };
            controller.QuestProgress.Add(prerequisiteContainer.QuestId, prerequisiteProgress);
            QuestSuggestion staleSuggestion = QuestManager.GetQuestSuggestions(controller, "NPC").Single();
            Assert.That(staleSuggestion.IsAvailable, Is.True);

            prerequisiteProgress.state = QuestState.InProgress;
            Assert.That(QuestManager.StartQuest(controller, staleSuggestion), Is.False);
            Assert.That(controller.QuestProgress.ContainsKey(container.QuestId), Is.False);

            prerequisiteProgress.state = QuestState.TurnedIn;
            QuestSuggestion refreshedSuggestion = QuestManager.GetQuestSuggestions(controller, "NPC").Single();
            Assert.That(refreshedSuggestion.IsAvailable, Is.True);
            Assert.That(QuestManager.StartQuest(controller, refreshedSuggestion), Is.True);

            QuestProgress createdProgress = controller.QuestProgress[container.QuestId];
            Assert.That(createdProgress, Is.Not.Null);
            Assert.That(createdProgress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(createdProgress.ActiveNodeGuids, Does.Contain("objective"));
        }

        [Test]
        public void QuestManager_GetQuestSuggestionsReturnsAllMatchesWithoutSelectingForTheGame()
        {
            QuestContainer firstContainer = CreateAsset<QuestContainer>();
            firstContainer.QuestId = 50;
            var firstStart = new QuestStartNodeData { Guid = "first-start" };
            var firstObjective = new QuestObjectiveNodeData
            {
                Guid = "first-objective",
                EventKey = "Talk",
                TargetId = 1,
                RequiredAmount = 1
            };
            var firstInteraction = new QuestInteractionEntryNodeData
            {
                Guid = "first-interaction",
                TargetId = "NPC-MULTI"
            };
            var firstSuggestionNodeData = new QuestSuggestionNodeData
            {
                Guid = "first-candidate",
                Priority = 5
            };
            firstContainer.Nodes.Add(firstStart);
            firstContainer.Nodes.Add(firstObjective);
            firstContainer.Nodes.Add(firstInteraction);
            firstContainer.Nodes.Add(firstSuggestionNodeData);
            firstContainer.NodeLinks.Add(Link("first-start", "Next", "first-objective"));
            firstContainer.NodeLinks.Add(Link("first-interaction", "Next", "first-candidate"));

            QuestContainer secondContainer = CreateAsset<QuestContainer>();
            secondContainer.QuestId = 51;
            var secondStart = new QuestStartNodeData { Guid = "second-start" };
            var secondObjective = new QuestObjectiveNodeData
            {
                Guid = "second-objective",
                EventKey = "Talk",
                TargetId = 2,
                RequiredAmount = 1
            };
            var secondInteraction = new QuestInteractionEntryNodeData
            {
                Guid = "second-interaction",
                TargetId = "NPC-MULTI"
            };
            var secondSuggestionNodeData = new QuestSuggestionNodeData
            {
                Guid = "second-candidate",
                Priority = 10
            };
            secondContainer.Nodes.Add(secondStart);
            secondContainer.Nodes.Add(secondObjective);
            secondContainer.Nodes.Add(secondInteraction);
            secondContainer.Nodes.Add(secondSuggestionNodeData);
            secondContainer.NodeLinks.Add(Link("second-start", "Next", "second-objective"));
            secondContainer.NodeLinks.Add(Link("second-interaction", "Next", "second-candidate"));

            QuestManager.Initialize(new[] { firstContainer, secondContainer });
            var controller = new FakeQuestController();

            QuestSuggestion[] questSuggestions = QuestManager.GetQuestSuggestions(controller, "NPC-MULTI");
            Assert.That(questSuggestions, Has.Length.EqualTo(2));
            Assert.That(questSuggestions.Select(suggestion => suggestion.QuestId),
                Is.EqualTo(new[] { firstContainer.QuestId, secondContainer.QuestId }));
            Assert.That(questSuggestions.Select(suggestion => suggestion.Priority),
                Is.EqualTo(new[] { 5, 10 }));

            firstSuggestionNodeData.Priority = secondSuggestionNodeData.Priority;
            QuestSuggestion[] samePrioritySuggestions = QuestManager.GetQuestSuggestions(controller, "NPC-MULTI");
            Assert.That(samePrioritySuggestions, Has.Length.EqualTo(2));
            Assert.That(samePrioritySuggestions.Select(suggestion => suggestion.QuestId),
                Is.EqualTo(new[] { firstContainer.QuestId, secondContainer.QuestId }));
        }

        [Test]
        public void QuestManager_CompletesObjectiveAndAdvancesToStateChange()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 20;
            var start = new QuestStartNodeData { Guid = "start" };
            var objective = new QuestObjectiveNodeData
            {
                Guid = "objective",
                EventKey = "Kill",
                TargetId = 3,
                RequiredAmount = 3
            };
            var complete = new QuestFlowEndNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            container.Nodes.Add(start);
            container.Nodes.Add(objective);
            container.Nodes.Add(complete);
            container.NodeLinks.Add(Link("start", "Next", "objective"));
            container.NodeLinks.Add(Link("objective", "Next", "complete"));

            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();
            var progress = new QuestProgress(container) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(container.QuestId, progress);

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            Assert.That(progress.ActiveNodeGuids, Does.Contain("objective"));

            Assert.That(QuestManager.ProcessObjectiveByGuid(controller, container.QuestId, "objective", 2), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ObjectiveAmounts["objective"], Is.EqualTo(2));
            QuestObjectiveInfo currentObjective = QuestManager.GetCurrentObjectives(controller, container.QuestId).Single();
            Assert.That(currentObjective.QuestId, Is.EqualTo(container.QuestId));
            Assert.That(currentObjective.NodeGuid, Is.EqualTo("objective"));
            Assert.That(currentObjective.EventKey, Is.EqualTo("Kill"));
            Assert.That(currentObjective.TargetId, Is.EqualTo(3));
            Assert.That(currentObjective.CurrentAmount, Is.EqualTo(2));
            Assert.That(currentObjective.RequiredAmount, Is.EqualTo(3));

            Assert.That(QuestManager.ProcessObjectiveByGuid(controller, container.QuestId, "objective"), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(progress.ActiveNodeGuids, Does.Not.Contain("objective"));
            Assert.That(QuestManager.GetCurrentObjectives(controller, container.QuestId), Is.Empty);
        }

        [Test]
        public void QuestRewardNode_ExecutesWithoutChoosingTheQuestCompletionPolicy()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 23;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestRewardNodeData { Guid = "reward" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective",
                EventKey = "Continue",
                RequiredAmount = 1
            });
            container.NodeLinks.Add(Link("start", "Next", "reward"));
            container.NodeLinks.Add(Link("reward", "Next", "objective"));

            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.CompletedNodeGuids, Does.Contain("reward"));
            Assert.That(progress.ActiveNodeGuids, Does.Contain("objective"));
        }

        [Test]
        public void QuestManager_StopsWithExecutionErrorWhenActionCannotExecute()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 24;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestActionNodeData
            {
                Guid = "action",
                Action = new MethodBindingData { Key = "tests.quest.missing-action" }
            });
            container.NodeLinks.Add(Link("start", "Next", "action"));
            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();
            LogAssert.Expect(LogType.Error, "[Quest] Action 'tests.quest.missing-action'이 등록되지 않았습니다.");

            bool started = QuestManager.StartQuest(controller, container.QuestId);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(started, Is.False);
            Assert.That(progress.state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
        }

        [TestCase(MethodKind.Action, QuestState.Failed)]
        [TestCase(MethodKind.Action, QuestState.NotStarted)]
        [TestCase(MethodKind.Condition, QuestState.Failed)]
        [TestCase(MethodKind.Condition, QuestState.NotStarted)]
        public void QuestManager_StopsOldFlowWhenMethodChangesQuestState(MethodKind kind, QuestState state)
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 1201;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            NodeBaseData stop = kind == MethodKind.Action
                ? new QuestActionNodeData { Guid = "stop", Action = new MethodBindingData { Key = "tests.quest.change-run" } }
                : new QuestConditionNodeData { Guid = "stop", Condition = new MethodBindingData { Key = "tests.quest.run-condition" } };
            container.Nodes.Add(stop);
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "unexpected-objective", RequiredAmount = 1 });
            container.NodeLinks.Add(Link("start", "Next", "stop"));
            container.NodeLinks.Add(Link("stop", kind == MethodKind.Action ? "Next" : "True", "unexpected-objective"));

            questRunAction = context => Assert.That(QuestManager.SetQuestState(context.Controller, container.QuestId, state), Is.True);
            questRunCondition = context =>
            {
                questRunAction(context);
                return true;
            };
            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(state));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("stop"));
        }

        [Test]
        public void QuestManager_ResetAndRestartInsideActionDoesNotResumeOldRun()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 1202;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestConditionNodeData { Guid = "route", Condition = new MethodBindingData { Key = "tests.quest.run-condition" } });
            container.Nodes.Add(new QuestActionNodeData { Guid = "restart", Action = new MethodBindingData { Key = "tests.quest.change-run" } });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "old-objective", RequiredAmount = 1 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "new-objective", RequiredAmount = 1 });
            container.NodeLinks.Add(Link("start", "Next", "route"));
            container.NodeLinks.Add(Link("route", "False", "restart"));
            container.NodeLinks.Add(Link("route", "True", "new-objective"));
            container.NodeLinks.Add(Link("restart", "Next", "old-objective"));

            bool restarted = false;
            questRunCondition = _ => restarted;
            questRunAction = context =>
            {
                restarted = true;
                Assert.That(QuestManager.ResetQuest(context.Controller, container.QuestId), Is.True);
                Assert.That(QuestManager.StartQuest(context.Controller, container.QuestId), Is.True);
            };
            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids, Is.EqualTo(new[] { "new-objective" }));
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("restart"));
            Assert.That(controller.ProgressChangedQuestIds, Has.Count.EqualTo(2));
        }

        [Test]
        public void QuestManager_DoesNotStartNodesIfWaitingQuestStopsTheNewQuest()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 1203;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "unexpected-objective", RequiredAmount = 1 });
            container.NodeLinks.Add(Link("start", "Next", "unexpected-objective"));

            QuestContainer waitingContainer = CreateAsset<QuestContainer>();
            waitingContainer.QuestId = 1204;
            waitingContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            waitingContainer.Nodes.Add(new QuestStateWaitNodeData { Guid = "wait", TargetQuestId = container.QuestId, RequiredState = QuestState.InProgress });
            waitingContainer.Nodes.Add(new QuestActionNodeData { Guid = "stop-other-quest", Action = new MethodBindingData { Key = "tests.quest.change-run" } });
            waitingContainer.Nodes.Add(new QuestObjectiveNodeData { Guid = "waiting-objective", RequiredAmount = 1 });
            waitingContainer.NodeLinks.Add(Link("start", "Next", "wait"));
            waitingContainer.NodeLinks.Add(Link("wait", "Next", "stop-other-quest"));
            waitingContainer.NodeLinks.Add(Link("stop-other-quest", "Next", "waiting-objective"));

            questRunAction = context => Assert.That(QuestManager.SetQuestState(context.Controller, container.QuestId, QuestState.Failed), Is.True);
            QuestManager.Initialize(new[] { container, waitingContainer });
            var controller = new FakeQuestController();
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.Failed));
            Assert.That(controller.QuestProgress[container.QuestId].ActiveNodeGuids, Is.Empty);
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids, Is.EqualTo(new[] { "waiting-objective" }));
        }

        [Test]
        public void QuestManager_RejectsLinksToMissingNodesDuringInitialization()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 21;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.NodeLinks.Add(Link("start", "Next", "missing"));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => QuestManager.Initialize(new[] { container }));

            Assert.That(exception.Message, Does.Contain("존재하지 않는 노드"));
        }

        [Test]
        public void QuestManager_RejectsNonPositiveQuestId()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 0;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => QuestManager.Initialize(new[] { container }));

            Assert.That(exception.Message, Does.Contain("양수를 사용"));
        }

        [Test]
        public void QuestManager_UsesCachedIndexUntilReinitialized()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 22;
            container.Nodes.Add(new QuestStartNodeData { Guid = "old-start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "old-objective",
                EventKey = "Old"
            });
            container.NodeLinks.Add(Link("old-start", "Next", "old-objective"));
            QuestManager.Initialize(new[] { container });

            container.Nodes = new List<NodeBaseData>
            {
                new QuestStartNodeData { Guid = "new-start" },
                new QuestFlowEndNodeData
                {
                    Guid = "new-state",
                    NewState = QuestState.CanComplete
                }
            };
            container.NodeLinks = new List<NodeLinkData>
            {
                Link("new-start", "Next", "new-state")
            };

            var controller = new FakeQuestController();
            var progress = new QuestProgress(container) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(container.QuestId, progress);

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids, Does.Contain("old-objective"));

            QuestManager.Initialize(new[] { container });
            var refreshedController = new FakeQuestController();
            var refreshedProgress = new QuestProgress(container) { state = QuestState.NotStarted };
            refreshedController.QuestProgress.Add(container.QuestId, refreshedProgress);

            Assert.That(QuestManager.StartQuest(refreshedController, container.QuestId), Is.True);
            Assert.That(refreshedProgress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void QuestManager_AndGateDerivesRequiredCountFromConnectedBranches()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 30;
            var start = new QuestStartNodeData { Guid = "start" };
            var first = new QuestObjectiveNodeData
            {
                Guid = "first",
                EventKey = "Collect",
                TargetId = 1,
                RequiredAmount = 1
            };
            var second = new QuestObjectiveNodeData
            {
                Guid = "second",
                EventKey = "Collect",
                TargetId = 2,
                RequiredAmount = 1
            };
#pragma warning disable CS0618
            var gate = new QuestAndGateNodeData { Guid = "gate" };
#pragma warning restore CS0618
            var complete = new QuestFlowEndNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            container.Nodes.Add(start);
            container.Nodes.Add(first);
            container.Nodes.Add(second);
            container.Nodes.Add(gate);
            container.Nodes.Add(complete);
            container.NodeLinks.Add(Link("start", "Next", "first"));
            container.NodeLinks.Add(Link("start", "Next", "second"));
            container.NodeLinks.Add(Link("first", "Next", "gate"));
            container.NodeLinks.Add(Link("second", "Next", "gate"));
            container.NodeLinks.Add(Link("gate", "Next", "complete"));

            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();
            var progress = new QuestProgress(container) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(container.QuestId, progress);

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestManager.ProcessObjectivesByEvent(controller, "Collect", 1, 1);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));

            QuestManager.ProcessObjectivesByEvent(controller, "Collect", 2, 1);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void QuestManager_ReportsOneEventToAllMatchingParallelObjectives()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 36;
            var start = new QuestStartNodeData { Guid = "start" };
            var first = new QuestObjectiveNodeData
            {
                Guid = "first",
                EventKey = "Collect",
                TargetId = 1,
                RequiredAmount = 1
            };
            var second = new QuestObjectiveNodeData
            {
                Guid = "second",
                EventKey = "Collect",
                TargetId = 1,
                RequiredAmount = 1
            };
            var gate = new QuestAndGateNodeData { Guid = "gate" };
            var complete = new QuestFlowEndNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            container.Nodes.Add(start);
            container.Nodes.Add(first);
            container.Nodes.Add(second);
            container.Nodes.Add(gate);
            container.Nodes.Add(complete);
            container.NodeLinks.Add(Link("start", "Next", "first"));
            container.NodeLinks.Add(Link("start", "Next", "second"));
            container.NodeLinks.Add(Link("first", "Next", "gate"));
            container.NodeLinks.Add(Link("second", "Next", "gate"));
            container.NodeLinks.Add(Link("gate", "Next", "complete"));

            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestManager.ProcessObjectivesByEvent(controller, "Collect", 1, 1);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.ObjectiveAmounts["first"], Is.EqualTo(1));
            Assert.That(progress.ObjectiveAmounts["second"], Is.EqualTo(1));
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void QuestManager_ProgressesMultipleQuestsIndependently()
        {
            QuestContainer firstContainer = CreateAsset<QuestContainer>();
            firstContainer.QuestId = 31;
            firstContainer.Nodes.Add(new QuestStartNodeData { Guid = "first-start" });
            firstContainer.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "first-objective",
                EventKey = "Kill",
                TargetId = 101,
                RequiredAmount = 1
            });
            firstContainer.Nodes.Add(new QuestFlowEndNodeData
            {
                Guid = "first-complete",
                NewState = QuestState.CanComplete
            });
            firstContainer.NodeLinks.Add(Link("first-start", "Next", "first-objective"));
            firstContainer.NodeLinks.Add(Link("first-objective", "Next", "first-complete"));

            QuestContainer secondContainer = CreateAsset<QuestContainer>();
            secondContainer.QuestId = 32;
            secondContainer.Nodes.Add(new QuestStartNodeData { Guid = "second-start" });
            secondContainer.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "second-objective",
                EventKey = "Kill",
                TargetId = 202,
                RequiredAmount = 1
            });
            secondContainer.Nodes.Add(new QuestFlowEndNodeData
            {
                Guid = "second-complete",
                NewState = QuestState.CanComplete
            });
            secondContainer.NodeLinks.Add(Link("second-start", "Next", "second-objective"));
            secondContainer.NodeLinks.Add(Link("second-objective", "Next", "second-complete"));

            QuestManager.Initialize(new[] { firstContainer, secondContainer });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, firstContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, secondContainer.QuestId), Is.True);

            QuestProgress firstProgress = controller.QuestProgress[firstContainer.QuestId];
            QuestProgress secondProgress = controller.QuestProgress[secondContainer.QuestId];
            Assert.That(firstProgress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(secondProgress.state, Is.EqualTo(QuestState.InProgress));

            QuestManager.ProcessObjectivesByEvent(controller, "Kill", 101, 1);

            Assert.That(firstProgress.state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(secondProgress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(secondProgress.ActiveNodeGuids, Does.Contain("second-objective"));
            Assert.That(secondProgress.ObjectiveAmounts["second-objective"], Is.Zero);
        }

        [Test]
        public void QuestManager_ResetsAndRestartsQuestWithCleanProgress()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 33;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective",
                EventKey = "Collect",
                TargetId = 7,
                RequiredAmount = 3
            });
            container.NodeLinks.Add(Link("start", "Next", "objective"));

            QuestManager.Initialize(new[] { container });
            var controller = new FakeQuestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            QuestManager.ProcessObjectivesByEvent(controller, "Collect", 7, 1);
            progress.CompletedNodeGuids.Add("old-action");
            progress.CompletedANDGateInputs.Add("old-gate|old-input");

            Assert.That(QuestManager.ResetQuest(controller, container.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.NotStarted));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.ObjectiveAmounts, Is.Empty);
            Assert.That(progress.CompletedNodeGuids, Is.Empty);
            Assert.That(progress.CompletedANDGateInputs, Is.Empty);

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids, Is.EquivalentTo(new[] { "objective" }));
            Assert.That(progress.ObjectiveAmounts["objective"], Is.Zero);

            progress.state = QuestState.TurnedIn;
            Assert.That(QuestManager.ResetQuest(controller, container.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.NotStarted));
        }

        [Test]
        public void QuestWaitForQuest_DoesNotStartTargetAndUsesTheStateChosenByTheDesigner()
        {
            QuestContainer childContainer = CreateAsset<QuestContainer>();
            childContainer.QuestId = 34;
            childContainer.Nodes.Add(new QuestStartNodeData { Guid = "child-start" });
            childContainer.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "child-objective",
                EventKey = "Talk",
                TargetId = 10,
                RequiredAmount = 1
            });
            childContainer.Nodes.Add(new QuestFlowEndNodeData
            {
                Guid = "child-ready",
                NewState = QuestState.CanComplete
            });
            childContainer.NodeLinks.Add(Link("child-start", "Next", "child-objective"));
            childContainer.NodeLinks.Add(Link("child-objective", "Next", "child-ready"));

            QuestContainer parentContainer = CreateAsset<QuestContainer>();
            parentContainer.QuestId = 35;
            parentContainer.Nodes.Add(new QuestStartNodeData { Guid = "parent-start" });
            parentContainer.Nodes.Add(new QuestStateWaitNodeData
            {
                Guid = "sub-quest",
                TargetQuestId = childContainer.QuestId,
                RequiredState = QuestState.CanComplete
            });
            parentContainer.Nodes.Add(new QuestFlowEndNodeData
            {
                Guid = "parent-complete",
                NewState = QuestState.TurnedIn
            });
            parentContainer.NodeLinks.Add(Link("parent-start", "Next", "sub-quest"));
            parentContainer.NodeLinks.Add(Link("sub-quest", "Next", "parent-complete"));

            QuestManager.Initialize(new[] { parentContainer, childContainer });
            var controller = new FakeQuestController();

            Assert.That(QuestManager.StartQuest(controller, parentContainer.QuestId), Is.True);
            Assert.That(controller.QuestProgress.ContainsKey(childContainer.QuestId), Is.False);
            Assert.That(QuestManager.StartQuest(controller, childContainer.QuestId), Is.True);
            Assert.That(QuestManager.ProcessObjectiveByGuid(controller, childContainer.QuestId, "child-objective"), Is.True);
            Assert.That(controller.QuestProgress[childContainer.QuestId].state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(controller.QuestProgress[parentContainer.QuestId].state, Is.EqualTo(QuestState.TurnedIn));
        }

        [Test]
        public void QuestProgress_CollectionsAreGetOnlyAndCreatedPerInstance()
        {
            var first = new QuestProgress();
            var second = new QuestProgress();
            string[] names =
            {
                nameof(QuestProgress.ActiveNodeGuids), nameof(QuestProgress.ObjectiveAmounts),
                nameof(QuestProgress.CompletedNodeGuids), nameof(QuestProgress.CompletedANDGateInputs)
            };

            foreach (string name in names)
            {
                PropertyInfo property = typeof(QuestProgress).GetProperty(name);
                Assert.That(property, Is.Not.Null, name);
                Assert.That(property.CanWrite, Is.False, name);
                object collection = property.GetValue(first);
                Assert.That(collection, Is.Not.Null.And.Empty, name);
                Assert.That(property.GetValue(first), Is.SameAs(collection), name);
                Assert.That(property.GetValue(second), Is.Not.SameAs(collection), name);
            }
        }

        [Test]
        public void QuestProgress_CollectionsAllowContentChangesWithoutAffectingOtherProgress()
        {
            var first = new QuestProgress();
            var second = new QuestProgress();

            first.ActiveNodeGuids.Add("objective");
            first.ObjectiveAmounts["objective"] = 2;
            first.CompletedNodeGuids.Add("action");
            first.CompletedANDGateInputs.Add("gate|objective");

            Assert.That(first.ActiveNodeGuids, Is.EqualTo(new[] { "objective" }));
            Assert.That(first.ObjectiveAmounts["objective"], Is.EqualTo(2));
            Assert.That(first.CompletedNodeGuids, Is.EqualTo(new[] { "action" }));
            Assert.That(first.CompletedANDGateInputs, Is.EqualTo(new[] { "gate|objective" }));
            Assert.That(second.ActiveNodeGuids, Is.Empty);
            Assert.That(second.ObjectiveAmounts, Is.Empty);
            Assert.That(second.CompletedNodeGuids, Is.Empty);
            Assert.That(second.CompletedANDGateInputs, Is.Empty);
        }

        [Test]
        public void QuestSaveData_RestoresEmptyCollectionsWithoutSharingProgress()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 77;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            QuestManager.Initialize(new[] { container });
            var saved = new QuestProgress(container);
            var saveData = new QuestSaveData { QuestList = new List<QuestProgress> { saved } };
            var controller = new FakeQuestController();

            Assert.That(QuestManager.RestoreSaveData(controller, saveData, true, out string error), Is.True, error);
            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress, Is.Not.SameAs(saved));
            Assert.That(progress.questId, Is.EqualTo(saved.questId));
            Assert.That(progress.state, Is.EqualTo(QuestState.NotStarted));
            Assert.That(progress.ActiveNodeGuids, Is.Not.Null.And.Empty);
            Assert.That(progress.ObjectiveAmounts, Is.Not.Null.And.Empty);
            Assert.That(progress.CompletedNodeGuids, Is.Not.Null.And.Empty);
            Assert.That(progress.CompletedANDGateInputs, Is.Not.Null.And.Empty);
        }

        [Test]
        public void QuestSaveData_RoundTripsDictionaryAndFlowCollections()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 77;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective-a",
                EventKey = "Collect",
                RequiredAmount = 10
            });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective-c",
                EventKey = "Visit",
                RequiredAmount = 2
            });
            container.Nodes.Add(new QuestActionNodeData { Guid = "action-1" });
            container.Nodes.Add(new QuestAndGateNodeData { Guid = "gate-1" });
            container.NodeLinks.Add(Link("start", "Next", "objective-a"));
            container.NodeLinks.Add(Link("objective-c", "Next", "gate-1"));
            QuestManager.Initialize(new[] { container });

            var source = new FakeQuestController();
            var progress = new QuestProgress(container) { state = QuestState.InProgress };
            progress.ActiveNodeGuids.Add("objective-a");
            progress.ObjectiveAmounts["objective-a"] = 4;
            progress.ObjectiveAmounts["objective-c"] = 2;
            progress.CompletedNodeGuids.AddRange(new[] { "objective-c", "action-1" });
            progress.CompletedANDGateInputs.Add("gate-1|objective-c");
            FieldInfo runVersion = typeof(QuestProgress).GetField("runVersion", BindingFlags.Instance | BindingFlags.NonPublic);
            runVersion.SetValue(progress, 42);
            source.QuestProgress.Add(container.QuestId, progress);

            QuestSaveData saved = QuestManager.CaptureSaveData(source);
            QuestProgress snapshot = saved.QuestList.Single();
            Assert.That(snapshot, Is.Not.SameAs(progress));
            Assert.That(snapshot.graphSchemaVersion, Is.EqualTo(container.SchemaVersion));
            Assert.That(snapshot.ObjectiveAmounts, Is.Not.SameAs(progress.ObjectiveAmounts));
            Assert.That(runVersion.GetValue(snapshot), Is.EqualTo(0));
            Assert.That(runVersion.GetValue(progress), Is.EqualTo(42));
            runVersion.SetValue(snapshot, 99);

            var target = new FakeQuestController();
            Assert.That(QuestManager.RestoreSaveData(target, saved, shouldClear: true, out string restoreError),
                Is.True,
                restoreError);

            QuestProgress restored = target.QuestProgress[container.QuestId];
            Assert.That(restored, Is.Not.SameAs(snapshot));
            Assert.That(restored.graphSchemaVersion, Is.EqualTo(snapshot.graphSchemaVersion));
            Assert.That(runVersion.GetValue(restored), Is.EqualTo(0));
            Assert.That(restored.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(restored.ActiveNodeGuids, Is.EquivalentTo(new[] { "objective-a" }));
            Assert.That(restored.ObjectiveAmounts["objective-a"], Is.EqualTo(4));
            Assert.That(restored.ObjectiveAmounts["objective-c"], Is.EqualTo(2));
            Assert.That(restored.CompletedNodeGuids, Does.Contain("objective-c"));
            Assert.That(restored.CompletedNodeGuids, Does.Contain("action-1"));
            Assert.That(restored.CompletedANDGateInputs, Does.Contain("gate-1|objective-c"));

            restored.ActiveNodeGuids.Clear();
            restored.ObjectiveAmounts.Clear();
            restored.CompletedNodeGuids.Clear();
            restored.CompletedANDGateInputs.Clear();
            Assert.That(progress.ActiveNodeGuids, Is.EqualTo(new[] { "objective-a" }));
            Assert.That(progress.ObjectiveAmounts, Has.Count.EqualTo(2));
            Assert.That(progress.CompletedNodeGuids, Is.EqualTo(new[] { "objective-c", "action-1" }));
            Assert.That(progress.CompletedANDGateInputs, Is.EqualTo(new[] { "gate-1|objective-c" }));
            Assert.That(snapshot.ActiveNodeGuids, Is.EqualTo(progress.ActiveNodeGuids));
            Assert.That(snapshot.ObjectiveAmounts, Is.EquivalentTo(progress.ObjectiveAmounts));
            Assert.That(snapshot.CompletedNodeGuids, Is.EqualTo(progress.CompletedNodeGuids));
            Assert.That(snapshot.CompletedANDGateInputs, Is.EqualTo(progress.CompletedANDGateInputs));

            progress.ObjectiveAmounts["objective-a"] = 5;
            progress.state = QuestState.Failed;
            Assert.That(snapshot.ObjectiveAmounts["objective-a"], Is.EqualTo(4));
            Assert.That(snapshot.state, Is.EqualTo(QuestState.InProgress));
        }

        [TestCase("", 1)]
        [TestCase(" ", 1)]
        [TestCase("objective", -1)]
        [TestCase("objective", 4)]
        public void QuestSaveData_RejectsInvalidObjectiveAmounts(string nodeGuid, int amount)
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 77;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = 3 });
            container.NodeLinks.Add(Link("start", QuestPortNames.Next, "objective"));
            QuestManager.Initialize(new[] { container });
            var progress = new QuestProgress(container)
            {
                state = QuestState.InProgress,
                ActiveNodeGuids = { "objective" },
                ObjectiveAmounts = { { nodeGuid, amount } }
            };
            var saved = new QuestSaveData { QuestList = new List<QuestProgress> { progress } };
            var controller = new FakeQuestController();

            Assert.That(QuestManager.RestoreSaveData(controller, saved, true, out string error), Is.False);
            Assert.That(controller.QuestProgress, Is.Empty);
            Assert.That(error, Does.Contain(nodeGuid == "objective" ? "범위를 벗어났습니다" : "현재 Objective 노드를 참조하지 않습니다"));
        }

        [Test]
        public void QuestSaveData_RejectsUnknownNodeBeforeChangingController()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 82;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = 1 });
            container.NodeLinks.Add(Link("start", "Next", "objective"));
            QuestManager.Initialize(new[] { container });

            var saveData = new QuestSaveData
            {
                QuestList = new List<QuestProgress>
                {
                    new()
                    {
                        questId = container.QuestId,
                        graphSchemaVersion = container.SchemaVersion,
                        state = QuestState.InProgress,
                        ActiveNodeGuids = { "missing-objective" }
                    }
                }
            };
            var target = new FakeQuestController();
            target.QuestProgress.Add(999, new QuestProgress { questId = 999 });

            bool applied = QuestManager.RestoreSaveData(target, saveData, shouldClear: true, out string error);

            Assert.That(applied, Is.False);
            Assert.That(error, Does.Contain("현재 정의에 없습니다"));
            Assert.That(target.QuestProgress.ContainsKey(999), Is.True);
        }

        [TestCase("negative-amount")]
        [TestCase("unknown-state")]
        [TestCase("duplicate-active-node")]
        public void QuestSaveData_CaptureRejectsDataThatCannotBeRestored(string invalidData)
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 84;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = 3 });
            container.NodeLinks.Add(Link("start", QuestPortNames.Next, "objective"));
            QuestManager.Initialize(new[] { container });
            var progress = new QuestProgress(container) { state = QuestState.InProgress };
            progress.ActiveNodeGuids.Add("objective");
            if (invalidData == "negative-amount")
            {
                progress.ObjectiveAmounts["objective"] = -1;
            }
            else if (invalidData == "unknown-state")
            {
                progress.state = (QuestState)999;
                progress.ActiveNodeGuids.Clear();
            }
            else
            {
                progress.ActiveNodeGuids.Add("objective");
            }

            var saved = new QuestSaveData { QuestList = new List<QuestProgress> { progress } };
            var source = new FakeQuestController();
            source.QuestProgress.Add(progress.questId, progress);
            var target = new FakeQuestController();

            Assert.That(QuestManager.RestoreSaveData(target, saved, true, out string error), Is.False);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => QuestManager.CaptureSaveData(source));
            Assert.That(exception.Message, Is.EqualTo(error));
        }

        [Test]
        public void QuestSaveData_RejectsRequiredAmountForActiveObjective()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 83;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective",
                RequiredAmount = 3
            });
            container.NodeLinks.Add(Link("start", "Next", "objective"));
            QuestManager.Initialize(new[] { container });

            var saveData = new QuestSaveData
            {
                QuestList = new List<QuestProgress>
                {
                    new()
                    {
                        questId = container.QuestId,
                        graphSchemaVersion = container.SchemaVersion,
                        state = QuestState.InProgress,
                        ActiveNodeGuids = { "objective" },
                        ObjectiveAmounts =
                        {
                            { "objective", 3 }
                        }
                    }
                }
            };
            var target = new FakeQuestController();
            target.QuestProgress.Add(999, new QuestProgress { questId = 999 });

            bool applied = QuestManager.RestoreSaveData(target, saveData, shouldClear: true, out string error);

            Assert.That(applied, Is.False);
            Assert.That(error, Does.Contain("활성 Objective"));
            Assert.That(target.QuestProgress.ContainsKey(999), Is.True);
        }

        [Test]
        public void QuestManager_ResumeRestoredQuestsNotifiesOnlyActiveStates()
        {
            QuestContainer inProgressContainer = CreateAsset<QuestContainer>();
            inProgressContainer.QuestId = 78;
            inProgressContainer.Nodes.Add(new QuestStartNodeData { Guid = "in-progress-start" });

            QuestContainer canCompleteContainer = CreateAsset<QuestContainer>();
            canCompleteContainer.QuestId = 79;
            canCompleteContainer.Nodes.Add(new QuestStartNodeData { Guid = "can-complete-start" });

            QuestContainer notStartedContainer = CreateAsset<QuestContainer>();
            notStartedContainer.QuestId = 80;
            notStartedContainer.Nodes.Add(new QuestStartNodeData { Guid = "not-started-start" });

            QuestContainer turnedInContainer = CreateAsset<QuestContainer>();
            turnedInContainer.QuestId = 81;
            turnedInContainer.Nodes.Add(new QuestStartNodeData { Guid = "turned-in-start" });

            QuestManager.Initialize(new[]
            {
                inProgressContainer,
                canCompleteContainer,
                notStartedContainer,
                turnedInContainer
            });

            var controller = new FakeQuestController();
            controller.QuestProgress.Add(inProgressContainer.QuestId,
                new QuestProgress(inProgressContainer) { state = QuestState.InProgress });
            controller.QuestProgress.Add(canCompleteContainer.QuestId,
                new QuestProgress(canCompleteContainer) { state = QuestState.CanComplete });
            controller.QuestProgress.Add(notStartedContainer.QuestId,
                new QuestProgress(notStartedContainer) { state = QuestState.NotStarted });
            controller.QuestProgress.Add(turnedInContainer.QuestId,
                new QuestProgress(turnedInContainer) { state = QuestState.TurnedIn });

            QuestManager.ResumeRestoredQuests(controller);

            Assert.That(controller.ProgressChangedQuestIds,
                Is.EquivalentTo(new[] { inProgressContainer.QuestId, canCompleteContainer.QuestId }));
        }

        [Test]
        public void GraphAssetMigrator_NewGraphsStartAtCurrentVersionAndNeedNoMigration()
        {
            GraphContainer[] graphs = { CreateAsset<DialogueContainer>(), CreateAsset<QuestContainer>() };
            foreach (GraphContainer graph in graphs)
            {
                Assert.That(graph.SchemaVersion, Is.EqualTo(GraphAssetMigrator.CurrentVersion));
                string before = JsonUtility.ToJson(graph);

                for (int i = 0; i < 2; i++)
                {
                    GraphAssetMigrationResult result = GraphAssetMigrator.Migrate(graph);
                    Assert.That(result.BeforeVersion, Is.EqualTo(GraphAssetMigrator.CurrentVersion));
                    Assert.That(result.AfterVersion, Is.EqualTo(GraphAssetMigrator.CurrentVersion));
                    Assert.That(result.Changed, Is.False);
                    Assert.That(JsonUtility.ToJson(graph), Is.EqualTo(before));
                }
            }
        }

        [Test]
        public void GraphAssetMigrator_DoesNotRepairCurrentGraphData()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes = null;
            graph.NodeLinks = null;

            GraphAssetMigrationResult result = GraphAssetMigrator.Migrate(graph);
            Assert.That(result.Changed, Is.False);
            Assert.That(graph.Nodes, Is.Null);
            Assert.That(graph.NodeLinks, Is.Null);
        }

        [TestCase(-1)]
        [TestCase(0)]
        public void GraphAssetMigrator_RejectsPreBaselineSchemaWithoutChangingIt(int version)
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            FieldInfo schemaField = typeof(GraphContainer).GetField(
                "schemaVersion",
                BindingFlags.Instance | BindingFlags.NonPublic);
            schemaField.SetValue(container, version);
            string before = JsonUtility.ToJson(container);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => GraphAssetMigrator.Migrate(container));
            Assert.That(exception.Message, Does.Contain("지원하지 않습니다"));
            Assert.That(JsonUtility.ToJson(container), Is.EqualTo(before));
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

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => GraphAssetMigrator.Migrate(graph));
            Assert.That(exception.Message, Does.Contain("지원하지 않습니다"));
            Assert.That(graph.SchemaVersion, Is.EqualTo(futureVersion));
        }

        [Test]
        public void GraphAssetMigrator_ThrowsForMissingGraph()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => GraphAssetMigrator.Migrate(null));
            Assert.That(exception.Message, Is.EqualTo("그래프 에셋이 필요합니다."));
        }

        [Test]
        public void QuestState_UsesConsecutiveValuesFromZero()
        {
            Assert.That(Enum.GetValues(typeof(QuestState)).Cast<QuestState>().Select(state => (int)state),
                Is.EqualTo(Enumerable.Range(0, 6)));
        }

        [Test]
        public void QuestSaveData_RestoresCurrentVersionWithoutMigration()
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 91;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = 5 });
            container.NodeLinks.Add(Link("start", QuestPortNames.Next, "objective"));
            QuestManager.Initialize(new[] { container });
            var original = new QuestSaveData
            {
                QuestList = new List<QuestProgress>
                {
                    new()
                    {
                        questId = 91,
                        graphSchemaVersion = GraphAssetMigrator.CurrentVersion,
                        state = QuestState.InProgress,
                        ActiveNodeGuids = { "objective" },
                        ObjectiveAmounts =
                        {
                            { "objective", 3 }
                        }
                    }
                }
            };

            Assert.That(QuestSaveData.CurrentSchemaVersion, Is.EqualTo(1));
            var controller = new FakeQuestController();
            Assert.That(QuestManager.RestoreSaveData(controller, original, shouldClear: true, out string error),
                Is.True, error);
            Assert.That(original.schemaVersion, Is.EqualTo(1));
            Assert.That(original.QuestList.Single().graphSchemaVersion,
                Is.EqualTo(GraphAssetMigrator.CurrentVersion));
            Assert.That(controller.QuestProgress[91].ObjectiveAmounts["objective"], Is.EqualTo(3));
        }

        [TestCase(QuestState.Failed)]
        [TestCase(QuestState.ExecutionError)]
        public void QuestSaveData_PreservesStoppedStateAndAllowsExplicitRestart(QuestState state)
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 94;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test" });
            container.NodeLinks.Add(Link("start", QuestPortNames.Next, "goal"));
            QuestManager.Initialize(new[] { container });
            var source = new FakeQuestController();
            source.QuestProgress.Add(container.QuestId, new QuestProgress(container) { state = state });

            QuestSaveData saved = QuestManager.CaptureSaveData(source);
            var restored = new FakeQuestController();
            Assert.That(QuestManager.RestoreSaveData(restored, saved, shouldClear: true, out string error), Is.True, error);
            Assert.That(restored.QuestProgress[container.QuestId].state, Is.EqualTo(state));
            Assert.That(QuestManager.ResetQuest(restored, container.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(restored, container.QuestId), Is.True);
            Assert.That(restored.QuestProgress[container.QuestId].ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        [TestCase(3)]
        public void QuestSaveData_RejectsUnsupportedSchemaWithoutChangingController(int version)
        {
            var saved = new QuestSaveData { schemaVersion = version };
            var controller = new FakeQuestController();
            var previous = new QuestProgress { questId = 93, state = QuestState.TurnedIn };
            controller.QuestProgress.Add(93, previous);

            Assert.That(QuestManager.RestoreSaveData(controller, saved, shouldClear: true, out string error), Is.False);
            Assert.That(error, Does.Contain("지원하지 않습니다"));
            Assert.That(controller.QuestProgress[93], Is.SameAs(previous));
            Assert.That(saved.schemaVersion, Is.EqualTo(version));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public void QuestSaveData_CaptureRejectsInvalidGraphVersionWithoutChangingProgress(int version)
        {
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 93;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = 3 });
            container.NodeLinks.Add(Link("start", QuestPortNames.Next, "objective"));
            QuestManager.Initialize(new[] { container });
            var progress = new QuestProgress(container)
            {
                graphSchemaVersion = version,
                state = QuestState.InProgress,
                ActiveNodeGuids = { "objective" },
                ObjectiveAmounts = { { "objective", 2 } }
            };
            var source = new FakeQuestController();
            source.QuestProgress.Add(container.QuestId, progress);
            var saved = new QuestSaveData { QuestList = new List<QuestProgress> { progress } };
            var target = new FakeQuestController();
            var previous = new QuestProgress(container);
            target.QuestProgress.Add(container.QuestId, previous);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => QuestManager.CaptureSaveData(source));
            Assert.That(QuestManager.RestoreSaveData(target, saved, true, out string error), Is.False);

            Assert.That(exception.Message, Is.EqualTo(error));
            Assert.That(source.QuestProgress[container.QuestId], Is.SameAs(progress));
            Assert.That(progress.graphSchemaVersion, Is.EqualTo(version));
            Assert.That(progress.ActiveNodeGuids, Is.EqualTo(new[] { "objective" }));
            Assert.That(progress.ObjectiveAmounts["objective"], Is.EqualTo(2));
            Assert.That(target.QuestProgress[container.QuestId], Is.SameAs(previous));
        }

        [Test]
        public void QuestSaveData_DoesNotOverwriteFutureGraphVersion()
        {
            int futureVersion = GraphAssetMigrator.CurrentVersion + 1;
            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 93;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            QuestManager.Initialize(new[] { container });
            var progress = new QuestProgress(container)
            {
                graphSchemaVersion = futureVersion,
                state = QuestState.NotStarted
            };

            var saved = new QuestSaveData { QuestList = new List<QuestProgress> { progress } };
            var controller = new FakeQuestController();

            Assert.That(QuestManager.RestoreSaveData(controller, saved, true, out string error), Is.False);
            Assert.That(error, Does.Contain("정의 스키마"));
            Assert.That(progress.graphSchemaVersion, Is.EqualTo(futureVersion));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(999)]
        public void QuestSaveData_RejectsUnregisteredQuestIdWithoutChangingController(int questId)
        {
            QuestManager.Initialize(Array.Empty<QuestContainer>());
            var progress = new QuestProgress
            {
                questId = questId,
                graphSchemaVersion = GraphAssetMigrator.CurrentVersion
            };
            var saved = new QuestSaveData { QuestList = new List<QuestProgress> { progress } };
            var source = new FakeQuestController();
            source.QuestProgress.Add(questId, progress);
            var target = new FakeQuestController();
            var previous = new QuestProgress { questId = 93 };
            target.QuestProgress.Add(previous.questId, previous);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => QuestManager.CaptureSaveData(source));
            Assert.That(QuestManager.RestoreSaveData(target, saved, true, out string error), Is.False);

            Assert.That(error, Does.Contain("등록되지 않았거나 읽을 수 없는 Quest ID"));
            Assert.That(exception.Message, Is.EqualTo(error));
            Assert.That(target.QuestProgress[previous.questId], Is.SameAs(previous));
            Assert.That(target.QuestProgress.Count, Is.EqualTo(1));
        }

        [Test]
        public void QuestManager_InvokesAttributedConditionAndTypedAction()
        {
            attributedQuestActionAmount = 0;
            attributedQuestActionFlag = false;

            QuestContainer container = CreateAsset<QuestContainer>();
            container.QuestId = 88;
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
            var complete = new QuestFlowEndNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            container.Nodes.Add(start);
            container.Nodes.Add(condition);
            container.Nodes.Add(action);
            container.Nodes.Add(complete);
            container.NodeLinks.Add(Link("start", "Next", "condition"));
            container.NodeLinks.Add(Link("condition", "True", "action"));
            container.NodeLinks.Add(Link("action", "Next", "complete"));

            QuestManager.Initialize(new[] { container });
            QuestMethodInvoker.Initialize();
            var controller = new FakeQuestController();
            var progress = new QuestProgress(container) { state = QuestState.NotStarted };
            controller.QuestProgress.Add(container.QuestId, progress);

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

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
        public void DialogueManager_ExecutesActionBeforeShowingFollowingLine()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var action = CreateDialogueRunActionNode("action");
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "After action" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(action);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "action"));
            graph.NodeLinks.Add(Link("action", "Next", "line"));

            var order = new List<string>();
            dialogueRunAction = step => order.Add(step);
            void CaptureLine(DialogueLineNodeData shownLine) => order.Add(shownLine.Guid);
            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
                Assert.That(order, Is.EqualTo(new[] { "action", "line" }));
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(line));
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DialogueManager_FollowsChoicePortWithOptionalActionChain(bool executeActions)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var actionChoice = new DialogueChoiceData { PortName = "accept", ChoiceText = "Accept" };
            var directChoice = new DialogueChoiceData { PortName = "skip", ChoiceText = "Skip" };
            DialogueChoiceData selectedChoice = executeActions ? actionChoice : directChoice;
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData> { actionChoice, directChoice }
            };
            var firstAction = CreateDialogueRunActionNode("first-action");
            var secondAction = CreateDialogueRunActionNode("second-action");
            var resultLine = new DialogueLineNodeData
            {
                Guid = "result",
                DialogueText = "Accepted"
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(firstAction);
            graph.Nodes.Add(secondAction);
            graph.Nodes.Add(resultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", "accept", "first-action"));
            graph.NodeLinks.Add(Link("first-action", "Next", "second-action"));
            graph.NodeLinks.Add(Link("second-action", "Next", "result"));
            graph.NodeLinks.Add(Link("choice", "skip", "result"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            var order = new List<string>();
            var reentrantSelections = new List<bool>();
            int promptId = 0;
            dialogueRunAction = step =>
            {
                order.Add(step);
                reentrantSelections.Add(DialogueManager.Instance.SelectChoice(promptId, selectedChoice));
            };
            void CaptureLine(DialogueLineNodeData line) => order.Add(line.Guid);
            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                DialogueMethodInvoker.Initialize();
                Assert.That(DialogueManager.Instance.StartConversation(
                        new DialogueEntryPoint(graph, "Default"),
                        new DialogueExecutionContext(speaker, interactor)),
                    Is.True);
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.True);
                Assert.That(order, Is.Empty);
                promptId = DialogueManager.Instance.CurrentPromptId;

                Assert.That(
                    DialogueManager.Instance.SelectChoice(promptId, selectedChoice),
                    Is.True);

                Assert.That(DialogueManager.Instance.SelectChoice(promptId, selectedChoice), Is.False);
                Assert.That(order, Is.EqualTo(executeActions
                    ? new[] { "first-action", "second-action", "result" }
                    : new[] { "result" }));
                Assert.That(reentrantSelections, Is.EqualTo(executeActions
                    ? new[] { false, false }
                    : Array.Empty<bool>()));
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(resultLine));
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.False);
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DialogueManager_ActionCompletionStartsNextConversationAfterActionReturns(bool selectAction)
        {
            DialogueContainer firstGraph = CreateAsset<DialogueContainer>();
            var firstEntry = new DialogueEntryNodeData { Guid = "first-entry", EntryId = "Default" };
            var selectedChoice = new DialogueChoiceData { PortName = "end", ChoiceText = "End" };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData> { selectedChoice }
            };
            var endingAction = CreateDialogueRunActionNode("ending-action");
            var endingLine = new DialogueLineNodeData
            {
                Guid = "ending-line",
                DialogueText = "Must not show"
            };
            firstGraph.Nodes.Add(firstEntry);
            firstGraph.Nodes.Add(endingAction);
            firstGraph.Nodes.Add(endingLine);
            firstGraph.NodeLinks.Add(Link("first-entry", "Next", selectAction ? "choice" : "ending-action"));
            firstGraph.NodeLinks.Add(Link("ending-action", "Next", "ending-line"));
            if (selectAction)
            {
                firstGraph.Nodes.Add(choiceNode);
                firstGraph.NodeLinks.Add(Link("choice", "end", "ending-action"));
            }

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
            var order = new List<string>();
            bool secondStarted = false;
            int callbackCount = 0;
            dialogueRunAction = _ =>
            {
                order.Add("action-enter");
                DialogueManager.Instance.CompleteConversation();
                order.Add("action-exit");
            };
            void CaptureLine(DialogueLineNodeData line) => order.Add(line.Guid);
            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                DialogueMethodInvoker.Initialize();
                bool firstStarted = DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(firstGraph, "Default"),
                    executionContext,
                    () =>
                    {
                        callbackCount++;
                        order.Add("completion");
                        secondStarted = DialogueManager.Instance.StartConversation(
                            new DialogueEntryPoint(secondGraph, "Default"),
                            executionContext);
                        DialogueManager.Instance.SendSignal("continue");
                    });

                Assert.That(firstStarted, Is.True);
                if (selectAction)
                {
                    Assert.That(order, Is.Empty);
                    Assert.That(DialogueManager.Instance.SelectChoice(
                        DialogueManager.Instance.CurrentPromptId, selectedChoice), Is.True);
                }
                Assert.That(order, Is.EqualTo(new[] { "action-enter", "action-exit", "completion", "result-line" }));
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(secondStarted, Is.True);
                Assert.That(DialogueManager.Instance.IsConversationActive, Is.True);
                Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(resultLine));
            }
            finally
            {
                DialogueManager.Instance.ShowLine -= CaptureLine;
            }
        }

        [Test]
        public void DialogueManager_SelectedActionFailureDoesNotShowFollowingLineOrComplete()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" };
            var selectedChoice = new DialogueChoiceData { PortName = "run", ChoiceText = "Run" };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData> { selectedChoice }
            };
            var action = new DialogueActionNodeData
            {
                Guid = "action",
                Action = new MethodBindingData { Key = "tests.invoker.throw" }
            };
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Must not show" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(action);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", "run", "action"));
            graph.NodeLinks.Add(Link("action", "Next", "line"));

            int shownCount = 0;
            int callbackCount = 0;
            void CaptureLine(DialogueLineNodeData _) => shownCount++;
            DialogueManager.Instance.ShowLine += CaptureLine;
            try
            {
                Assert.That(DialogueManager.Instance.StartConversation(
                    new DialogueEntryPoint(graph, "Default"), onComplete: () => callbackCount++), Is.True);
                int promptId = DialogueManager.Instance.CurrentPromptId;
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "\\[Dialogue\\] Action 'tests\\.invoker\\.throw' 실행 중 예외가 발생했습니다\\.[\\s\\S]*InvalidOperationException: reflection invocation test"));

                Assert.That(DialogueManager.Instance.SelectChoice(promptId, selectedChoice), Is.True);
                Assert.That(DialogueManager.Instance.SelectChoice(promptId, selectedChoice), Is.False);
                Assert.That(shownCount, Is.Zero);
                Assert.That(callbackCount, Is.Zero);
                Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
                Assert.That(DialogueManager.Instance.LastEndReason, Is.EqualTo(DialogueEndReason.Faulted));
                Assert.That(DialogueManager.Instance.CurrentLine, Is.Null);
                Assert.That(DialogueManager.Instance.CurrentChoices, Is.Empty);
                Assert.That(DialogueManager.Instance.CurrentPromptId, Is.Zero);
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
                ChoiceText = "Second"
            };
            var secondChoiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice-2",
                Choices = new List<DialogueChoiceData> { secondChoice }
            };
            var action = new DialogueActionNodeData
            {
                Guid = "action",
                Action = new MethodBindingData { Key = "tests.dialogue.choice-action" }
            };
            var resultLine = new DialogueLineNodeData { Guid = "result", DialogueText = "Result" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(firstChoiceNode);
            graph.Nodes.Add(secondChoiceNode);
            graph.Nodes.Add(action);
            graph.Nodes.Add(resultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice-1"));
            graph.NodeLinks.Add(Link("choice-1", "accept", "choice-2"));
            graph.NodeLinks.Add(Link("choice-2", "accept", "action"));
            graph.NodeLinks.Add(Link("action", "Next", "result"));

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
            Assert.That(DialogueManager.Instance.SelectChoice(secondPromptId, firstChoice), Is.False);
            Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.True);
            Assert.That(DialogueManager.Instance.CurrentPromptId, Is.EqualTo(secondPromptId));
            Assert.That(DialogueManager.Instance.CurrentChoices, Is.EqualTo(new[] { secondChoice }));
            Assert.That(dialogueChoiceActionCount, Is.Zero);

            Assert.That(DialogueManager.Instance.SelectChoice(secondPromptId, secondChoice), Is.True);
            Assert.That(dialogueChoiceActionCount, Is.EqualTo(1));
            Assert.That(DialogueManager.Instance.CurrentLine, Is.SameAs(resultLine));
        }

        [Test]
        public void DialogueManager_SynchronousChoice_DoesNotReenterChoiceEvent()
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

                DialogueManager.Instance.CompleteConversation();
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
                DialogueManager.Instance.CompleteConversation();

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
                DialogueManager.Instance.CompleteConversation();
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
            DialogueManager.Instance.CompleteConversation();

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

        [QuestAction("tests.quest.change-run", Owner = QuestMethodOwner.Global)]
        internal static void ChangeQuestRun(QuestExecutionContext context)
        {
            questRunAction?.Invoke(context);
        }

        [QuestCondition("tests.quest.run-condition", Owner = QuestMethodOwner.Global)]
        internal static bool EvaluateQuestRunCondition(QuestExecutionContext context)
        {
            return questRunCondition?.Invoke(context) ?? true;
        }

        [QuestCondition("tests.quest.is-ready", Owner = QuestMethodOwner.Global)]
        internal static bool IsAttributedQuestReady(
            QuestExecutionContext context,
            int required)
        {
            return context.Progress?.state == QuestState.InProgress && required == 42;
        }

        [QuestAction("tests.quest.record-action", Owner = QuestMethodOwner.Global)]
        internal static void RecordAttributedQuestAction(
            QuestExecutionContext context,
            int amount,
            bool flag)
        {
            attributedQuestActionAmount = context.Progress == null ? -1 : amount;
            attributedQuestActionFlag = flag;
        }

        [DialogueCondition("tests.dialogue.choice-visible", Owner = DialogueMethodOwner.Global)]
        [QuestCondition("tests.quest.choice-visible", Owner = QuestMethodOwner.Global)]
        private static bool IsDialogueChoiceVisible(bool visible)
        {
            return visible;
        }

        [DialogueAction("tests.dialogue.choice-action", Owner = DialogueMethodOwner.Global)]
        private static void RecordDialogueChoiceAction()
        {
            dialogueChoiceActionCount++;
        }

        [DialogueAction("tests.dialogue.run-action", Owner = DialogueMethodOwner.Global)]
        private static void RunDialogueAction(string step)
        {
            dialogueRunAction?.Invoke(step);
        }

        [DialogueAction("tests.invoker.action", Owner = DialogueMethodOwner.Global)]
        [QuestAction("tests.invoker.action", Owner = QuestMethodOwner.Global)]
        private static void RecordInvokerAction()
        {
            dialogueChoiceActionCount++;
        }

        [DialogueAction("tests.dialogue.context", Owner = DialogueMethodOwner.Global)]
        private static void RecordDialogueContext(DialogueExecutionContext context)
        {
            invokedDialogueContext = context;
        }

        [DialogueAction("tests.invoker.throw", Owner = DialogueMethodOwner.Global)]
        [QuestAction("tests.invoker.throw", Owner = QuestMethodOwner.Global)]
        private static void ThrowInvokerException()
        {
            throw new InvalidOperationException("reflection invocation test");
        }

        [DialogueAction("None", Owner = DialogueMethodOwner.Global)]
        [QuestAction("None", Owner = QuestMethodOwner.Global)]
        private static void RecordNoneKeyAction()
        {
            dialogueChoiceActionCount++;
        }

        [DialogueCondition("None", Owner = DialogueMethodOwner.Global)]
        [QuestCondition("None", Owner = QuestMethodOwner.Global)]
        private static bool EvaluateNoneKeyCondition()
        {
            return true;
        }

        private static void RecordOverloadedAction(int amount)
        {
            overloadedActionAmount = amount;
        }

        private static void RecordOverloadedAction<T>(int amount)
        {
            throw new InvalidOperationException("제네릭 오버로드가 선택되면 안 됩니다.");
        }

        private sealed class FactoryGenericOwner<T>
        {
            public static void Execute() { }
        }

        private static async void FactoryAsyncAction()
        {
            await System.Threading.Tasks.Task.Yield();
        }

        private static void FactoryOptionalAction(int amount = 1) { }

        private static void FactoryParamsAction(params int[] amounts) { }

        [Flags]
        private enum FactoryIntFlags : int { Low = 1, High = 1 << 30 }

        [Flags]
        private enum FactoryLongFlags : long { High = 1L << 40 }

        [Flags]
        private enum FactoryULongFlags : ulong { High = 1UL << 40 }

        private enum FactoryLongEnum : long { High = 1L << 40 }

        private enum FactoryULongEnum : ulong { High = 1UL << 63 }

        private static void AcceptIntFlagsArgument(FactoryIntFlags flags) { }

        private static void AcceptLongFlagsArgument(FactoryLongFlags flags) { }

        private static void AcceptULongFlagsArgument(FactoryULongFlags flags) { }

        private static void AcceptOrdinaryEnumArgument(QuestState state) { }

        private static void AcceptLongEnumArgument(FactoryLongEnum state) { }

        private static void AcceptULongEnumArgument(FactoryULongEnum state) { }

        [DialogueAction("tests.invoker.all-types", Owner = DialogueMethodOwner.Global)]
        [QuestAction("tests.invoker.all-types", Owner = QuestMethodOwner.Global)]
        private static void AcceptAllSupportedArgumentTypes(
            string text,
            bool flag,
            int count,
            float ratio,
            QuestState state,
            DialogueContainer asset)
        {
            invokedArgumentValues = new object[] { text, flag, count, ratio, state, asset };
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
            DialogueManager.Instance.Tick(scaledDeltaTime, unscaledDeltaTime);
        }

        private static DialogueActionNodeData CreateDialogueRunActionNode(string guid)
        {
            return new DialogueActionNodeData
            {
                Guid = guid,
                Action = new MethodBindingData
                {
                    Key = "tests.dialogue.run-action",
                    Arguments = CreateDialogueArguments(nameof(RunDialogueAction), MethodKind.Action, ("arg0", guid))
                }
            };
        }

        private static List<MethodArgumentData> CreateQuestArguments(
            string methodName,
            MethodKind kind,
            params (string Id, object Value)[] values)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
                    method,
                    kind,
                    kind == MethodKind.Action
                        ? "tests.quest.record-action"
                        : "tests.quest.is-ready",
                    QuestMethodOwner.Global,
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
            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
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
            public List<int> ProgressChangedQuestIds { get; } = new();
            public QuestExecutionContext InvokedContext { get; private set; }
            public int InvokedAmount { get; private set; }

            [QuestAction("tests.quest.instance", Owner = QuestMethodOwner.Controller)]
            private void RecordContext(QuestExecutionContext context, int amount)
            {
                InvokedContext = context;
                InvokedAmount = amount;
            }

            public void OnQuestProgressChanged(QuestContainer container, QuestProgress progress)
            {
                ProgressChangedQuestIds.Add(progress.questId);
            }
        }
    }
}
