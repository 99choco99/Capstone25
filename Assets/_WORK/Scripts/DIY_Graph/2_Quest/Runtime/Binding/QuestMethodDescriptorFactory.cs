using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UniversalGraph
{
    /// <summary>Attribute가 붙은 Quest 메서드 시그니처를 검증하고 호출 정보를 만듭니다.</summary>
    public static class QuestMethodDescriptorFactory
    {
        /// <summary>Source Generator 메타데이터로 설명 정보를 만들고 Reflection은 대체 경로로만 사용합니다.</summary>
        internal static bool TryCreateGenerated(
            Assembly sourceAssembly,
            QuestGeneratedMethodRegistration registration,
            out QuestMethodDescriptor descriptor,
            out string error)
        {
            descriptor = null;
            if (sourceAssembly == null || registration == null)
            {
                error = "생성된 Quest 메서드의 어셈블리 또는 등록 정보가 null입니다.";
                return false;
            }

            string key = registration.Key?.Trim();
            if (string.IsNullOrWhiteSpace(key)
                || key == "None")
            {
                error = "생성된 Quest 메서드 키가 비어 있거나 예약 값 'None'입니다.";
                return false;
            }

            if (registration.Kind != MethodKind.Action
                && registration.Kind != MethodKind.Condition)
            {
                error = $"'{key}'의 Quest 메서드 종류가 올바르지 않습니다.";
                return false;
            }

            if (registration.Target != QuestMethodTarget.Controller
                && registration.Target != QuestMethodTarget.Global)
            {
                error = $"'{key}'의 Quest 호출 대상이 올바르지 않습니다.";
                return false;
            }

            if (!TryResolveGeneratedType(
                    sourceAssembly,
                    registration.DeclaringTypeMetadataName,
                    sourceAssembly.GetName().Name,
                    out Type declaringType))
            {
                error = $"'{key}'의 선언 타입 '{registration.DeclaringTypeMetadataName}'을 찾을 수 없습니다.";
                return false;
            }

            if (registration.Target == QuestMethodTarget.Global)
            {
                if (!registration.IsStatic)
                {
                    error = $"Global Quest 메서드 '{key}'는 static이어야 합니다.";
                    return false;
                }
            }
            else if (registration.IsStatic || !typeof(IQuestController).IsAssignableFrom(declaringType))
            {
                error = $"Controller Quest 메서드 '{key}'는 IQuestController의 인스턴스 메서드여야 합니다.";
                return false;
            }

            GeneratedParameterRegistration[] generatedParameters =
                registration.Parameters ?? Array.Empty<GeneratedParameterRegistration>();
            var parameters = new MethodParameterDescriptor[generatedParameters.Length];
            var signatureTypes = new Type[generatedParameters.Length];
            var parameterIds = new HashSet<string>();
            bool hasContext = false;

            for (int index = 0; index < generatedParameters.Length; index++)
            {
                GeneratedParameterRegistration generated = generatedParameters[index];
                if (generated == null
                    || !TryResolveGeneratedType(
                        sourceAssembly,
                        generated.TypeMetadataName,
                        generated.TypeAssemblyName,
                        out Type parameterType))
                {
                    error = $"'{key}'의 {index}번 파라미터 타입을 찾을 수 없습니다.";
                    return false;
                }

                signatureTypes[index] = parameterType;
                string displayName = string.IsNullOrWhiteSpace(generated.DisplayName)
                    ? $"arg{index}"
                    : generated.DisplayName;
                if (parameterType == typeof(QuestExecutionContext))
                {
                    if (hasContext)
                    {
                        error = $"'{key}'는 QuestExecutionContext를 한 번만 받을 수 있습니다.";
                        return false;
                    }

                    hasContext = true;
                    parameters[index] = new MethodParameterDescriptor(
                        index,
                        displayName,
                        displayName,
                        parameterType,
                        MethodParameterSource.QuestExecutionContext,
                        MethodArgumentKind.String);
                    continue;
                }

                if (!MethodArgumentCodec.TryGetArgumentKind(parameterType, out MethodArgumentKind argumentKind))
                {
                    error = $"'{key}'의 파라미터 '{displayName}' 타입 '{parameterType.FullName}'은 지원하지 않습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(generated.ParameterId)
                    || !parameterIds.Add(generated.ParameterId))
                {
                    error = $"'{key}'에 비어 있거나 중복된 파라미터 ID '{generated.ParameterId}'가 있습니다.";
                    return false;
                }

                var parameter = new MethodParameterDescriptor(
                    index,
                    generated.ParameterId,
                    displayName,
                    parameterType,
                    MethodParameterSource.Serialized,
                    argumentKind);
                parameters[index] = parameter;
            }

            MethodInfo method = null;
            if (registration.DirectInvoker == null)
            {
                method = declaringType.GetMethod(
                    registration.MethodMetadataName,
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    signatureTypes,
                    null);
                Type returnType = registration.Kind == MethodKind.Action ? typeof(void) : typeof(bool);
                if (method == null
                    || method.IsStatic != registration.IsStatic
                    || method.ReturnType != returnType
                    || method.IsAbstract
                    || method.IsGenericMethodDefinition
                    || method.ContainsGenericParameters)
                {
                    error = $"'{key}'의 Reflection 대체 시그니처가 생성된 메타데이터와 일치하지 않습니다.";
                    return false;
                }
            }

            descriptor = new QuestMethodDescriptor(
                key,
                registration.Kind,
                registration.Target,
                declaringType,
                registration.MethodMetadataName,
                registration.IsStatic,
                method,
                parameters,
                registration.DirectInvoker);
            error = null;
            return true;
        }

        /// <summary>Attribute가 붙은 Quest 메서드 하나를 검증하고 에디터·런타임 호출 정보를 만듭니다.</summary>
        public static bool TryCreateFromReflection(
            MethodInfo method,
            MethodKind kind,
            string key,
            QuestMethodTarget target,
            out QuestMethodDescriptor descriptor,
            out string error)
        {
            descriptor = null;
            if (method == null)
            {
                error = "MethodInfo가 null입니다.";
                return false;
            }

            string qualifiedName = $"{method.DeclaringType?.FullName}.{method.Name}";
            key = key?.Trim();
            if (string.IsNullOrWhiteSpace(key) || key == "None")
            {
                error = $"'{qualifiedName}'에 빈 키가 있거나 예약 값 'None'을 사용했습니다.";
                return false;
            }

            if (kind != MethodKind.Action && kind != MethodKind.Condition)
            {
                error = $"'{key}'의 Quest 메서드 종류가 올바르지 않습니다.";
                return false;
            }

            if (target != QuestMethodTarget.Controller && target != QuestMethodTarget.Global)
            {
                error = $"'{key}'의 Quest 호출 대상이 올바르지 않습니다.";
                return false;
            }

            Type expectedReturn = kind == MethodKind.Action ? typeof(void) : typeof(bool);
            if (method.ReturnType != expectedReturn)
            {
                error = $"'{key}' ({qualifiedName})는 {expectedReturn.Name} 타입을 반환해야 합니다.";
                return false;
            }

            if (method.DeclaringType == null
                || method.IsAbstract
                || method.IsSpecialName
                || method.IsGenericMethodDefinition
                || method.ContainsGenericParameters
                || method.DeclaringType.ContainsGenericParameters)
            {
                error = $"'{key}' ({qualifiedName})는 구체적인 타입에 선언된 제네릭이 아닌 구체적인 메서드여야 합니다.";
                return false;
            }

            if ((method.CallingConvention & CallingConventions.VarArgs) != 0
                || method.IsDefined(typeof(ExtensionAttribute), inherit: false)
                || method.GetCustomAttribute<AsyncStateMachineAttribute>(inherit: false) != null)
            {
                error = $"'{key}' ({qualifiedName})는 가변 인수, 확장 메서드 또는 async 메서드일 수 없습니다.";
                return false;
            }

            if (target == QuestMethodTarget.Global && !method.IsStatic)
            {
                error = $"Global Quest 메서드 '{key}'는 static이어야 합니다.";
                return false;
            }

            if (target == QuestMethodTarget.Controller
                && (method.IsStatic || !typeof(IQuestController).IsAssignableFrom(method.DeclaringType)))
            {
                error = $"Controller Quest 메서드 '{key}'는 IQuestController의 인스턴스 메서드여야 합니다.";
                return false;
            }

            ParameterInfo[] methodParameters = method.GetParameters();
            var parameters = new MethodParameterDescriptor[methodParameters.Length];
            int serializedParameterCount = 0;
            bool hasContext = false;

            for (int index = 0; index < methodParameters.Length; index++)
            {
                ParameterInfo parameter = methodParameters[index];
                Type parameterType = parameter.ParameterType;
                string parameterName = parameter.Name ?? $"arg{index}";
                if (parameterType.IsByRef || parameter.IsOut || parameter.IsIn)
                {
                    error = $"'{key}'의 파라미터 '{parameterName}'에는 ref, out, in을 사용할 수 없습니다.";
                    return false;
                }

                if (parameter.IsOptional || parameter.GetCustomAttribute<ParamArrayAttribute>(false) != null)
                {
                    error = $"'{key}'의 파라미터 '{parameterName}'는 선택적 파라미터 또는 params일 수 없습니다.";
                    return false;
                }

                if (parameterType == typeof(QuestExecutionContext))
                {
                    if (hasContext)
                    {
                        error = $"'{key}'는 QuestExecutionContext를 한 번만 받을 수 있습니다.";
                        return false;
                    }

                    hasContext = true;
                    parameters[index] = new MethodParameterDescriptor(
                        index,
                        parameterName,
                        parameterName,
                        parameterType,
                        MethodParameterSource.QuestExecutionContext,
                        MethodArgumentKind.String);
                    continue;
                }

                if (!MethodArgumentCodec.TryGetArgumentKind(parameterType, out MethodArgumentKind argumentKind))
                {
                    error = $"'{key}'의 파라미터 '{parameterName}' 타입 '{parameterType.FullName}'은 지원하지 않습니다.";
                    return false;
                }

                string parameterId = $"arg{serializedParameterCount++}";

                var descriptorParameter = new MethodParameterDescriptor(
                    index,
                    parameterId,
                    parameterName,
                    parameterType,
                    MethodParameterSource.Serialized,
                    argumentKind);
                parameters[index] = descriptorParameter;
            }

            descriptor = new QuestMethodDescriptor(
                key,
                kind,
                target,
                method,
                parameters);
            error = null;
            return true;
        }

        private static bool TryResolveGeneratedType(
            Assembly sourceAssembly,
            string typeMetadataName,
            string typeAssemblyName,
            out Type type)
        {
            type = null;
            if (string.IsNullOrWhiteSpace(typeMetadataName)
                || string.IsNullOrWhiteSpace(typeAssemblyName))
            {
                return false;
            }

            if (sourceAssembly.GetName().Name == typeAssemblyName)
            {
                type = sourceAssembly.GetType(typeMetadataName, false, false);
                return type != null;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != typeAssemblyName)
                {
                    continue;
                }

                type = assembly.GetType(typeMetadataName, false, false);
                if (type != null)
                {
                    return true;
                }
            }

            type = Type.GetType($"{typeMetadataName}, {typeAssemblyName}", false, false);
            return type != null;
        }
    }
}
