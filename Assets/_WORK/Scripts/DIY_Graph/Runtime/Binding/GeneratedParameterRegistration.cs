using System.ComponentModel;

namespace UniversalGraph
{
    /// <summary>Source Generator가 전달하는 직렬화 가능한 메서드 인수 정보입니다.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class GeneratedParameterRegistration
    {
        public GeneratedParameterRegistration(
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
}
