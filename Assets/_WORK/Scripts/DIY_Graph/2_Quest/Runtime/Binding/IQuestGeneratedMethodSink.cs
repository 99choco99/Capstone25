using System.ComponentModel;

namespace UniversalGraph
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IQuestGeneratedMethodSink
    {
        void Add(QuestGeneratedMethodRegistration registration);
    }
}
