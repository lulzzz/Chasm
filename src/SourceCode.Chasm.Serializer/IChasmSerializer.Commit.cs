using System;
using System.Buffers;

namespace SourceCode.Chasm.Serializer
{
    partial interface IChasmSerializer // .Commit
    {
        IMemoryOwner<byte> Serialize(Commit model);

        Commit DeserializeCommit(ReadOnlySpan<byte> span);
    }
}
