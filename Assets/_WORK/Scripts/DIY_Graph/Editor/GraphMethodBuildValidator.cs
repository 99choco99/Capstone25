using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniversalGraph.Editor
{
    /// <summary>Attribute가 붙은 런타임 메서드의 선언과 중복 키를 Reflection으로 검증합니다.</summary>
    internal static class GraphMethodBuildValidator
    {
        internal sealed class Report
        {
            internal readonly List<string> Errors = new();
            internal int DialogueMethodCount;
            internal int QuestMethodCount;
        }

        /// <summary>스크립트가 다시 로드되면 잘못된 메서드 선언을 게임 실행 전에 알려줍니다.</summary>
        [InitializeOnLoadMethod]
        private static void ValidateAfterReload()
        {
            EditorApplication.delayCall += () => Log(Validate());
        }

        [MenuItem("Tools/Universal Graph/Validate Method Bindings")]
        private static void ValidateFromMenu()
        {
            Report report = Validate();
            Log(report);
            string summary = report.Errors.Count == 0
                ? $"검증을 통과했습니다. Dialogue: {report.DialogueMethodCount}, Quest: {report.QuestMethodCount}."
                : $"오류 {report.Errors.Count}개로 검증에 실패했습니다. Console을 확인하세요.";
            EditorUtility.DisplayDialog("Universal Graph 메서드 검증", summary, "확인");
        }

        /// <summary>플레이어 어셈블리에 들어가는 메서드만 검사하고 에디터 전용 예제는 제외합니다.</summary>
        internal static Report Validate()
        {
            var playerAssemblies = new HashSet<string>();
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                playerAssemblies.Add(assembly.name);

                // 현재 플레이어에서 참조하는 DLL의 Attribute 메서드도 검사합니다.
                foreach (string reference in assembly.compiledAssemblyReferences)
                {
                    playerAssemblies.Add(Path.GetFileNameWithoutExtension(reference));
                }
            }

            var methods = new List<MethodInfo>();
            methods.AddRange(TypeCache.GetMethodsWithAttribute<DialogueActionAttribute>());
            methods.AddRange(TypeCache.GetMethodsWithAttribute<DialogueConditionAttribute>());
            methods.AddRange(TypeCache.GetMethodsWithAttribute<QuestActionAttribute>());
            methods.AddRange(TypeCache.GetMethodsWithAttribute<QuestConditionAttribute>());

            return Validate(methods.Where(method => method.DeclaringType != null
                && playerAssemblies.Contains(method.DeclaringType.Assembly.GetName().Name)));
        }

        /// <summary>그래프에서 아직 선택하지 않은 메서드도 검사하고 어셈블리 사이의 중복 키를 찾습니다.</summary>
        internal static Report Validate(IEnumerable<MethodInfo> methods)
        {
            var report = new Report();
            var methodsByKey = new Dictionary<(string Domain, MethodKind Kind, string Key), MethodDescriptor>();

            foreach (MethodInfo method in methods.Distinct())
            {
                // 한 메서드에 여러 Attribute가 붙어 있어도 각각의 사용 규칙을 검사합니다.
                DialogueActionAttribute dialogueAction = method.GetCustomAttribute<DialogueActionAttribute>(false);
                if (dialogueAction != null)
                {
                    ValidateDialogueMethod(method, MethodKind.Action, dialogueAction.Key, dialogueAction.Owner);
                }

                DialogueConditionAttribute dialogueCondition = method.GetCustomAttribute<DialogueConditionAttribute>(false);
                if (dialogueCondition != null)
                {
                    ValidateDialogueMethod(method, MethodKind.Condition, dialogueCondition.Key, dialogueCondition.Owner);
                }

                QuestActionAttribute questAction = method.GetCustomAttribute<QuestActionAttribute>(false);
                if (questAction != null)
                {
                    ValidateQuestMethod(method, MethodKind.Action, questAction.Key, questAction.Target);
                }

                QuestConditionAttribute questCondition = method.GetCustomAttribute<QuestConditionAttribute>(false);
                if (questCondition != null)
                {
                    ValidateQuestMethod(method, MethodKind.Condition, questCondition.Key, questCondition.Target);
                }
            }

            return report;

            void ValidateDialogueMethod(MethodInfo method, MethodKind kind, string key, DialogueMethodOwner owner)
            {
                report.DialogueMethodCount++;
                if (!DialogueMethodDescriptorFactory.TryCreateFromReflection(
                        method, kind, key, owner, out DialogueMethodDescriptor descriptor, out string error))
                {
                    report.Errors.Add("[Dialogue] " + error);
                    return;
                }

                AddMethod("Dialogue", descriptor);
            }

            void ValidateQuestMethod(MethodInfo method, MethodKind kind, string key, QuestMethodTarget target)
            {
                report.QuestMethodCount++;
                if (!QuestMethodDescriptorFactory.TryCreateFromReflection(
                        method, kind, key, target, out QuestMethodDescriptor descriptor, out string error))
                {
                    report.Errors.Add("[Quest] " + error);
                    return;
                }

                AddMethod("Quest", descriptor);
            }

            void AddMethod(string domain, MethodDescriptor descriptor)
            {
                var key = (domain, descriptor.Kind, descriptor.Key);
                if (methodsByKey.TryGetValue(key, out MethodDescriptor existing))
                {
                    report.Errors.Add($"[{domain}] 중복된 {descriptor.Kind} 키 '{descriptor.Key}': " +
                        $"{existing.QualifiedMethodName}, {descriptor.QualifiedMethodName}.");
                    return;
                }

                methodsByKey.Add(key, descriptor);
            }
        }

        /// <summary>메서드 검증 오류를 Unity Console에 출력합니다.</summary>
        internal static void Log(Report report)
        {
            foreach (string error in report.Errors)
            {
                Debug.LogError("[Universal Graph] " + error);
            }
        }
    }

    /// <summary>잘못된 메서드 선언이 있으면 스크립팅 백엔드와 관계없이 빌드를 차단합니다.</summary>
    internal sealed class GraphMethodBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        /// <summary>빌드 전에 바인딩을 검증하고 안전하지 않은 등록이 있으면 빌드를 중단합니다.</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            GraphMethodBuildValidator.Report validation = GraphMethodBuildValidator.Validate();
            GraphMethodBuildValidator.Log(validation);
            if (validation.Errors.Count > 0)
            {
                throw new BuildFailedException(
                    $"Universal Graph 메서드 검증이 오류 {validation.Errors.Count}개로 실패했습니다. " +
                    "Console을 열거나 Tools/Universal Graph/Validate Method Bindings를 실행하세요.");
            }
        }
    }
}
