using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyNameSpace;

namespace HTMLNameSpace
{
    public static class Common
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
        public static string GetAsideIdentifier(MetaData.IMetaData metadata, bool humanvisible = false)
        {
            if (humanvisible) return metadata.Identifier;
            else return metadata.EscapedIdentifier;
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a supercontainer.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <param name="humanvisible">Determines if the returned id should be escaped for use as a file (false) or displayed as original for human viewing (true).</param>
        /// <returns>A ready for use identifier.</returns>
        public static string GetAsideLink(MetaData.IMetaData metadata, AsideType type, string AssetsFolderName, List<string> location = null)
        {
            if (location == null) location = new List<string>();
            string id = GetAsideIdentifier(metadata);
            if (id == null) throw new Exception("ID is null");
            string classname = GetAsideName(type);
            string path = GetLinkToFolder(new List<string>() { AssetsFolderName, classname + "s" }, location) + id.Replace(':', '-') + ".html";
            return $"<a href=\"{path}\" class=\"info-link {classname}-link\">{ GetAsideIdentifier(metadata, true)}</a>";
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
        public static string Collapsible(string name, string content, CollapsibleState state = CollapsibleState.Closed)
        {
            collapsible_counter++;
            string id = $"collapsible-{collapsible_counter}";
            string check = state == CollapsibleState.Open ? " checked" : "";
            return $@"<input type=""checkbox"" id=""{id}""{check}/>
<label for=""{id}"">{name}</label>
<div class=""collapsable"">{content}</div>";
        }
    }
}