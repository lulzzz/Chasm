using System;
using System.Buffers;
using SourceCode.Chasm.Serializer.Proto.Wire;

namespace SourceCode.Chasm.Serializer.Proto
{
    partial class ProtoChasmSerializer // .CommitId
    {
        public IMemoryOwner<byte> Serialize(CommitId model)
        {
            CommitIdWire wire = model.Convert();

            IMemoryOwner<byte> slice = SerializeImpl(wire);
            return slice;
        }

        public CommitId DeserializeCommitId(ReadOnlySpan<byte> span)
        {
            if (span.Length == 0) return default;

            var wire = new CommitIdWire();
            DeserializeImpl(span, ref wire);

            CommitId? model = wire.Convert();
            return model.Value;
        }
    }
}
