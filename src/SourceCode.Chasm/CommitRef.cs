using System;
using System.Diagnostics;

namespace SourceCode.Chasm
{
    [DebuggerDisplay("{" + nameof(Branch) + ",nq} ({" + nameof(CommitId) + "." + nameof(Chasm.CommitId.Sha1) + ".ToString(\"D\"),nq,ac})")]
    public readonly struct CommitRef : IEquatable<CommitRef>
    {
        #region Constants

        private static readonly CommitRef s_empty;

        /// <summary>
        /// A singleton representing an empty <see cref="CommitRef"/> value.
        /// </summary>
        /// <value>
        /// The empty.
        /// </value>
        public static ref readonly CommitRef Empty => ref s_empty;

        #endregion

        #region Properties

        public string Branch { get; }

        public CommitId CommitId { get; }

        #endregion

        #region Constructors

        public CommitRef(in string branch, in CommitId commitId)
        {
            if (string.IsNullOrWhiteSpace(branch)) throw new ArgumentNullException(nameof(branch));

            Branch = branch;
            CommitId = commitId;
        }

        #endregion

        #region IEquatable

        public bool Equals(CommitRef other)
        {
            if (!CommitIdComparer.Default.Equals(CommitId, other.CommitId)) return false;
            if (!StringComparer.Ordinal.Equals(Branch, other.Branch)) return false;

            return true;
        }

        public override bool Equals(object obj)
            => obj is CommitRef other
            && Equals(other);

        public override int GetHashCode()
        {
#if !NETSTANDARD2_0
            var hc = new HashCode();

            hc.Add(CommitId, CommitIdComparer.Default);
            hc.Add(Branch ?? string.Empty, StringComparer.Ordinal);

            return hc.ToHashCode();
#else
            int hc = 11;
            unchecked
            {
                hc = hc * 7 + CommitId.GetHashCode();
                hc = hc * 7 + Branch?.GetHashCode() ?? 0;
            }
            return hc;
#endif
        }

        #endregion

        #region Operators

        public static bool operator ==(CommitRef x, CommitRef y) => x.Equals(y);

        public static bool operator !=(CommitRef x, CommitRef y) => !(x == y);

        public override string ToString() => $"{CommitId.Sha1:D} ({Branch})";

        #endregion
    }
}
