using System;
using System.Collections.Generic;

namespace UniversalGraph.Editor
{
    /// <summary>특정 그래프 컨테이너의 검증 규칙을 제공하는 에디터 어셈블리가 구현합니다.</summary>
    public interface IGraphValidator
    {
        Type ContainerType { get; }
        void Validate(GraphValidationContext context, ICollection<GraphValidationIssue> issues);
    }

    /// <summary>도메인 그래프 검증기를 위한 강타입 부모 클래스입니다.</summary>
    public abstract class GraphValidator<TContainer> : IGraphValidator where TContainer : GraphContainer
    {
        public Type ContainerType => typeof(TContainer);

        /// <summary>컨테이너 타입을 확인하고 강타입 검증 구현으로 전달합니다.</summary>
        public void Validate(GraphValidationContext context, ICollection<GraphValidationIssue> issues)
        {
            Validate((TContainer)context.Container, context, issues);
        }

        /// <summary>실제 그래프 컨테이너 도메인에 필요한 검증 규칙을 구현합니다.</summary>
        protected abstract void Validate(
            TContainer container,
            GraphValidationContext context,
            ICollection<GraphValidationIssue> issues);
    }
}
