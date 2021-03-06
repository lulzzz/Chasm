using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SourceCode.Clay;
using crypt = System.Security.Cryptography;

namespace SourceCode.Chasm.Repository.Disk
{
    partial class DiskChasmRepo // .File
    {
        /// <summary>
        /// Writes a file to disk, returning the content's <see cref="Sha1"/> value.
        /// The <paramref name="onWrite"/> function permits a transformation operation
        /// on the source value before calculating the hash and writing to the destination.
        /// For example, the source stream may be encoded as Json.
        /// The <paramref name="afterWrite"/> function permits an operation to be
        /// performed on the file immediately after writing it. For example, the file
        /// may be uploaded to the cloud.
        /// </summary>
        /// <param name="onWrite">An action to take on the internal hashing stream.</param>
        /// <param name="afterWrite">An action to take on the file after writing has finished.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
        /// <remarks>Note that the <paramref name="onWrite"/> function should maintain the integrity
        /// of the source stream: the hash will be taken on the result of this operation.
        /// For example, transforming to Json is appropriate but compression is not since the latter
        /// is not a representative model of the original content, but rather a storage optimization.</remarks>
        public static async Task<Sha1> StageFileAsync(Func<Stream, ValueTask> onWrite, Func<Sha1, string, ValueTask> afterWrite, ChasmRequestContext requestContext = default, CancellationToken cancellationToken = default)
        {
            if (onWrite == null) throw new ArgumentNullException(nameof(onWrite));

            requestContext = ChasmRequestContext.Ensure(requestContext);

            // Note that an empty file is physically created
            var filePath = Path.GetTempFileName();

            try
            {
                Sha1 sha1;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.Read))
                using (var ct = crypt.SHA1.Create())
                {
                    using (var cs = new crypt.CryptoStream(fs, ct, crypt.CryptoStreamMode.Write))
                    {
                        await onWrite(cs)
                            .ConfigureAwait(false);
                    }
                    sha1 = new Sha1(ct.Hash);
                }

                if (afterWrite != null)
                {
                    await afterWrite(sha1, filePath)
                        .ConfigureAwait(false);
                }

                return sha1;
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        /// <summary>
        /// Writes a file to disk, returning the content's <see cref="Sha1"/> value.
        /// The <paramref name="afterWrite"/> function permits an operation to be
        /// performed on the file immediately after writing it. For example, the file
        /// may be uploaded to the cloud.
        /// </summary>
        /// <param name="stream">The content to hash and write.</param>
        /// <param name="afterWrite">An action to take on the file, after writing has finished.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
        public static Task<Sha1> WriteFileAsync(Stream stream, Func<Sha1, string, ValueTask> afterWrite, ChasmRequestContext requestContext = default, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            requestContext = ChasmRequestContext.Ensure(requestContext);

            ValueTask HashWriter(Stream output)
#if !NETSTANDARD2_0
                => new ValueTask(stream.CopyToAsync(output, cancellationToken));
#else
                => new ValueTask(stream.CopyToAsync(output, 1024, cancellationToken));
#endif

            return StageFileAsync(HashWriter, afterWrite, requestContext, cancellationToken);
        }

        /// <summary>
        /// Writes a file to disk, returning the content's <see cref="Sha1"/> value.
        /// The <paramref name="afterWrite"/> function permits an operation to be
        /// performed on the file immediately after writing it. For example, the file
        /// may be uploaded to the cloud.
        /// </summary>
        /// <param name="buffer">The content to hash and write.</param>
        /// <param name="afterWrite">An action to take on the file, after writing has finished.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
        public static Task<Sha1> WriteFileAsync(ReadOnlyMemory<byte> buffer, Func<Sha1, string, ValueTask> afterWrite, ChasmRequestContext requestContext = default, CancellationToken cancellationToken = default)
        {
            requestContext = ChasmRequestContext.Ensure(requestContext);

            ValueTask HashWriter(Stream output)
#if !NETSTANDARD2_0
                => output.WriteAsync(buffer, cancellationToken);
#else
            {
                unsafe
                {
                    // https://github.com/dotnet/corefx/pull/32669#issuecomment-429579594
                    fixed (byte* ba = &System.Runtime.InteropServices.MemoryMarshal.GetReference(buffer.Span))
                    {
                        for (int i = 0; i < buffer.Length; i++) // TODO: Perf
                            output.WriteByte(ba[i]);
                    }
                }
                return default;
            }
#endif
            return StageFileAsync(HashWriter, afterWrite, requestContext, cancellationToken);
        }

        private static async Task<byte[]> ReadFileAsync(string path, ChasmRequestContext requestContext = default, CancellationToken cancellationToken = default)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(path));

            requestContext = ChasmRequestContext.Ensure(requestContext);

            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                return default;

            if (!File.Exists(path))
                return default;

            using (FileStream fileStream = await WaitForFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, cancellationToken)
                .ConfigureAwait(false))
            {
                var bytes = await ReadBytesAsync(fileStream, requestContext, cancellationToken)
                    .ConfigureAwait(false);

                return bytes;
            }
        }

        private static async Task<byte[]> ReadBytesAsync(Stream stream, ChasmRequestContext requestContext = default, CancellationToken cancellationToken = default)
        {
            Debug.Assert(stream != null);

            requestContext = ChasmRequestContext.Ensure(requestContext);

            int offset = 0;
            int remaining = (int)stream.Length;

            byte[] bytes = new byte[remaining];
            while (remaining > 0)
            {
                int count = await stream.ReadAsync(bytes, offset, remaining, cancellationToken)
                    .ConfigureAwait(false);

                if (count == 0)
                    throw new EndOfStreamException("End of file");

                offset += count;
                remaining -= count;
            }

            return bytes;
        }

        private static async Task TouchFileAsync(string path, ChasmRequestContext requestContext = default, CancellationToken cancellationToken = default)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(path));

            if (!File.Exists(path))
                return;

            requestContext = ChasmRequestContext.Ensure(requestContext);

            for (int retryCount = 0; retryCount < _retryMax; retryCount++)
            {
                try
                {
                    File.SetLastAccessTimeUtc(path, DateTime.UtcNow);
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(_retryMs, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private static async Task<FileStream> WaitForFileAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken, int bufferSize = 4096)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(path));

            int retryCount = 0;
            while (true)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(path, mode, access, share, bufferSize);
                    return fs;
                }
                catch (IOException) when (++retryCount < _retryMax)
                {
                    fs?.Dispose();
                    await Task.Delay(_retryMs, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private static void WriteMetadata(ChasmMetadata metadata, string path)
        {
            var dto = new JsonMetadata(metadata?.ContentType, metadata?.Filename);
            var json = dto.ToJson();
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static ChasmMetadata ReadMetadata(string path)
        {
            ChasmMetadata metadata = null;

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var dto = JsonMetadata.FromJson(json);

                metadata = new ChasmMetadata(dto.ContentType, dto.Filename);
            }

            return metadata;
        }

        private static string DeriveCommitRefFileName(string name, string branch)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            if (branch == null) return name;

            string refName = Path.Combine(name, $"{branch}{CommitExtension}");
            return refName;
        }

        public static (string filePath, string metaPath) DeriveFileNames(string root, Sha1 sha1)
        {
            System.Collections.Generic.KeyValuePair<string, string> tokens = sha1.Split(PrefixLength);

            string fileName = Path.Combine(tokens.Key, tokens.Value);
            string filePath = Path.Combine(root, fileName);
            string metaPath = filePath + ".metadata";

            return (filePath, metaPath);
        }
    }
}
