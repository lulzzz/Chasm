using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SourceCode.Clay;

namespace SourceCode.Chasm.Repository
{
    partial interface IChasmRepository // .Object
    {
        Task<bool> ExistsAsync(Sha1 objectId, ChasmRequestContext requestContext, CancellationToken cancellationToken);

        Task<IChasmBlob> ReadObjectAsync(Sha1 objectId, ChasmRequestContext requestContext, CancellationToken cancellationToken);

        Task<IChasmStream> ReadStreamAsync(Sha1 objectId, ChasmRequestContext requestContext, CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<Sha1, IChasmBlob>> ReadObjectBatchAsync(IEnumerable<Sha1> objectIds, ChasmRequestContext requestContext, CancellationToken cancellationToken);

        /// <summary>
        /// Writes a buffer to the destination, returning the content's <see cref="Sha1"/> value.
        /// </summary>
        /// <param name="buffer">The content to hash and write.</param>
        /// <param name="forceOverwrite">Forces the target to be ovwerwritten, even if it already exists.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
        Task<WriteResult<Sha1>> WriteObjectAsync(ReadOnlyMemory<byte> buffer, ChasmMetadata metadata, bool forceOverwrite, ChasmRequestContext requestContext, CancellationToken cancellationToken);

        /// <summary>
        /// Writes a stream to the destination, returning the content's <see cref="Sha1"/> value.
        /// </summary>
        /// <param name="stream">The content to hash and write.</param>
        /// <param name="forceOverwrite">Forces the target to be ovwerwritten, even if it already exists.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
        Task<WriteResult<Sha1>> WriteObjectAsync(Stream stream, ChasmMetadata metadata, bool forceOverwrite, ChasmRequestContext requestContext, CancellationToken cancellationToken);

        /// <summary>
        /// Writes a stream to the destination, returning the content's <see cref="Sha1"/> value.
        /// The <paramref name="beforeHash"/> function permits a transformation operation
        /// on the source value before calculating the hash and writing to the destination.
        /// For example, the source stream may be encoded as Json.
        /// </summary>
        /// <param name="beforeHash">An action to take on the internal stream, before calculating the hash.</param>
        /// <param name="forceOverwrite">Forces the target to be ovwerwritten, even if it already exists.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
        Task<WriteResult<Sha1>> WriteObjectAsync(Func<Stream, ValueTask> beforeHash, ChasmMetadata metadata, bool forceOverwrite, ChasmRequestContext requestContext, CancellationToken cancellationToken);

        /// <summary>
        /// Writes a list of buffers to the destination, returning the contents' <see cref="Sha1"/> values.
        /// </summary>
        /// <param name="blobs">The content to hash and write.</param>
        /// <param name="forceOverwrite">Forces the target to be ovwerwritten, even if it already exists.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
        Task<IReadOnlyList<WriteResult<Sha1>>> WriteObjectsAsync(IEnumerable<IChasmBlob> blobs, bool forceOverwrite, ChasmRequestContext requestContext, CancellationToken cancellationToken);
    }
}
