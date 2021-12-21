using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyNameSpace;

namespace HTMLNameSpace
{
    public static class CommonPieces
    {
        /// <summary>An enum to save what type of detail aside it is.</summary>
        public enum AsideType { Read, Template, RecombinedTemplate }

        public static string GetAsideName(AsideType type)
        {
            switch (type)
            {
                case AsideType.Read:
                    return "read";
                case AsideType.Template:
                    return "template";
                case AsideType.RecombinedTemplate:
                    return "recombined-template";
            }
            throw new ArgumentException("Invalid AsideType in GetAsideName.");
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <param name="humanvisible">Determines if the returned id should be escaped for use as a file (false) or displayed as original for human viewing (true).</param>
        /// <returns>A ready for use identifier.</returns>
        public static string GetAsideIdentifier(ReadMetaData.IMetaData metadata, bool humanvisible = false)
        {
            if (humanvisible) return metadata.Identifier;
            else return metadata.EscapedIdentifier;
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <returns>A ready for use identifier.</returns>
        public static string GetAsideLink(ReadMetaData.IMetaData metadata, AsideType type, string AssetsFolderName, List<string> location = null)
        {
            string classname = GetAsideName(type);
            return $"<a href=\"{GetAsideRawLink(metadata, type, AssetsFolderName, location)}\" class=\"info-link {classname}-link\">{ GetAsideIdentifier(metadata, true)}</a>";
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <returns>The link that can be used as href attribute in a link.</returns>
        public static string GetAsideRawLink(ReadMetaData.IMetaData metadata, AsideType type, string AssetsFolderName, List<string> location = null)
        {
            if (location == null) location = new List<string>();
            string id = GetAsideIdentifier(metadata);
            if (id == null) throw new Exception("ID is null");
            string classname = GetAsideName(type);
            return GetLinkToFolder(new List<string>() { AssetsFolderName, classname + "s" }, location) + id.Replace(':', '-') + ".html";
        }

        public static string GetLinkToFolder(List<string> target, List<string> location)
        {
            int i = 0;
            for (; i < target.Count && i < location.Count; i++)
            {
                if (target[i] != location[i]) break;
            }
            var pieces = new List<string>(location.Count + target.Count - 2 * i);
            pieces.AddRange(Enumerable.Repeat("..", location.Count - i));
            pieces.AddRange(target.Skip(i));
            return string.Join("/", pieces.ToArray()) + "/";
        }

        public enum CollapsibleState { Closed, Open };

        static int collapsible_counter = 0;
        /// <summary>
        /// Create a collapsible region to be used as a main tab in the report.
        /// </summary>
        /// <param name="name">The name to display.</param>
        /// <param name="content">The content.</param>
        /// <param name="state">The state of the collapsible, default closed</param>
        public static string Collapsible(string id, string name, string content, CollapsibleState state = CollapsibleState.Closed)
        {
            collapsible_counter++;
            string cid = $"collapsible-{collapsible_counter}";
            string check = state == CollapsibleState.Open ? " checked" : "";
            return $@"<input type=""checkbox"" id=""{cid}""{check}/>
<label for=""{cid}"">{name}</label>
<div class=""collapsable"" id=""{id}"">{content}</div>";
        }

        /// <summary> Create a warning to use in the HTML report to show users that they need to look into someting. </summary>
        /// <param name="title"> The title to of the warning. </param>
        /// <param name="content"> The content as raw HTML (so enclosed in &lt;p&gt; for normal text). </param>
        public static string Warning(string title, string content)
        {
            return $"<div class=\"warning\"><h3>{title}</h3>{content}</div>";
        }

        public static string TemplateHighDecoyWarning()
        {
            return CommonPieces.Warning("High decoy scores",
@"<p>The highest scoring decoy template scores at least 50% of the actual templates in template matching. Look into the Decoy segment for more details. </p>
<p> Maybe the origin of this issue is one of the following:</p>
<ul>
<li><p>A sample with very high background noise, in that case the result should be thoroughly checked by hand. </p></li>
<li><p>Incorrect segments chosen, the chosen segments should match the expected segments and species of the dataset.</p></li>
</ul>");
        }

        public static string RecombineHighDecoyWarning(string name)
        {
            return CommonPieces.Warning("High decoy scores",
@$"<p>The highest scoring recombination decoy template scores at least 50% of the actual templates in recombination in ""{name}"". </p>
<p> Maybe the origin of this issue is one of the following: </p>
<ul>
<li><p>A sample with very high background noise, in that case the result should be thoroughly checked by hand. </p></li>
<li><p>Incorrect segments chosen, the chosen segments should match the expected segments and species of the dataset.</p></li>
</ul>");
        }
    }
}