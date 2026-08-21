using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using UniversalDialogue.Generator;

namespace UniversalDialogue.Generator.Tests
{
    internal static class Program
    {
        private static readonly CSharpParseOptions ParseOptions =
            new CSharpParseOptions(LanguageVersion.CSharp9);

        private static readonly MetadataReference[] PlatformReferences =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        private const string RuntimeContract = @"
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object { }
    public class Component : Object { }
}

namespace UnityEngine.Scripting
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class PreserveAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RequireAttributeUsagesAttribute : Attribute { }
}

namespace UniversalDialogue
{
    public enum DialogueTarget { Speaker, Interactor, Global }
    public enum DialogueMethodKind { Action, Condition }

    [UnityEngine.Scripting.RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class DialogueActionAttribute : UnityEngine.Scripting.PreserveAttribute
    {
        public DialogueActionAttribute(string key) { Key = key; }
        public string Key { get; }
        public DialogueTarget Target { get; set; } = DialogueTarget.Speaker;
    }

    [UnityEngine.Scripting.RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class DialogueConditionAttribute : UnityEngine.Scripting.PreserveAttribute
    {
        public DialogueConditionAttribute(string key) { Key = key; }
        public string Key { get; }
        public DialogueTarget Target { get; set; } = DialogueTarget.Speaker;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class DialogueParameterAttribute : Attribute
    {
        public DialogueParameterAttribute(string id) { Id = id; }
        public string Id { get; }
    }

    public sealed class DialogueContext { }

    [UnityEngine.Scripting.RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DialogueGeneratedProviderAttribute : Attribute
    {
        public DialogueGeneratedProviderAttribute(Type providerType) { ProviderType = providerType; }
        public Type ProviderType { get; }
    }

    public delegate object DialogueGeneratedMethodInvoker(object target, object[] arguments);

    public interface IDialogueGeneratedMethodProvider
    {
        void Collect(IDialogueGeneratedMethodSink sink);
    }

    public interface IDialogueGeneratedMethodSink
    {
        void Add(DialogueGeneratedMethodRegistration registration);
    }

    public sealed class DialogueGeneratedMethodRegistration
    {
        public DialogueGeneratedMethodRegistration(
            DialogueMethodKind kind,
            string key,
            DialogueTarget target,
            string declaringTypeMetadataName,
            string methodMetadataName,
            bool isStatic,
            DialogueGeneratedParameterRegistration[] parameters,
            DialogueGeneratedMethodInvoker directInvoker)
        {
            Kind = kind;
            Key = key;
            Target = target;
            DeclaringTypeMetadataName = declaringTypeMetadataName;
            MethodMetadataName = methodMetadataName;
            IsStatic = isStatic;
            Parameters = parameters;
            DirectInvoker = directInvoker;
        }

        public DialogueMethodKind Kind { get; }
        public string Key { get; }
        public DialogueTarget Target { get; }
        public string DeclaringTypeMetadataName { get; }
        public string MethodMetadataName { get; }
        public bool IsStatic { get; }
        public DialogueGeneratedParameterRegistration[] Parameters { get; }
        public DialogueGeneratedMethodInvoker DirectInvoker { get; }
    }

    public sealed class DialogueGeneratedParameterRegistration
    {
        public DialogueGeneratedParameterRegistration(
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

        public string ParameterId { get; }
        public string DisplayName { get; }
        public string TypeMetadataName { get; }
        public string TypeAssemblyName { get; }
    }

    public sealed class RecordingSink : IDialogueGeneratedMethodSink
    {
        public List<DialogueGeneratedMethodRegistration> Items { get; } =
            new List<DialogueGeneratedMethodRegistration>();

        public void Add(DialogueGeneratedMethodRegistration registration)
        {
            Items.Add(registration);
        }
    }
}
";

        private const string ValidHandlers = @"
using UniversalDialogue;

namespace Game
{
    public sealed class ItemData : UnityEngine.Object { }

    public sealed class Handler : UnityEngine.Component
    {
        public ItemData LastItem;
        public int LastAmount;
        public bool LastSecret;
        public DialogueContext LastContext;

        [DialogueAction(""give_item"", Target = DialogueTarget.Speaker)]
        internal void GiveItem(
            [DialogueParameter(""item_id"")] ItemData item,
            int amount,
            bool isSecret,
            DialogueContext context)
        {
            LastItem = item;
            LastAmount = amount;
            LastSecret = isSecret;
            LastContext = context;
        }

        [DialogueCondition(""has_amount"", Target = DialogueTarget.Global)]
        public static bool HasAmount(int amount)
        {
            return amount == 7;
        }

        [DialogueAction(""private_action"", Target = DialogueTarget.Global)]
        private static void PrivateAction() { }
    }

    internal static class Outer
    {
        internal enum Mode { One, Two }

        [DialogueCondition(""nested_condition"", Target = DialogueTarget.Global)]
        internal static bool IsMode(Mode mode)
        {
            return mode == Mode.Two;
        }
    }
}
";

        private static int passed;

        public static int Main()
        {
            try
            {
                Run("no runtime reference emits nothing", NoRuntimeReferenceEmitsNothing);
                Run("empty runtime assembly emits preserved provider", EmptyProviderIsGenerated);
                Run("valid handlers compile and registrations invoke", ValidProviderCompilesAndInvokes);
                Run("invalid signatures report all contract diagnostics", InvalidSignaturesAreRejected);
                Run("duplicate keys report both declarations", DuplicateKeysAreRejected);
                Run("inaccessible declarations use reflection fallback", InaccessibleDeclarationsUseNullInvoker);

                Console.WriteLine($"PASS: {passed} generator tests");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception);
                return 1;
            }
        }

        private static void NoRuntimeReferenceEmitsNothing()
        {
            GeneratorRun run = RunGenerator("NoRuntimeApi", "public sealed class PlainType { }");
            Assert(run.GeneratedSources.Count == 0, "Source was generated without Runtime API symbols.");
        }

        private static void EmptyProviderIsGenerated()
        {
            GeneratorRun run = RunGenerator("EmptyConsumer", RuntimeContract);
            AssertNoGeneratorErrors(run);
            Assert(run.GeneratedSources.Count == 1, "An empty consumer must get exactly one provider.");

            string source = run.GeneratedSources[0];
            AssertContains(source, "DialogueGeneratedProviderAttribute");
            AssertContains(source, "[global::UnityEngine.Scripting.Preserve]");
            AssertContains(source, "public void Collect");
            Assert(!source.Contains("sink.Add("), "Empty provider unexpectedly contains registrations.");
            AssertNoCompilationErrors(run.OutputCompilation);
        }

        private static void ValidProviderCompilesAndInvokes()
        {
            GeneratorRun run = RunGenerator("ValidConsumer", RuntimeContract, ValidHandlers);
            AssertNoGeneratorErrors(run);
            AssertNoCompilationErrors(run.OutputCompilation);

            string generated = run.GeneratedSources.Single();
            AssertContains(generated, "new global::UniversalDialogue.DialogueGeneratedParameterRegistration(");
            AssertContains(generated, "\"item_id\"");
            AssertContains(generated, "\"Game.ItemData\"");
            AssertContains(generated, "\"Game.Outer+Mode\"");
            AssertContains(generated, "Invoke_");

            Assembly assembly = EmitAndLoad(run.OutputCompilation);
            object provider = CreateGeneratedProvider(assembly);
            Type sinkType = assembly.GetType("UniversalDialogue.RecordingSink", throwOnError: true);
            object sink = Activator.CreateInstance(sinkType);
            provider.GetType().GetMethod("Collect").Invoke(provider, new[] { sink });

            IList items = (IList)sinkType.GetProperty("Items").GetValue(sink);
            Assert(items.Count == 4, $"Expected four registrations, got {items.Count}.");

            object give = FindRegistration(items, "give_item");
            Assert((string)GetProperty(give, "DeclaringTypeMetadataName") == "Game.Handler",
                "Declaring type metadata name is incorrect.");
            Assert((string)GetProperty(give, "MethodMetadataName") == "GiveItem",
                "Method metadata name is incorrect.");
            Assert(!(bool)GetProperty(give, "IsStatic"), "Instance action was marked static.");

            Array giveParameters = (Array)GetProperty(give, "Parameters");
            Assert(giveParameters.Length == 4, "Parameter registration count is incorrect.");
            object itemParameter = giveParameters.GetValue(0);
            Assert((string)GetProperty(itemParameter, "ParameterId") == "item_id",
                "Stable DialogueParameter id was not emitted.");
            Assert((string)GetProperty(itemParameter, "DisplayName") == "item",
                "Parameter display name was not emitted.");
            Assert((string)GetProperty(itemParameter, "TypeMetadataName") == "Game.ItemData",
                "Parameter metadata type name was not emitted.");
            Assert((string)GetProperty(itemParameter, "TypeAssemblyName") == "ValidConsumer",
                "Parameter assembly simple name was not emitted.");

            Type handlerType = assembly.GetType("Game.Handler", throwOnError: true);
            Type itemType = assembly.GetType("Game.ItemData", throwOnError: true);
            Type contextType = assembly.GetType("UniversalDialogue.DialogueContext", throwOnError: true);
            object handler = Activator.CreateInstance(handlerType);
            object item = Activator.CreateInstance(itemType);
            object dialogueContext = Activator.CreateInstance(contextType);
            Delegate giveInvoker = (Delegate)GetProperty(give, "DirectInvoker");
            Assert(giveInvoker != null, "Accessible instance action did not get a direct invoker.");
            object actionResult = giveInvoker.DynamicInvoke(
                handler,
                new object[] { item, 12, true, dialogueContext });
            Assert(actionResult == null, "Action direct invoker must return null.");
            Assert(ReferenceEquals(handlerType.GetField("LastItem").GetValue(handler), item),
                "Direct action invoker passed the wrong Unity object.");
            Assert((int)handlerType.GetField("LastAmount").GetValue(handler) == 12,
                "Direct action invoker passed the wrong integer.");
            Assert((bool)handlerType.GetField("LastSecret").GetValue(handler),
                "Direct action invoker passed the wrong boolean.");
            Assert(ReferenceEquals(handlerType.GetField("LastContext").GetValue(handler), dialogueContext),
                "Direct action invoker passed the wrong DialogueContext.");

            object condition = FindRegistration(items, "has_amount");
            Assert((bool)GetProperty(condition, "IsStatic"), "Global condition was not marked static.");
            Delegate conditionInvoker = (Delegate)GetProperty(condition, "DirectInvoker");
            Assert(conditionInvoker != null, "Accessible condition did not get a direct invoker.");
            Assert((bool)conditionInvoker.DynamicInvoke(null, new object[] { 7 }),
                "Condition direct invoker returned the wrong value.");
            Assert(!(bool)conditionInvoker.DynamicInvoke(null, new object[] { 6 }),
                "Condition direct invoker returned the wrong value.");
        }

        private static void InvalidSignaturesAreRejected()
        {
            const string invalidHandlers = @"
using UniversalDialogue;

namespace Game
{
    public sealed class InvalidHandler : UnityEngine.Component
    {
        [DialogueAction("""", Target = DialogueTarget.Global)]
        public static void EmptyKey() { }

        [DialogueCondition(""wrong_return"", Target = DialogueTarget.Global)]
        public static int WrongReturn() { return 1; }

        [DialogueAction(""wrong_target"")]
        public static void WrongTarget() { }

        [DialogueAction(""unsupported_parameter"", Target = DialogueTarget.Global)]
        public static void Unsupported(decimal value) { }

        [DialogueAction(""duplicate_parameter"", Target = DialogueTarget.Global)]
        public static void Duplicate(
            [DialogueParameter(""same"")] int first,
            [DialogueParameter(""same"")] int second) { }

        [DialogueAction(""vararg_action"", Target = DialogueTarget.Global)]
        public static void VarargAction(__arglist) { }
    }

    public static class InvalidExtensions
    {
        [DialogueAction(""extension_action"", Target = DialogueTarget.Global)]
        public static void ExtensionAction(this InvalidHandler handler) { }
    }
}
";

            GeneratorRun run = RunGenerator("InvalidConsumer", RuntimeContract, invalidHandlers);
            string[] ids = run.GeneratorDiagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.Id)
                .ToArray();

            Assert(ids.Contains("UDG001"), "Missing invalid-key diagnostic.");
            Assert(ids.Contains("UDG002"), "Missing invalid-method diagnostic.");
            Assert(ids.Contains("UDG003"), "Missing invalid-target diagnostic.");
            Assert(ids.Contains("UDG004"), "Missing invalid-parameter diagnostic.");
            Assert(ids.Contains("UDG005"), "Missing duplicate-parameter diagnostic.");
            Assert(!run.GeneratedSources.Single().Contains("wrong_return"),
                "Invalid method leaked into the generated provider.");
            Assert(!run.GeneratedSources.Single().Contains("vararg_action"),
                "Varargs method leaked into the generated provider.");
            Assert(!run.GeneratedSources.Single().Contains("extension_action"),
                "Extension method leaked into the generated provider.");
        }

        private static void DuplicateKeysAreRejected()
        {
            const string duplicateHandlers = @"
using UniversalDialogue;

public static class DuplicateHandlers
{
    [DialogueAction(""duplicate"", Target = DialogueTarget.Global)]
    public static void First() { }

    [DialogueAction(""duplicate"", Target = DialogueTarget.Global)]
    public static void Second() { }

    [DialogueCondition(""duplicate"", Target = DialogueTarget.Global)]
    public static bool DifferentKindIsAllowed() { return true; }
}
";

            GeneratorRun run = RunGenerator("DuplicateConsumer", RuntimeContract, duplicateHandlers);
            Diagnostic[] duplicates = run.GeneratorDiagnostics
                .Where(diagnostic => diagnostic.Id == "UDG006")
                .ToArray();
            Assert(duplicates.Length == 2,
                $"Expected one duplicate-key error per Action declaration, got {duplicates.Length}.");

            string generated = run.GeneratedSources.Single();
            Assert(!generated.Contains("@First("), "First duplicate received a direct invoker.");
            Assert(!generated.Contains("@Second("), "Second duplicate received a direct invoker.");
            AssertContains(generated, "@DifferentKindIsAllowed(");
        }

        private static void InaccessibleDeclarationsUseNullInvoker()
        {
            const string privateHandler = @"
using UniversalDialogue;

internal static class HiddenContainer
{
    private static class HiddenType
    {
        [DialogueAction(""hidden_type"", Target = DialogueTarget.Global)]
        public static void Run() { }
    }
}
";

            GeneratorRun run = RunGenerator("PrivateConsumer", RuntimeContract, privateHandler);
            AssertNoGeneratorErrors(run);
            AssertNoCompilationErrors(run.OutputCompilation);
            string source = run.GeneratedSources.Single();
            AssertContains(source, "\"HiddenContainer+HiddenType\"");
            AssertContains(source, "\"Run\"");
            Assert(!source.Contains("HiddenType.@Run("),
                "Inaccessible declaration received an uncompilable direct call.");

            Assembly assembly = EmitAndLoad(run.OutputCompilation);
            object provider = CreateGeneratedProvider(assembly);
            Type sinkType = assembly.GetType("UniversalDialogue.RecordingSink", throwOnError: true);
            object sink = Activator.CreateInstance(sinkType);
            provider.GetType().GetMethod("Collect").Invoke(provider, new[] { sink });
            IList items = (IList)sinkType.GetProperty("Items").GetValue(sink);
            object registration = FindRegistration(items, "hidden_type");
            Assert(GetProperty(registration, "DirectInvoker") == null,
                "Private nested type should use exact-signature reflection fallback.");
        }

        private static GeneratorRun RunGenerator(string assemblyName, params string[] sources)
        {
            SyntaxTree[] trees = sources
                .Select(source => CSharpSyntaxTree.ParseText(source, ParseOptions))
                .ToArray();
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                trees,
                PlatformReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new ISourceGenerator[] { new DialogueBindingGenerator() },
                parseOptions: ParseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out Compilation outputCompilation,
                out _);

            GeneratorDriverRunResult result = driver.GetRunResult();
            List<string> generatedSources = result.Results
                .SelectMany(generatorResult => generatorResult.GeneratedSources)
                .Select(generated => generated.SourceText.ToString())
                .ToList();
            ImmutableArray<Diagnostic> generatorDiagnostics = result.Diagnostics;

            return new GeneratorRun(
                outputCompilation,
                generatedSources,
                generatorDiagnostics);
        }

        private static Assembly EmitAndLoad(Compilation compilation)
        {
            using var stream = new MemoryStream();
            EmitResult result = compilation.Emit(stream);
            if (!result.Success)
            {
                throw new InvalidOperationException(
                    "Compilation emit failed:\n" +
                    string.Join("\n", result.Diagnostics.Where(
                        diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
            }

            return Assembly.Load(stream.ToArray());
        }

        private static object CreateGeneratedProvider(Assembly assembly)
        {
            object attribute = assembly.GetCustomAttributes()
                .Single(value => value.GetType().FullName ==
                                 "UniversalDialogue.DialogueGeneratedProviderAttribute");
            Type providerType = (Type)attribute.GetType()
                .GetProperty("ProviderType")
                .GetValue(attribute);
            Assert(providerType.GetCustomAttributes()
                    .Any(value => value.GetType().FullName ==
                                  "UnityEngine.Scripting.PreserveAttribute"),
                "Generated provider class is missing Preserve.");
            MethodInfo collect = providerType.GetMethod("Collect");
            Assert(collect.GetCustomAttributes()
                    .Any(value => value.GetType().FullName ==
                                  "UnityEngine.Scripting.PreserveAttribute"),
                "Generated Collect method is missing Preserve.");
            return Activator.CreateInstance(providerType);
        }

        private static object FindRegistration(IList registrations, string key)
        {
            foreach (object registration in registrations)
            {
                if ((string)GetProperty(registration, "Key") == key)
                    return registration;
            }

            throw new InvalidOperationException($"Registration '{key}' was not generated.");
        }

        private static object GetProperty(object target, string name)
        {
            return target.GetType().GetProperty(name).GetValue(target);
        }

        private static void AssertNoGeneratorErrors(GeneratorRun run)
        {
            Diagnostic[] errors = run.GeneratorDiagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();
            Assert(errors.Length == 0,
                "Unexpected generator errors:\n" + string.Join("\n", errors.AsEnumerable()));
        }

        private static void AssertNoCompilationErrors(Compilation compilation)
        {
            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();
            Assert(errors.Length == 0,
                "Generated output does not compile:\n" + string.Join("\n", errors.AsEnumerable()));
        }

        private static void AssertContains(string actual, string expected)
        {
            Assert(actual.Contains(expected), $"Expected generated source to contain: {expected}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Run(string name, Action test)
        {
            test();
            passed++;
            Console.WriteLine("PASS: " + name);
        }

        private sealed class GeneratorRun
        {
            internal GeneratorRun(
                Compilation outputCompilation,
                List<string> generatedSources,
                ImmutableArray<Diagnostic> generatorDiagnostics)
            {
                OutputCompilation = outputCompilation;
                GeneratedSources = generatedSources;
                GeneratorDiagnostics = generatorDiagnostics;
            }

            internal Compilation OutputCompilation { get; }
            internal List<string> GeneratedSources { get; }
            internal ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }
        }
    }
}
