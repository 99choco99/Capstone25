using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using ReflectionAssembly = System.Reflection.Assembly;

namespace UniversalGraph.Editor
{
    /// <summary>Attribute가 붙은 런타임 메서드에 생성된 AOT 메타데이터가 존재하는지 확인합니다.</summary>
    internal static class GraphAotBuildValidator
    {
        private sealed class DialogueSink : IDialogueGeneratedMethodSink
        {
            public readonly List<DialogueGeneratedMethodRegistration> Items = new();
            /// <summary>검증할 Dialogue 생성 등록 정보 하나를 수집합니다.</summary>
            public void Add(DialogueGeneratedMethodRegistration registration) => Items.Add(registration);
        }

        private sealed class QuestSink : IQuestGeneratedMethodSink
        {
            public readonly List<QuestGeneratedMethodRegistration> Items = new();
            /// <summary>검증할 Quest 생성 등록 정보 하나를 수집합니다.</summary>
            public void Add(QuestGeneratedMethodRegistration registration) => Items.Add(registration);
        }

        internal sealed class Report
        {
            internal readonly List<string> Errors = new();
            internal readonly List<string> Warnings = new();
            internal int DialogueMethodCount;
            internal int QuestMethodCount;
        }

        [MenuItem("Tools/Universal Graph/Validate IL2CPP Bindings")]
        private static void ValidateFromMenu()
        {
            Report report = Validate();
            Log(report);
            string summary = report.Errors.Count == 0
                ? $"검증을 통과했습니다. Dialogue: {report.DialogueMethodCount}, Quest: {report.QuestMethodCount}, " +
                  $"경고: {report.Warnings.Count}개."
                : $"오류 {report.Errors.Count}개로 검증에 실패했습니다. Console을 확인하세요.";
            EditorUtility.DisplayDialog("Universal Graph IL2CPP 검증", summary, "확인");
        }

        /// <summary>플레이어 어셈블리에 들어가는 메서드만 검사하고 에디터 전용 예제는 제외합니다.</summary>
        internal static Report Validate()
        {
            var report = new Report();
            var playerAssemblies = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(value => value.name));

            var dialogueMethods = new List<MethodInfo>();
            dialogueMethods.AddRange(TypeCache.GetMethodsWithAttribute<DialogueActionAttribute>());
            dialogueMethods.AddRange(TypeCache.GetMethodsWithAttribute<DialogueConditionAttribute>());
            dialogueMethods = dialogueMethods
                .Where(method => IsPlayerMethod(method, playerAssemblies))
                .Distinct()
                .ToList();

            var questMethods = new List<MethodInfo>();
            questMethods.AddRange(TypeCache.GetMethodsWithAttribute<QuestActionAttribute>());
            questMethods.AddRange(TypeCache.GetMethodsWithAttribute<QuestConditionAttribute>());
            questMethods = questMethods
                .Where(method => IsPlayerMethod(method, playerAssemblies))
                .Distinct()
                .ToList();

            report.DialogueMethodCount = dialogueMethods.Count;
            report.QuestMethodCount = questMethods.Count;

            foreach (IGrouping<ReflectionAssembly, MethodInfo> group in dialogueMethods.GroupBy(method => method.DeclaringType.Assembly))
            {
                ValidateDialogueAssembly(group.Key, group, report);
            }

            foreach (IGrouping<ReflectionAssembly, MethodInfo> group in questMethods.GroupBy(method => method.DeclaringType.Assembly))
            {
                ValidateQuestAssembly(group.Key, group, report);
            }

            return report;
        }

        private static void ValidateDialogueAssembly(
            ReflectionAssembly assembly,
            IEnumerable<MethodInfo> methods,
            Report report)
        {
            var sink = new DialogueSink();
            if (!CollectProviders<DialogueGeneratedProviderAttribute, IDialogueGeneratedMethodProvider>(
                    assembly,
                    attribute => attribute.ProviderType,
                    provider => provider.Collect(sink),
                    report.Errors,
                    "Dialogue"))
            {
                return;
            }

            foreach (MethodInfo method in methods)
            {
                DialogueActionAttribute action = method.GetCustomAttribute<DialogueActionAttribute>(false);
                DialogueConditionAttribute condition = method.GetCustomAttribute<DialogueConditionAttribute>(false);
                MethodKind kind = action != null ? MethodKind.Action : MethodKind.Condition;
                string key = action?.Key ?? condition?.Key;
                DialogueMethodOwner owner = action?.Owner ?? condition.Owner;
                if (!DialogueMethodDescriptorFactory.TryCreateFromReflection(
                        method,
                        kind,
                        key,
                        owner,
                        out _,
                        out string error))
                {
                    report.Errors.Add(error);
                    continue;
                }

                List<DialogueGeneratedMethodRegistration> matches = sink.Items.Where(value =>
                    value != null
                    && value.Kind == kind
                    && value.Key == key
                    && value.DeclaringTypeMetadataName == method.DeclaringType.FullName
                    && value.MethodMetadataName == method.Name).ToList();
                if (matches.Count != 1)
                {
                    report.Errors.Add($"Dialogue 메서드 '{method.DeclaringType.FullName}.{method.Name}'에 " +
                                      $"생성된 등록 정보가 {matches.Count}개 있습니다. 정확히 하나여야 합니다. " +
                                      "Generator DLL을 다시 임포트하세요.");
                }
                else if (matches[0].DirectInvoker == null)
                {
                    report.Warnings.Add($"Dialogue '{key}'가 보존된 Reflection 대체 경로를 사용합니다. " +
                                        "IL2CPP에서 직접 호출하려면 선언 타입과 메서드를 internal 또는 public으로 지정하세요.");
                }
            }
        }

        private static void ValidateQuestAssembly(
            ReflectionAssembly assembly,
            IEnumerable<MethodInfo> methods,
            Report report)
        {
            var sink = new QuestSink();
            if (!CollectProviders<QuestGeneratedProviderAttribute, IQuestGeneratedMethodProvider>(
                    assembly,
                    attribute => attribute.ProviderType,
                    provider => provider.Collect(sink),
                    report.Errors,
                    "Quest"))
            {
                return;
            }

            foreach (MethodInfo method in methods)
            {
                QuestActionAttribute action = method.GetCustomAttribute<QuestActionAttribute>(false);
                QuestConditionAttribute condition = method.GetCustomAttribute<QuestConditionAttribute>(false);
                MethodKind kind = action != null ? MethodKind.Action : MethodKind.Condition;
                string key = action?.Key ?? condition?.Key;
                QuestMethodTarget target = action?.Target ?? condition.Target;
                if (!QuestMethodDescriptorFactory.TryCreateFromReflection(
                        method,
                        kind,
                        key,
                        target,
                        out _,
                        out string error))
                {
                    report.Errors.Add(error);
                    continue;
                }

                List<QuestGeneratedMethodRegistration> matches = sink.Items.Where(value =>
                    value != null
                    && value.Kind == kind
                    && value.Key == key
                    && value.DeclaringTypeMetadataName == method.DeclaringType.FullName
                    && value.MethodMetadataName == method.Name).ToList();
                if (matches.Count != 1)
                {
                    report.Errors.Add($"Quest 메서드 '{method.DeclaringType.FullName}.{method.Name}'에 " +
                                      $"생성된 등록 정보가 {matches.Count}개 있습니다. 정확히 하나여야 합니다. " +
                                      "Generator DLL을 다시 임포트하세요.");
                }
                else if (matches[0].DirectInvoker == null)
                {
                    report.Warnings.Add($"Quest '{key}'가 보존된 Reflection 대체 경로를 사용합니다. " +
                                        "IL2CPP에서 직접 호출하려면 선언 타입과 메서드를 internal 또는 public으로 지정하세요.");
                }
            }
        }

        private static bool CollectProviders<TAttribute, TProvider>(
            ReflectionAssembly assembly,
            Func<TAttribute, Type> getProviderType,
            Action<TProvider> collect,
            ICollection<string> errors,
            string domain)
            where TAttribute : Attribute
        {
            object[] attributes;
            try
            {
                attributes = assembly.GetCustomAttributes(typeof(TAttribute), false);
            }
            catch (Exception exception)
            {
                errors.Add($"어셈블리 '{assembly.GetName().Name}'에서 {domain} Provider를 읽지 못했습니다: " +
                           exception.Message);
                return false;
            }
            if (attributes.Length == 0)
            {
                errors.Add($"어셈블리 '{assembly.GetName().Name}'의 {domain} 처리기에 생성된 Provider가 없습니다.");
                return false;
            }

            bool success = true;
            foreach (TAttribute attribute in attributes.Cast<TAttribute>())
            {
                Type providerType = getProviderType(attribute);
                if (providerType == null
                    || providerType.Assembly != assembly
                    || !typeof(TProvider).IsAssignableFrom(providerType))
                {
                    errors.Add($"어셈블리 '{assembly.GetName().Name}'의 {domain} Provider가 올바르지 않습니다.");
                    success = false;
                    continue;
                }

                try
                {
                    collect((TProvider)Activator.CreateInstance(providerType, true));
                }
                catch (Exception exception)
                {
                    errors.Add($"{domain} Provider '{providerType.FullName}' 실행에 실패했습니다: {exception.Message}");
                    success = false;
                }
            }
            return success;
        }

        private static bool IsPlayerMethod(MethodInfo method, ISet<string> playerAssemblies)
        {
            return method?.DeclaringType != null
                   && playerAssemblies.Contains(method.DeclaringType.Assembly.GetName().Name);
        }

        /// <summary>AOT 검증 경고와 오류를 Unity Console에 출력합니다.</summary>
        internal static void Log(Report report)
        {
            foreach (string warning in report.Warnings)
            {
                Debug.LogWarning("[Universal Graph AOT] " + warning);
            }
            foreach (string error in report.Errors)
            {
                Debug.LogError("[Universal Graph AOT] " + error);
            }
        }
    }

    /// <summary>생성된 메서드 정보가 없거나 오래되었으면 IL2CPP 빌드를 차단합니다.</summary>
    internal sealed class GraphAotBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        /// <summary>IL2CPP 빌드 전에 바인딩을 검증하고 안전하지 않은 등록이 있으면 빌드를 중단합니다.</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(report.summary.platform);
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            if (PlayerSettings.GetScriptingBackend(namedTarget) != ScriptingImplementation.IL2CPP)
            {
                return;
            }

            GraphAotBuildValidator.Report validation = GraphAotBuildValidator.Validate();
            GraphAotBuildValidator.Log(validation);
            if (validation.Errors.Count > 0)
            {
                throw new BuildFailedException(
                    $"Universal Graph IL2CPP 검증이 오류 {validation.Errors.Count}개로 실패했습니다. " +
                    "Console을 열거나 Tools/Universal Graph/Validate IL2CPP Bindings를 실행하세요.");
            }
        }
    }
}
