using System;
using System.Collections.Generic;
using System.Linq;
using Stitch;
using HtmlGenerator;

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

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a super container.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <param name="human_visible">Determines if the returned id should be escaped for use as a file (false) or displayed as original for human viewing (true).</param>
        /// <returns>A ready for use identifier.</returns>
        public static string GetAsideIdentifier(Read.IRead metadata, bool human_visible = false)
        {
            if (human_visible) return metadata.Identifier;
            else return metadata.EscapedIdentifier;
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a super container.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <returns>A ready for use identifier.</returns>
        public static string GetAsideLink(Read.IRead metadata, AsideType type, string AssetsFolderName, List<string> location = null, string target = "")
        {
            string class_name = GetAsideName(type);
            target = String.IsNullOrEmpty(target) ? "" : "#" + target;
            return $"<a href=\"{GetAsideRawLink(metadata, type, AssetsFolderName, location)}{target}\" class=\"info-link {class_name}-link\" target='_blank'>{GetAsideIdentifier(metadata, true)}</a>";
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a super container.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <returns>A ready for use identifier.</returns>
        public static HtmlBuilder GetAsideLinkHtml(Read.IRead metadata, AsideType type, string AssetsFolderName, List<string> location = null, string target = "")
        {
            string class_name = GetAsideName(type);
            target = String.IsNullOrEmpty(target) ? "" : "#" + target;
            var html = new HtmlBuilder();
            html.OpenAndClose(HtmlTag.a, $"href='{GetAsideRawLink(metadata, type, AssetsFolderName, location)}{target}' class='info-link {class_name}-link' target='_blank'", GetAsideIdentifier(metadata, true));
            return html;
        }

        /// <summary>To generate an identifier ready for use in the HTML page of an element in a container in a super container.</summary>
        /// <param name="metadata">The metadata for a Read or Template.</param>
        /// <returns>The link that can be used as href attribute in a link.</returns>
        public static string GetAsideRawLink(Read.IRead metadata, AsideType type, string AssetsFolderName, List<string> location = null)
        {
            if (location == null) location = new List<string>();
            string id = GetAsideIdentifier(metadata);
            string class_name = GetAsideName(type);
            return GetLinkToFolder(new List<string>() { AssetsFolderName, class_name + "s" }, location) + id.Replace(':', '-') + ".html";
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

        /// <summary> Create a warning to use in the HTML report to show users that they need to look into something. </summary>
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

        /// <summary> Generate help for the user, to provide insight in the inner workings of the program. It generates a button
        /// which can be used (hovered/selected) by the user to show the accompanying help text. </summary>
        /// <param name="title">The title for the help message.</param>
        /// <param name="content">The inner content to show, can be any valid HTML.</param>
        /// <returns>The HTMl needed for this help.</returns>
        public static string UserHelp(string title, string content)
        {
            return $"<button type='button' class='user-help'><span class='mark'>?</span><div class='content'><h3>{title}</h3><p>{content}</p></div></button>";
        }

        /// <summary> Generate a tag, eg a title, with a built in help message. </summary>
        /// <param name="title">The title for the tag, and the help message.</param>
        /// <param name="help">The help content, can be any valid HTML but will be placed in paragraph tags.</param>
        /// <param name="tag">The tag level (`h1` for `<h1>` etc).</param>
        /// <param name="classes">A list of classes to be added to this element.</param>
        /// <param name="extra">Any extra text for the tag, eg "aria-hidden='true'".</param>
        /// <returns>The HTMl needed for this help.</returns>
        public static string TagWithHelp(string tag, string title, string help, string classes = null, string extra = "")
        {
            classes = classes != null ? $" class='{classes.Trim()}'" : "";
            extra = String.IsNullOrWhiteSpace(extra) ? " " + extra.Trim() : "";
            return $"<{tag}{classes}{extra}>{title}{UserHelp(title, help)}</{tag}>";
        }

        /// <summary> Generate a Copy Data button for the user, while giving help about the data structure. It generates a button
        /// which can be used (hovered/selected) by the user to show the accompanying help text. </summary>
        /// <param name="title">The title for the help message.</param>
        /// <param name="content">The inner content to show, can be any valid HTML. (it will be enclosed in `<p>` tags)</param>
        /// <returns>The HTMl needed for this button.</returns>
        public static string CopyData(string title, string help = null)
        {
            var text = help != null ? $"<p>{help}</p>" : "";
            return $"<button type='button' class='user-help copy-data'><span class='mark'>Copy Data</span><div class='content'><h3>{title}</h3>{text}<h4>Preview</h4><pre class='example'>Loading example...</pre><i>Click on the button to copy the data to your clipboard.</i></div></button>";
        }
    }
}