using System.ComponentModel;

namespace UniversalGraph
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IQuestGeneratedMethodProvider
    {
        void Collect(IQuestGeneratedMethodSink sink);
    }
}
