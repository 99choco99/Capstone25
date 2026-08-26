using System;

namespace UniversalGraph
{
    /// <summary>Binding 데이터에서 공통으로 사용하는 타입 식별자를 만듭니다.</summary>
    public static class MethodTypeUtility
    {
        /// <summary>타입의 전체 이름과 어셈블리 이름으로 이식 가능한 고정 식별자를 반환합니다.</summary>
        public static string GetStableTypeId(Type type)
        {
            return type == null
                ? string.Empty
                : $"{type.FullName}, {type.Assembly.GetName().Name}";
        }
    }
}
