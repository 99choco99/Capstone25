using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UniversalGraph.Dialogue.Generator
{
    /// <summary>
    /// Generates one dialogue method provider for each Unity script assembly that
    /// references UniversalGraph.Runtime.
    /// </summary>
    [Microsoft.CodeAnalysis.GeneratorAttribute]
    public sealed class DialogueBindingGenerator : ISourceGenerator
    {
        private const string ActionAttributeName =
            "UniversalGraph.Dialogue.DialogueActionAttribute";
        private const string ConditionAttributeName =
            "UniversalGraph.Dialogue.DialogueConditionAttribute";
        private const string ParameterAttributeName =
            "UniversalGraph.Dialogue.DialogueParameterAttribute";
        private const string ContextTypeName =
            "UniversalGraph.Dialogue.DialogueContext";
        private const string ProviderAttributeName =
            "UniversalGraph.Dialogue.DialogueGeneratedProviderAttribute";
        private const string ProviderInterfaceName =
            "UniversalGraph.Dialogue.IDialogueGeneratedMethodProvider";
        private const string SinkInterfaceName =
            "UniversalGraph.Dialogue.IDialogueGeneratedMethodSink";
        private const string RegistrationTypeName =
            "UniversalGraph.Dialogue.DialogueGeneratedMethodRegistration";
        private const string InvokerTypeName =
            "UniversalGraph.Dialogue.DialogueGeneratedMethodInvoker";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;
            INamedTypeSymbol actionAttribute =
                compilation.GetTypeByMetadataName(ActionAttributeName);
            INamedTypeSymbol conditionAttribute =
                compilation.GetTypeByMetadataName(ConditionAttributeName);

            // Assemblies that do not reference UniversalGraph.Runtime are outside
            // this generator's contract and must not receive generated source.
            if (actionAttribute == null || conditionAttribute == null)
                return;

            // This guard lets the analyzer DLL coexist with an older Runtime package
            // during a staged upgrade. Once the generated-provider API is present,
            // every referencing assembly receives a provider, including an empty one.
            if (compilation.GetTypeByMetadataName(ProviderAttributeName) == null ||
                compilation.GetTypeByMetadataName(ProviderInterfaceName) == null ||
                compilation.GetTypeByMetadataName(SinkInterfaceName) == null ||
                compilation.GetTypeByMetadataName(RegistrationTypeName) == null ||
                compilation.GetTypeByMetadataName(InvokerTypeName) == null)
            {
                return;
            }

            var candidates = new List<DialogueBinding>();
            var visitedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
            {
                foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (!visitedMethods.Add(method))
                        continue;

                    foreach (AttributeData attribute in method.GetAttributes())
                    {
                        DialogueBindingKind? kind = null;
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, actionAttribute))
                            kind = DialogueBindingKind.Action;
                        else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, conditionAttribute))
                            kind = DialogueBindingKind.Condition;

                        if (kind.HasValue)
                            candidates.Add(CreateBinding(method, attribute, kind.Value));
                    }
                }
            }

            INamedTypeSymbol parameterAttribute =
                compilation.GetTypeByMetadataName(ParameterAttributeName);
            INamedTypeSymbol contextType =
                compilation.GetTypeByMetadataName(ContextTypeName);
            INamedTypeSymbol componentType =
                compilation.GetTypeByMetadataName("UnityEngine.Component");
            INamedTypeSymbol unityObjectType =
                compilation.GetTypeByMetadataName("UnityEngine.Object");

            var validBindings = new List<DialogueBinding>();
            foreach (DialogueBinding candidate in candidates)
            {
                if (ValidateBinding(
                        context,
                        candidate,
                        parameterAttribute,
                        contextType,
                        componentType,
                        unityObjectType))
                {
                    validBindings.Add(candidate);
                }
            }

            RemoveAndReportDuplicateKeys(context, compilation.AssemblyName, validBindings);
            validBindings.Sort(DialogueBindingComparer.Instance);

            string source = GenerateProvider(compilation.AssemblyName, validBindings);
            string hintName = "UniversalGraph.Dialogue.GeneratedProvider." +
                              DialogueSymbolUtility.GetStableHash(compilation.AssemblyName).ToString("x8") +
                              ".g.cs";
            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(
            INamespaceSymbol rootNamespace)
        {
            foreach (INamedTypeSymbol type in rootNamespace.GetTypeMembers())
            {
                foreach (INamedTypeSymbol result in EnumerateTypeAndNestedTypes(type))
                    yield return result;
            }

            foreach (INamespaceSymbol childNamespace in rootNamespace.GetNamespaceMembers())
            {
                foreach (INamedTypeSymbol result in EnumerateTypes(childNamespace))
                    yield return result;
            }
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypeAndNestedTypes(
            INamedTypeSymbol type)
        {
            yield return type;
            foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
            {
                foreach (INamedTypeSymbol result in EnumerateTypeAndNestedTypes(nestedType))
                    yield return result;
            }
        }

        private static DialogueBinding CreateBinding(
            IMethodSymbol method,
            AttributeData attribute,
            DialogueBindingKind kind)
        {
            string key = null;
            if (attribute.ConstructorArguments.Length > 0)
                key = attribute.ConstructorArguments[0].Value as string;

            int target = 0;
            foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
            {
                if (!string.Equals(namedArgument.Key, "Target", StringComparison.Ordinal) ||
                    namedArgument.Value.Value == null)
                {
                    continue;
                }

                try
                {
                    target = Convert.ToInt32(namedArgument.Value.Value);
                }
                catch (Exception)
                {
                    target = int.MinValue;
                }
            }

            Location location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
                                method.Locations.FirstOrDefault() ??
                                Location.None;

            return new DialogueBinding(method, kind, key, target, location);
        }

        private static bool ValidateBinding(
            GeneratorExecutionContext context,
            DialogueBinding binding,
            INamedTypeSymbol parameterAttribute,
            INamedTypeSymbol contextType,
            INamedTypeSymbol componentType,
            INamedTypeSymbol unityObjectType)
        {
            bool isValid = true;
            IMethodSymbol method = binding.Method;
            string kindName = binding.Kind.ToString();
            string methodName = method.ToDisplayString();

            if (string.IsNullOrWhiteSpace(binding.Key) ||
                string.Equals(binding.Key, "None", StringComparison.OrdinalIgnoreCase))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DialogueDiagnostics.InvalidKey,
                    binding.Location,
                    kindName,
                    methodName));
                isValid = false;
            }

            bool hasCorrectReturn = binding.Kind == DialogueBindingKind.Action
                ? method.ReturnsVoid
                : method.ReturnType.SpecialType == SpecialType.System_Boolean;
            if (!hasCorrectReturn)
            {
                string expected = binding.Kind == DialogueBindingKind.Action ? "void" : "bool";
                ReportInvalidMethod(context, binding, $"return type must be {expected}");
                isValid = false;
            }

            if (method.IsAbstract)
            {
                ReportInvalidMethod(context, binding, "abstract methods cannot be invoked");
                isValid = false;
            }

            if (method.IsGenericMethod ||
                DialogueSymbolUtility.HasOpenGenericContainingType(method.ContainingType))
            {
                ReportInvalidMethod(context, binding, "generic methods and generic declaring types are not supported");
                isValid = false;
            }

            if (method.IsAsync)
            {
                ReportInvalidMethod(context, binding, "async methods are not supported");
                isValid = false;
            }

            if (method.MethodKind != MethodKind.Ordinary &&
                method.MethodKind != MethodKind.ExplicitInterfaceImplementation)
            {
                ReportInvalidMethod(context, binding, "only ordinary methods are supported");
                isValid = false;
            }

            if (method.IsVararg || method.IsExtensionMethod)
            {
                ReportInvalidMethod(
                    context,
                    binding,
                    "varargs and extension methods are not supported");
                isValid = false;
            }

            if (binding.Target < 0 || binding.Target > 2)
            {
                ReportInvalidTarget(context, binding, "Unknown", "the enum value is outside the supported range");
                isValid = false;
            }
            else if (binding.Target == 2)
            {
                if (!method.IsStatic)
                {
                    ReportInvalidTarget(context, binding, "Global", "Global methods must be static");
                    isValid = false;
                }
            }
            else
            {
                string targetName = binding.Target == 0 ? "Speaker" : "Interactor";
                if (method.IsStatic)
                {
                    ReportInvalidTarget(context, binding, targetName, "Speaker/Interactor methods must be instance methods");
                    isValid = false;
                }

                if (!DialogueSymbolUtility.IsOrDerivesFrom(method.ContainingType, componentType))
                {
                    ReportInvalidTarget(context, binding, targetName, "the declaring type must derive from UnityEngine.Component");
                    isValid = false;
                }
            }

            bool hasContextParameter = false;
            var parameterIds = new HashSet<string>(StringComparer.Ordinal);
            binding.Parameters.Clear();
            foreach (IParameterSymbol parameter in method.Parameters)
            {
                Location parameterLocation = parameter.Locations.FirstOrDefault() ?? binding.Location;
                if (parameter.RefKind != RefKind.None)
                {
                    ReportInvalidParameter(
                        context,
                        binding,
                        parameter,
                        parameterLocation,
                        "ref, out and in parameters are not supported");
                    isValid = false;
                }

                if (parameter.IsOptional)
                {
                    ReportInvalidParameter(
                        context,
                        binding,
                        parameter,
                        parameterLocation,
                        "optional parameters are not supported");
                    isValid = false;
                }

                if (parameter.IsParams)
                {
                    ReportInvalidParameter(
                        context,
                        binding,
                        parameter,
                        parameterLocation,
                        "params parameters are not supported");
                    isValid = false;
                }

                if (contextType != null &&
                    SymbolEqualityComparer.Default.Equals(parameter.Type, contextType))
                {
                    if (hasContextParameter)
                    {
                        ReportInvalidParameter(
                            context,
                            binding,
                            parameter,
                            parameterLocation,
                            "DialogueContext can only appear once");
                        isValid = false;
                    }

                    hasContextParameter = true;
                    binding.Parameters.Add(CreateParameterMetadata(parameter, parameter.Name));
                    continue;
                }

                if (!DialogueSymbolUtility.IsSupportedSerializedType(parameter.Type, unityObjectType))
                {
                    ReportInvalidParameter(
                        context,
                        binding,
                        parameter,
                        parameterLocation,
                        $"type '{parameter.Type.ToDisplayString()}' cannot be stored by DialogueArgumentCodec");
                    isValid = false;
                    continue;
                }

                string parameterId = parameter.Name;
                if (parameterAttribute != null)
                {
                    foreach (AttributeData attribute in parameter.GetAttributes())
                    {
                        if (!SymbolEqualityComparer.Default.Equals(
                                attribute.AttributeClass,
                                parameterAttribute))
                        {
                            continue;
                        }

                        parameterId = attribute.ConstructorArguments.Length > 0
                            ? attribute.ConstructorArguments[0].Value as string
                            : null;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(parameterId))
                {
                    ReportInvalidParameter(
                        context,
                        binding,
                        parameter,
                        parameterLocation,
                        "DialogueParameter id cannot be empty");
                    isValid = false;
                }
                else if (!parameterIds.Add(parameterId))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DialogueDiagnostics.DuplicateParameterId,
                        parameterLocation,
                        kindName,
                        methodName,
                        parameterId));
                    isValid = false;
                }

                binding.Parameters.Add(CreateParameterMetadata(parameter, parameterId));
            }

            binding.HasDirectInvoker = isValid && DialogueSymbolUtility.CanEmitDirectCall(method);
            return isValid;
        }

        private static DialogueParameterMetadata CreateParameterMetadata(
            IParameterSymbol parameter,
            string parameterId)
        {
            return new DialogueParameterMetadata(
                parameterId ?? string.Empty,
                parameter.Name,
                DialogueSymbolUtility.GetReflectionTypeName(parameter.Type),
                parameter.Type.ContainingAssembly?.Name ?? string.Empty);
        }

        private static void RemoveAndReportDuplicateKeys(
            GeneratorExecutionContext context,
            string assemblyName,
            List<DialogueBinding> bindings)
        {
            var duplicateSet = new HashSet<DialogueBinding>();
            IEnumerable<IGrouping<string, DialogueBinding>> groups = bindings.GroupBy(
                binding => ((int)binding.Kind).ToString() + "\0" + binding.Key,
                StringComparer.Ordinal);

            foreach (IGrouping<string, DialogueBinding> group in groups)
            {
                if (group.Count() < 2)
                    continue;

                foreach (DialogueBinding binding in group)
                {
                    duplicateSet.Add(binding);
                    context.ReportDiagnostic(Diagnostic.Create(
                        DialogueDiagnostics.DuplicateKey,
                        binding.Location,
                        binding.Kind.ToString(),
                        binding.Key,
                        assemblyName ?? string.Empty));
                }
            }

            bindings.RemoveAll(binding => duplicateSet.Contains(binding));
        }

        private static string GenerateProvider(
            string assemblyName,
            IReadOnlyList<DialogueBinding> bindings)
        {
            string safeAssemblyName = DialogueSymbolUtility.SanitizeIdentifier(assemblyName);
            uint assemblyHash = DialogueSymbolUtility.GetStableHash(assemblyName);
            string providerName = "__DialogueGeneratedProvider_" +
                                  safeAssemblyName + "_" +
                                  assemblyHash.ToString("x8");

            var builder = new StringBuilder(4096);
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#pragma warning disable");
            builder.Append("[assembly: global::UniversalGraph.Dialogue.DialogueGeneratedProviderAttribute(typeof(global::UniversalGraph.Dialogue.Generated.")
                .Append(providerName)
                .AppendLine("))]");
            builder.AppendLine();
            builder.AppendLine("namespace UniversalGraph.Dialogue.Generated");
            builder.AppendLine("{");
            builder.AppendLine("    [global::UnityEngine.Scripting.Preserve]");
            builder.Append("    public sealed class ")
                .Append(providerName)
                .AppendLine(" : global::UniversalGraph.Dialogue.IDialogueGeneratedMethodProvider");
            builder.AppendLine("    {");
            builder.AppendLine("        [global::UnityEngine.Scripting.Preserve]");
            builder.AppendLine("        public void Collect(global::UniversalGraph.Dialogue.IDialogueGeneratedMethodSink sink)");
            builder.AppendLine("        {");

            for (int index = 0; index < bindings.Count; index++)
            {
                DialogueBinding binding = bindings[index];
                builder.AppendLine("            sink.Add(new global::UniversalGraph.Dialogue.DialogueGeneratedMethodRegistration(");
                builder.Append("                global::UniversalGraph.Dialogue.DialogueMethodKind.")
                    .Append(binding.Kind == DialogueBindingKind.Action ? "Action" : "Condition")
                    .AppendLine(",");
                builder.Append("                ")
                    .Append(DialogueSymbolUtility.EscapeString(binding.Key))
                    .AppendLine(",");
                builder.Append("                global::UniversalGraph.Dialogue.DialogueTarget.")
                    .Append(GetTargetName(binding.Target))
                    .AppendLine(",");
                builder.Append("                ")
                    .Append(DialogueSymbolUtility.EscapeString(
                        DialogueSymbolUtility.GetReflectionTypeName(binding.Method.ContainingType)))
                    .AppendLine(",");
                builder.Append("                ")
                    .Append(DialogueSymbolUtility.EscapeString(binding.Method.MetadataName))
                    .AppendLine(",");
                builder.Append("                ")
                    .Append(binding.Method.IsStatic ? "true" : "false")
                    .AppendLine(",");
                builder.AppendLine("                new global::UniversalGraph.Dialogue.DialogueGeneratedParameterRegistration[]");
                builder.AppendLine("                {");
                foreach (DialogueParameterMetadata parameter in binding.Parameters)
                {
                    builder.AppendLine("                    new global::UniversalGraph.Dialogue.DialogueGeneratedParameterRegistration(");
                    builder.Append("                        ")
                        .Append(DialogueSymbolUtility.EscapeString(parameter.ParameterId))
                        .AppendLine(",");
                    builder.Append("                        ")
                        .Append(DialogueSymbolUtility.EscapeString(parameter.DisplayName))
                        .AppendLine(",");
                    builder.Append("                        ")
                        .Append(DialogueSymbolUtility.EscapeString(parameter.TypeMetadataName))
                        .AppendLine(",");
                    builder.Append("                        ")
                        .Append(DialogueSymbolUtility.EscapeString(parameter.TypeAssemblyName))
                        .AppendLine("),");
                }
                builder.AppendLine("                },");
                builder.Append("                ")
                    .Append(binding.HasDirectInvoker ? "Invoke_" + index : "null")
                    .AppendLine("));");
            }

            builder.AppendLine("        }");

            for (int index = 0; index < bindings.Count; index++)
            {
                DialogueBinding binding = bindings[index];
                if (!binding.HasDirectInvoker)
                    continue;

                builder.AppendLine();
                AppendInvoker(builder, binding, index);
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendInvoker(
            StringBuilder builder,
            DialogueBinding binding,
            int index)
        {
            IMethodSymbol method = binding.Method;
            string declaringType = DialogueSymbolUtility.GetSourceTypeName(method.ContainingType);
            string methodName = DialogueSymbolUtility.EscapeIdentifier(method.Name);

            builder.Append("        private static object Invoke_")
                .Append(index)
                .AppendLine("(object target, object[] arguments)");
            builder.AppendLine("        {");

            string receiver = method.IsStatic
                ? declaringType
                : "((" + declaringType + ")target)";
            string invocation = receiver + "." + methodName + "(" +
                                string.Join(", ", method.Parameters.Select(
                                    (parameter, parameterIndex) =>
                                        "(" + DialogueSymbolUtility.GetSourceTypeName(parameter.Type) +
                                        ")arguments[" + parameterIndex + "]")) +
                                ")";

            if (binding.Kind == DialogueBindingKind.Action)
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

        private static void ReportInvalidMethod(
            GeneratorExecutionContext context,
            DialogueBinding binding,
            string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DialogueDiagnostics.InvalidMethod,
                binding.Location,
                binding.Kind.ToString(),
                binding.Method.ToDisplayString(),
                reason));
        }

        private static void ReportInvalidTarget(
            GeneratorExecutionContext context,
            DialogueBinding binding,
            string targetName,
            string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DialogueDiagnostics.InvalidTarget,
                binding.Location,
                binding.Kind.ToString(),
                binding.Method.ToDisplayString(),
                targetName,
                reason));
        }

        private static void ReportInvalidParameter(
            GeneratorExecutionContext context,
            DialogueBinding binding,
            IParameterSymbol parameter,
            Location location,
            string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DialogueDiagnostics.InvalidParameter,
                location,
                binding.Kind.ToString(),
                binding.Method.ToDisplayString(),
                parameter.Name,
                reason));
        }

        private static string GetTargetName(int target)
        {
            switch (target)
            {
                case 0:
                    return "Speaker";
                case 1:
                    return "Interactor";
                case 2:
                    return "Global";
                default:
                    return "Speaker";
            }
        }

        private enum DialogueBindingKind
        {
            Action,
            Condition
        }

        private sealed class DialogueBinding
        {
            internal DialogueBinding(
                IMethodSymbol method,
                DialogueBindingKind kind,
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
            internal DialogueBindingKind Kind { get; }
            internal string Key { get; }
            internal int Target { get; }
            internal Location Location { get; }
            internal bool HasDirectInvoker { get; set; }
            internal List<DialogueParameterMetadata> Parameters { get; } =
                new List<DialogueParameterMetadata>();
        }

        private sealed class DialogueParameterMetadata
        {
            internal DialogueParameterMetadata(
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

        private sealed class DialogueBindingComparer : IComparer<DialogueBinding>
        {
            internal static readonly DialogueBindingComparer Instance =
                new DialogueBindingComparer();

            public int Compare(DialogueBinding left, DialogueBinding right)
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return -1;
                if (right == null)
                    return 1;

                int result = left.Kind.CompareTo(right.Kind);
                if (result != 0)
                    return result;

                result = string.Compare(left.Key, right.Key, StringComparison.Ordinal);
                if (result != 0)
                    return result;

                result = string.Compare(
                    DialogueSymbolUtility.GetReflectionTypeName(left.Method.ContainingType),
                    DialogueSymbolUtility.GetReflectionTypeName(right.Method.ContainingType),
                    StringComparison.Ordinal);
                if (result != 0)
                    return result;

                return string.Compare(
                    left.Method.MetadataName,
                    right.Method.MetadataName,
                    StringComparison.Ordinal);
            }
        }

    }
}


