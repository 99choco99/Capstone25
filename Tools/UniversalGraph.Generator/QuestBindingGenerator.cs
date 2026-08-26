using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UniversalGraph.Generator
{
    /// <summary>Generates AOT-safe Quest registrations and direct invokers for every consumer assembly.</summary>
    public sealed class QuestBindingGenerator : ISourceGenerator
    {
        private const string ActionAttributeName = "UniversalGraph.QuestActionAttribute";
        private const string ConditionAttributeName = "UniversalGraph.QuestConditionAttribute";
        private const string QuestParameterAttributeName = "UniversalGraph.QuestParameterAttribute";
        private const string DialogueParameterAttributeName = "UniversalGraph.DialogueParameterAttribute";
        private const string ContextTypeName = "UniversalGraph.QuestExecutionContext";
        private const string ControllerInterfaceName = "UniversalGraph.IQuestController";
        private const string ProviderAttributeName = "UniversalGraph.QuestGeneratedProviderAttribute";
        private const string ProviderInterfaceName = "UniversalGraph.IQuestGeneratedMethodProvider";
        private const string SinkInterfaceName = "UniversalGraph.IQuestGeneratedMethodSink";
        private const string RegistrationTypeName = "UniversalGraph.QuestGeneratedMethodRegistration";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;
            INamedTypeSymbol actionAttribute = compilation.GetTypeByMetadataName(ActionAttributeName);
            INamedTypeSymbol conditionAttribute = compilation.GetTypeByMetadataName(ConditionAttributeName);
            if (actionAttribute == null || conditionAttribute == null)
            {
                return;
            }

            if (compilation.GetTypeByMetadataName(ProviderAttributeName) == null
                || compilation.GetTypeByMetadataName(ProviderInterfaceName) == null
                || compilation.GetTypeByMetadataName(SinkInterfaceName) == null
                || compilation.GetTypeByMetadataName(RegistrationTypeName) == null)
            {
                return;
            }

            var candidates = new List<QuestBinding>();
            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
            {
                foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (!visited.Add(method))
                    {
                        continue;
                    }

                    foreach (AttributeData attribute in method.GetAttributes())
                    {
                        QuestBindingKind? kind = null;
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, actionAttribute))
                        {
                            kind = QuestBindingKind.Action;
                        }
                        else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, conditionAttribute))
                        {
                            kind = QuestBindingKind.Condition;
                        }

                        if (kind.HasValue)
                        {
                            candidates.Add(CreateBinding(method, attribute, kind.Value));
                        }
                    }
                }
            }

            INamedTypeSymbol questParameterAttribute =
                compilation.GetTypeByMetadataName(QuestParameterAttributeName);
            INamedTypeSymbol dialogueParameterAttribute =
                compilation.GetTypeByMetadataName(DialogueParameterAttributeName);
            INamedTypeSymbol contextType = compilation.GetTypeByMetadataName(ContextTypeName);
            INamedTypeSymbol controllerInterface =
                compilation.GetTypeByMetadataName(ControllerInterfaceName);
            INamedTypeSymbol unityObjectType =
                compilation.GetTypeByMetadataName("UnityEngine.Object");

            var valid = new List<QuestBinding>();
            foreach (QuestBinding candidate in candidates)
            {
                if (ValidateBinding(
                        context,
                        candidate,
                        questParameterAttribute,
                        dialogueParameterAttribute,
                        contextType,
                        controllerInterface,
                        unityObjectType))
                {
                    valid.Add(candidate);
                }
            }

            RemoveAndReportDuplicateKeys(context, compilation.AssemblyName, valid);
            valid.Sort(QuestBindingComparer.Instance);
            string source = GenerateProvider(compilation.AssemblyName, valid);
            string hintName = "UniversalGraph.Quest.GeneratedProvider." +
                              DialogueSymbolUtility.GetStableHash(compilation.AssemblyName).ToString("x8") +
                              ".g.cs";
            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }

        private static QuestBinding CreateBinding(
            IMethodSymbol method,
            AttributeData attribute,
            QuestBindingKind kind)
        {
            string key = attribute.ConstructorArguments.Length > 0
                ? attribute.ConstructorArguments[0].Value as string
                : null;
            int target = 0;
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (!string.Equals(argument.Key, "Target", StringComparison.Ordinal)
                    || argument.Value.Value == null)
                {
                    continue;
                }

                try
                {
                    target = Convert.ToInt32(argument.Value.Value);
                }
                catch
                {
                    target = int.MinValue;
                }
            }

            Location location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                                ?? method.Locations.FirstOrDefault()
                                ?? Location.None;
            return new QuestBinding(method, kind, key, target, location);
        }

        private static bool ValidateBinding(
            GeneratorExecutionContext context,
            QuestBinding binding,
            INamedTypeSymbol questParameterAttribute,
            INamedTypeSymbol dialogueParameterAttribute,
            INamedTypeSymbol contextType,
            INamedTypeSymbol controllerInterface,
            INamedTypeSymbol unityObjectType)
        {
            bool valid = true;
            IMethodSymbol method = binding.Method;
            string kindName = "Quest " + binding.Kind;
            string methodName = method.ToDisplayString();

            if (string.IsNullOrWhiteSpace(binding.Key)
                || string.Equals(binding.Key, "None", StringComparison.OrdinalIgnoreCase))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    QuestDiagnostics.InvalidKey,
                    binding.Location,
                    kindName,
                    methodName));
                valid = false;
            }

            bool correctReturn = binding.Kind == QuestBindingKind.Action
                ? method.ReturnsVoid
                : method.ReturnType.SpecialType == SpecialType.System_Boolean;
            if (!correctReturn)
            {
                ReportInvalidMethod(
                    context,
                    binding,
                    $"반환 타입은 {(binding.Kind == QuestBindingKind.Action ? "void" : "bool")}이어야 합니다.");
                valid = false;
            }

            if (method.IsAbstract
                || method.IsGenericMethod
                || DialogueSymbolUtility.HasOpenGenericContainingType(method.ContainingType)
                || method.IsAsync
                || method.IsVararg
                || method.IsExtensionMethod
                || method.MethodKind != MethodKind.Ordinary
                   && method.MethodKind != MethodKind.ExplicitInterfaceImplementation)
            {
                ReportInvalidMethod(
                    context,
                    binding,
                    "구체 타입에 선언된 비제네릭 동기식 일반 메서드만 지원합니다.");
                valid = false;
            }

            if (binding.Target < 0 || binding.Target > 1)
            {
                ReportInvalidTarget(context, binding, "Unknown", "열거형 값이 지원 범위를 벗어났습니다.");
                valid = false;
            }
            else if (binding.Target == 1)
            {
                if (!method.IsStatic)
                {
                    ReportInvalidTarget(context, binding, "Global", "Global 메서드는 static이어야 합니다.");
                    valid = false;
                }
            }
            else
            {
                if (method.IsStatic)
                {
                    ReportInvalidTarget(context, binding, "Controller", "Controller 메서드는 인스턴스 메서드여야 합니다.");
                    valid = false;
                }

                if (!ImplementsInterface(method.ContainingType, controllerInterface))
                {
                    ReportInvalidTarget(
                        context,
                        binding,
                        "Controller",
                        "선언 타입은 IQuestController를 구현해야 합니다.");
                    valid = false;
                }
            }

            bool hasContext = false;
            var parameterIds = new HashSet<string>(StringComparer.Ordinal);
            binding.Parameters.Clear();
            foreach (IParameterSymbol parameter in method.Parameters)
            {
                Location parameterLocation = parameter.Locations.FirstOrDefault() ?? binding.Location;
                if (parameter.RefKind != RefKind.None)
                {
                    ReportInvalidParameter(context, binding, parameter, parameterLocation,
                        "ref, out, in 파라미터는 지원하지 않습니다.");
                    valid = false;
                }
                if (parameter.IsOptional)
                {
                    ReportInvalidParameter(context, binding, parameter, parameterLocation,
                        "선택적 파라미터는 지원하지 않습니다.");
                    valid = false;
                }
                if (parameter.IsParams)
                {
                    ReportInvalidParameter(context, binding, parameter, parameterLocation,
                        "params 파라미터는 지원하지 않습니다.");
                    valid = false;
                }

                if (contextType != null
                    && SymbolEqualityComparer.Default.Equals(parameter.Type, contextType))
                {
                    if (hasContext)
                    {
                        ReportInvalidParameter(context, binding, parameter, parameterLocation,
                            "QuestExecutionContext는 한 번만 사용할 수 있습니다.");
                        valid = false;
                    }
                    hasContext = true;
                    binding.Parameters.Add(CreateParameter(parameter, parameter.Name));
                    continue;
                }

                if (!DialogueSymbolUtility.IsSupportedSerializedType(parameter.Type, unityObjectType))
                {
                    ReportInvalidParameter(
                        context,
                        binding,
                        parameter,
                        parameterLocation,
                        $"'{parameter.Type.ToDisplayString()}' 타입은 MethodArgumentCodec으로 저장할 수 없습니다.");
                    valid = false;
                    continue;
                }

                string parameterId = GetParameterId(
                    parameter,
                    questParameterAttribute,
                    dialogueParameterAttribute);
                if (string.IsNullOrWhiteSpace(parameterId))
                {
                    ReportInvalidParameter(context, binding, parameter, parameterLocation,
                        "QuestParameter ID는 비워 둘 수 없습니다.");
                    valid = false;
                }
                else if (!parameterIds.Add(parameterId))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        QuestDiagnostics.DuplicateParameterId,
                        parameterLocation,
                        kindName,
                        methodName,
                        parameterId));
                    valid = false;
                }

                binding.Parameters.Add(CreateParameter(parameter, parameterId));
            }

            binding.HasDirectInvoker = valid && DialogueSymbolUtility.CanEmitDirectCall(method);
            return valid;
        }

        private static string GetParameterId(
            IParameterSymbol parameter,
            INamedTypeSymbol questParameterAttribute,
            INamedTypeSymbol dialogueParameterAttribute)
        {
            AttributeData dialogueFallback = null;
            foreach (AttributeData attribute in parameter.GetAttributes())
            {
                if (questParameterAttribute != null
                    && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, questParameterAttribute))
                {
                    return attribute.ConstructorArguments.Length > 0
                        ? attribute.ConstructorArguments[0].Value as string
                        : null;
                }

                if (dialogueParameterAttribute != null
                    && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, dialogueParameterAttribute))
                {
                    dialogueFallback = attribute;
                }
            }

            return dialogueFallback == null
                ? parameter.Name
                : dialogueFallback.ConstructorArguments.Length > 0
                    ? dialogueFallback.ConstructorArguments[0].Value as string
                    : null;
        }

        private static QuestParameterMetadata CreateParameter(
            IParameterSymbol parameter,
            string parameterId)
        {
            return new QuestParameterMetadata(
                parameterId ?? string.Empty,
                parameter.Name,
                DialogueSymbolUtility.GetReflectionTypeName(parameter.Type),
                parameter.Type.ContainingAssembly?.Name ?? string.Empty);
        }

        private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceType)
        {
            if (type == null || interfaceType == null)
            {
                return false;
            }
            return type.AllInterfaces.Any(candidate =>
                SymbolEqualityComparer.Default.Equals(candidate, interfaceType));
        }

        private static void RemoveAndReportDuplicateKeys(
            GeneratorExecutionContext context,
            string assemblyName,
            List<QuestBinding> bindings)
        {
            var duplicates = new HashSet<QuestBinding>();
            foreach (IGrouping<string, QuestBinding> group in bindings.GroupBy(
                         binding => ((int)binding.Kind) + "\0" + binding.Key,
                         StringComparer.Ordinal))
            {
                if (group.Count() < 2)
                {
                    continue;
                }

                foreach (QuestBinding binding in group)
                {
                    duplicates.Add(binding);
                    context.ReportDiagnostic(Diagnostic.Create(
                        QuestDiagnostics.DuplicateKey,
                        binding.Location,
                        "Quest " + binding.Kind,
                        binding.Key,
                        assemblyName ?? string.Empty));
                }
            }
            bindings.RemoveAll(binding => duplicates.Contains(binding));
        }

        private static string GenerateProvider(
            string assemblyName,
            IReadOnlyList<QuestBinding> bindings)
        {
            string providerName = "__QuestGeneratedProvider_" +
                                  DialogueSymbolUtility.SanitizeIdentifier(assemblyName) + "_" +
                                  DialogueSymbolUtility.GetStableHash(assemblyName).ToString("x8");
            var builder = new StringBuilder(4096);
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#pragma warning disable");
            builder.Append("[assembly: global::UniversalGraph.QuestGeneratedProviderAttribute(typeof(global::UniversalGraph.Quest.Generated.")
                .Append(providerName)
                .AppendLine("))]");
            builder.AppendLine("namespace UniversalGraph.Quest.Generated");
            builder.AppendLine("{");
            builder.AppendLine("    [global::UnityEngine.Scripting.Preserve]");
            builder.Append("    public sealed class ").Append(providerName)
                .AppendLine(" : global::UniversalGraph.IQuestGeneratedMethodProvider");
            builder.AppendLine("    {");
            builder.AppendLine("        [global::UnityEngine.Scripting.Preserve]");
            builder.AppendLine("        public void Collect(global::UniversalGraph.IQuestGeneratedMethodSink sink)");
            builder.AppendLine("        {");

            for (int index = 0; index < bindings.Count; index++)
            {
                QuestBinding binding = bindings[index];
                builder.AppendLine("            sink.Add(new global::UniversalGraph.QuestGeneratedMethodRegistration(");
                builder.Append("                global::UniversalGraph.MethodKind.")
                    .Append(binding.Kind == QuestBindingKind.Action ? "Action" : "Condition").AppendLine(",");
                builder.Append("                ").Append(DialogueSymbolUtility.EscapeString(binding.Key)).AppendLine(",");
                builder.Append("                global::UniversalGraph.QuestMethodTarget.")
                    .Append(binding.Target == 1 ? "Global" : "Controller").AppendLine(",");
                builder.Append("                ").Append(DialogueSymbolUtility.EscapeString(
                    DialogueSymbolUtility.GetReflectionTypeName(binding.Method.ContainingType))).AppendLine(",");
                builder.Append("                ").Append(DialogueSymbolUtility.EscapeString(binding.Method.MetadataName)).AppendLine(",");
                builder.Append("                ").Append(binding.Method.IsStatic ? "true" : "false").AppendLine(",");
                builder.AppendLine("                new global::UniversalGraph.GeneratedParameterRegistration[]");
                builder.AppendLine("                {");
                foreach (QuestParameterMetadata parameter in binding.Parameters)
                {
                    builder.AppendLine("                    new global::UniversalGraph.GeneratedParameterRegistration(");
                    builder.Append("                        ").Append(DialogueSymbolUtility.EscapeString(parameter.ParameterId)).AppendLine(",");
                    builder.Append("                        ").Append(DialogueSymbolUtility.EscapeString(parameter.DisplayName)).AppendLine(",");
                    builder.Append("                        ").Append(DialogueSymbolUtility.EscapeString(parameter.TypeMetadataName)).AppendLine(",");
                    builder.Append("                        ").Append(DialogueSymbolUtility.EscapeString(parameter.TypeAssemblyName)).AppendLine("),");
                }
                builder.AppendLine("                },");
                builder.Append("                ").Append(binding.HasDirectInvoker ? "Invoke_" + index : "null").AppendLine("));");
            }

            builder.AppendLine("        }");
            for (int index = 0; index < bindings.Count; index++)
            {
                if (bindings[index].HasDirectInvoker)
                {
                    AppendInvoker(builder, bindings[index], index);
                }
            }
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendInvoker(StringBuilder builder, QuestBinding binding, int index)
        {
            IMethodSymbol method = binding.Method;
            string declaringType = DialogueSymbolUtility.GetSourceTypeName(method.ContainingType);
            string receiver = method.IsStatic ? declaringType : "((" + declaringType + ")target)";
            string invocation = receiver + "." + DialogueSymbolUtility.EscapeIdentifier(method.Name) + "(" +
                                string.Join(", ", method.Parameters.Select((parameter, parameterIndex) =>
                                    "(" + DialogueSymbolUtility.GetSourceTypeName(parameter.Type) + ")arguments[" +
                                    parameterIndex + "]")) + ")";
            builder.AppendLine();
            builder.Append("        private static object Invoke_").Append(index)
                .AppendLine("(object target, object[] arguments)");
            builder.AppendLine("        {");
            if (binding.Kind == QuestBindingKind.Action)
            {
                builder.Append("            ").Append(invocation).AppendLine(";");
                builder.AppendLine("            return null;");
            }
            else
            {
                builder.Append("            return ").Append(invocation).AppendLine(";");
            }
            builder.AppendLine("        }");
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
        {
            foreach (INamedTypeSymbol type in root.GetTypeMembers())
            {
                foreach (INamedTypeSymbol nested in EnumerateTypeAndNestedTypes(type))
                {
                    yield return nested;
                }
            }
            foreach (INamespaceSymbol child in root.GetNamespaceMembers())
            {
                foreach (INamedTypeSymbol type in EnumerateTypes(child))
                {
                    yield return type;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypeAndNestedTypes(INamedTypeSymbol type)
        {
            yield return type;
            foreach (INamedTypeSymbol nested in type.GetTypeMembers())
            {
                foreach (INamedTypeSymbol value in EnumerateTypeAndNestedTypes(nested))
                {
                    yield return value;
                }
            }
        }

        private static void ReportInvalidMethod(
            GeneratorExecutionContext context,
            QuestBinding binding,
            string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                QuestDiagnostics.InvalidMethod,
                binding.Location,
                "Quest " + binding.Kind,
                binding.Method.ToDisplayString(),
                reason));
        }

        private static void ReportInvalidTarget(
            GeneratorExecutionContext context,
            QuestBinding binding,
            string target,
            string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                QuestDiagnostics.InvalidTarget,
                binding.Location,
                "Quest " + binding.Kind,
                binding.Method.ToDisplayString(),
                target,
                reason));
        }

        private static void ReportInvalidParameter(
            GeneratorExecutionContext context,
            QuestBinding binding,
            IParameterSymbol parameter,
            Location location,
            string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                QuestDiagnostics.InvalidParameter,
                location,
                "Quest " + binding.Kind,
                binding.Method.ToDisplayString(),
                parameter.Name,
                reason));
        }

        private enum QuestBindingKind
        {
            Action,
            Condition
        }

        private sealed class QuestBinding
        {
            internal QuestBinding(
                IMethodSymbol method,
                QuestBindingKind kind,
                string key,
                int target,
                Location location)
            {
                Method = method;
                Kind = kind;
                Key = key;
                Target = target;
                Location = location;
            }

            internal IMethodSymbol Method { get; }
            internal QuestBindingKind Kind { get; }
            internal string Key { get; }
            internal int Target { get; }
            internal Location Location { get; }
            internal bool HasDirectInvoker { get; set; }
            internal List<QuestParameterMetadata> Parameters { get; } = new List<QuestParameterMetadata>();
        }

        private sealed class QuestParameterMetadata
        {
            internal QuestParameterMetadata(
                string parameterId,
                string displayName,
                string typeMetadataName,
                string typeAssemblyName)
            {
                ParameterId = parameterId;
                DisplayName = displayName;
                TypeMetadataName = typeMetadataName;
                TypeAssemblyName = typeAssemblyName;
            }

            internal string ParameterId { get; }
            internal string DisplayName { get; }
            internal string TypeMetadataName { get; }
            internal string TypeAssemblyName { get; }
        }

        private sealed class QuestBindingComparer : IComparer<QuestBinding>
        {
            internal static readonly QuestBindingComparer Instance = new QuestBindingComparer();

            public int Compare(QuestBinding left, QuestBinding right)
            {
                if (ReferenceEquals(left, right)) return 0;
                if (left == null) return -1;
                if (right == null) return 1;
                int result = left.Kind.CompareTo(right.Kind);
                if (result != 0) return result;
                result = string.Compare(left.Key, right.Key, StringComparison.Ordinal);
                if (result != 0) return result;
                result = string.Compare(
                    DialogueSymbolUtility.GetReflectionTypeName(left.Method.ContainingType),
                    DialogueSymbolUtility.GetReflectionTypeName(right.Method.ContainingType),
                    StringComparison.Ordinal);
                return result != 0
                    ? result
                    : string.Compare(left.Method.MetadataName, right.Method.MetadataName, StringComparison.Ordinal);
            }
        }
    }
}
