using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using SourceCode.Clay;
using SourceCode.Clay.Json;

namespace SourceCode.Chasm.Serializer.Json.Wire
{
    internal static class TreeNodeExtensions
    {
        private const string _name = "name";
        private const string _kind = "kind";
        private const string _nodeId = "nodeId";

        public static TreeNode ReadTreeNode(this JsonReader jr)
        {
            Debug.Assert(jr != null);

            string name = default;
            NodeKind kind = default;
            Sha1 sha1 = default;

            jr.ReadObject(n =>
            {
                switch (n)
                {
                    case _name:
                        name = (string)jr.Value;
                        return true;

                    case _kind:
                        kind = jr.ParseEnum<NodeKind>(true) ?? default;
                        return true;

                    case _nodeId:
                        sha1 = jr.ReadSha1() ?? default;
                        return true;
                }

                return false;
            });

            return (name == null && kind == default && sha1 == default) ? default : new TreeNode(name, kind, sha1);
        }

        public static TreeNode ReadTreeNode(this string json)
        {
            if (json == null || json == JsonConstants.JsonNull) return default;

            using (var tr = new StringReader(json))
            using (var jr = new JsonTextReader(tr))
            {
                jr.DateParseHandling = DateParseHandling.None;

                TreeNode model = ReadTreeNode(jr);
                return model;
            }
        }

        public static void Write(this JsonWriter jw, TreeNode model)
        {
            Debug.Assert(jw != null);

            if (model == default)
            {
                jw.WriteNull();
                return;
            }

            jw.WriteStartObject();
            {
                // Name
                jw.WritePropertyName(_name);
                jw.WriteValue(model.Name);

                // Kind
                jw.WritePropertyName(_kind);
                jw.WriteValue(model.Kind.ToString());

                // NodeId
                jw.WritePropertyName(_nodeId);
                jw.WriteValue(model.Sha1.ToString("n"));
            }
            jw.WriteEndObject();
        }

        public static string Write(this TreeNode model)
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
