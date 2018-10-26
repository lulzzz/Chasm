using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using SourceCode.Clay.Json;

namespace SourceCode.Chasm.Serializer.Json.Wire
{
    internal static class CommitExtensions
    {
        private const string _parents = "parents";
        private const string _treeId = "treeId";
        private const string _author = "author";
        private const string _committer = "committer";
        private const string _message = "message";

        public static Commit ReadCommit(this JsonReader jr)
        {
            if (jr == null) throw new ArgumentNullException(nameof(jr));

            Audit author = default;
            Audit committer = default;
            TreeId? treeId = default;
            IReadOnlyList<CommitId> parents = null;
            string message = null;

            // Switch
            return jr.ReadObject(n =>
            {
                switch (n)
                {
                    case _parents:
                        parents = ReadParents();
                        return true;

                    case _author:
                        author = jr.ReadAudit();
                        return true;

                    case _committer:
                        committer = jr.ReadAudit();
                        return true;

                    case _message:
                        message = (string)jr.Value;
                        return true;

                    case _treeId:
                        treeId = ReadTreeId();
                        return true;
                }

                return false;
            },

            // Factory
            () => new Commit(parents, treeId, author, committer, message));

            // Property

            IReadOnlyList<CommitId> ReadParents() => jr.ReadArray(() =>
            {
                Clay.Sha1? sha1 = jr.ReadSha1();
                return sha1 == null ? default : new CommitId(sha1.Value);
            });

            TreeId? ReadTreeId()
            {
                Clay.Sha1? sha1 = jr.ReadSha1();
                return sha1 == null ? null : (TreeId?)new TreeId(sha1.Value);
            }
        }

        public static Commit ReadCommit(this string json)
        {
            if (json == null || json == JsonConstants.JsonNull) return default;

            using (var tr = new StringReader(json))
            using (var jr = new JsonTextReader(tr))
            {
                jr.DateParseHandling = DateParseHandling.None;

                Commit model = ReadCommit(jr);
                return model;
            }
        }

        public static void Write(this JsonTextWriter jw, Commit model)
        {
            if (jw == null) throw new ArgumentNullException(nameof(jw));

            if (model == default)
            {
                jw.WriteNull();
                return;
            }

            jw.WriteStartObject();
            {
                // Parents
                if (model.Parents != null && model.Parents.Count > 0)
                {
                    jw.WritePropertyName(_parents);
                    jw.WriteStartArray();

                    for (int i = 0; i < model.Parents.Count; i++)
                        jw.WriteValue(model.Parents[i].Sha1.ToString("n"));

                    jw.WriteEndArray();
                }

                // Author
                if (model.Author != default)
                {
                    jw.WritePropertyName(_author);
                    jw.Write(model.Author);
                }

                // Committer
                if (model.Committer != default)
                {
                    jw.WritePropertyName(_committer);
                    jw.Write(model.Committer);
                }

                // Message
                if (model.Message != null)
                {
                    jw.WritePropertyName(_message);
                    jw.WriteValue(model.Message);
                }

                // TreeId
                if (model.TreeId != null)
                {
                    jw.WritePropertyName(_treeId);
                    jw.WriteValue(model.TreeId.Value.ToString());
                }
            }
            jw.WriteEndObject();
        }

        public static string Write(this Commit model)
        {
            if (model == default) return JsonConstants.JsonNull;

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            using (var jw = new JsonTextWriter(sw))
            {
                Write(jw, model);
            }

            return sb.ToString();
        }
    }
}
