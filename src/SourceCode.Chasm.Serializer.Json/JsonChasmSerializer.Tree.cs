using System;
using System.Buffers;
using System.Text;
using SourceCode.Chasm.Serializer.Json.Wire;
using SourceCode.Clay.Buffers;

namespace SourceCode.Chasm.Serializer.Json
{
    partial class JsonChasmSerializer // .Tree
    {
        public IMemoryOwner<byte> Serialize(TreeNodeMap model)
        {
            string json = model.Write();
            int length = Encoding.UTF8.GetMaxByteCount(json.Length); // Utf8 is 1-4 bpc

            IMemoryOwner<byte> rented = _pool.Rent(length);
            length = Encoding.UTF8.GetBytes(json, rented.Memory.Span);

            IMemoryOwner<byte> slice = rented.WrapSlice(0, length);
            return slice;
        }

        public TreeNodeMap DeserializeTree(ReadOnlySpan<byte> span)
        {
            if (span.Length == 0) throw new ArgumentNullException(nameof(span));

            string json = Encoding.UTF8.GetString(span);

            TreeNodeMap model = json.ReadTreeNodeMap();
            return model;
        }
    }
}
