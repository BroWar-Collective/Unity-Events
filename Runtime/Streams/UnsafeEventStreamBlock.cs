using System.Diagnostics.CodeAnalysis;
using Unity.Collections;

namespace BroWar.Events.Streams
{
    [GenerateTestsForBurstCompatibility]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Convenience")]
    internal unsafe struct UnsafeEventStreamBlock
    {
        internal UnsafeEventStreamBlock* Next;
        internal fixed byte Data[1];
    }
}
