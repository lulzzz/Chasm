using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SourceCode.Clay;

namespace SourceCode.Chasm.Repository
{
    partial class ChasmRepository // .Object
    {
        #region Read

        public abstract Task<ReadOnlyMemory<byte>?> ReadObjectAsync(Sha1 objectId, CancellationToken cancellationToken);

        public abstract Task<Stream> ReadStreamAsync(Sha1 objectId, CancellationToken cancellationToken);

        public virtual async Task<IReadOnlyDictionary<Sha1, ReadOnlyMemory<byte>>> ReadObjectBatchAsync(IEnumerable<Sha1> objectIds, CancellationToken cancellationToken)
        {
            if (objectIds == null) return ImmutableDictionary<Sha1, ReadOnlyMemory<byte>>.Empty;

            // Enumerate batches
            var dict = new Dictionary<Sha1, Task<ReadOnlyMemory<byte>?>>(Sha1Comparer.Default);
            foreach (Sha1 objectId in objectIds)
            {
                dict[objectId] = ReadObjectAsync(objectId, cancellationToken);
            }

            await Task.WhenAll(dict.Values.ToList())
                .ConfigureAwait(false);

            var dict2 = new Dictionary<Sha1, ReadOnlyMemory<byte>>(Sha1Comparer.Default);
            foreach (KeyValuePair<Sha1, Task<ReadOnlyMemory<byte>?>> task in dict)
            {
                if (task.Value == null || task.Value.Result.Value.Length == 0)
                    continue;

                dict2[task.Key] = task.Value.Result.Value;
            }

            return dict2;
        }

        #endregion

        #region Write

        public abstract Task<Sha1> WriteObjectAsync(Memory<byte> buffer, bool forceOverwrite, CancellationToken cancellationToken);

        public abstract Task<Sha1> WriteObjectAsync(Stream stream, bool forceOverwrite, CancellationToken cancellationToken);

        public abstract Task<Sha1> WriteObjectAsync(Func<Stream, ValueTask> beforeHash, bool forceOverwrite, CancellationToken cancellationToken);

        public virtual async Task WriteObjectsAsync(IEnumerable<Memory<byte>> buffers, bool forceOverwrite, CancellationToken cancellationToken)
        {
            if (buffers == null || !buffers.Any()) return;

            var tasks = new List<Task>();
            foreach (Memory<byte> buffer in buffers)
            {
                Task<Sha1> task = WriteObjectAsync(buffer, forceOverwrite, cancellationToken);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks)
                .ConfigureAwait(false);
        }

        #endregion
    }
}
