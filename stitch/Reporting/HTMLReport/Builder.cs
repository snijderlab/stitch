using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel;
using System.Reflection;
using AssemblyNameSpace;
using static HTMLNameSpace.CommonPieces;
using static AssemblyNameSpace.HelperFunctionality;
using System.Collections.ObjectModel;

namespace HTMLNameSpace
{
    public class HTMLBuilder
    {
        private StringBuilder buffer;
        private List<string> open_tags = new List<string>();

        /// <summary>
        /// Create a new HTMLBuilder, possibly building on top of the given buffer.
        /// </summary>
        /// <param name="builder"></param>
        public HTMLBuilder(StringBuilder builder = null)
        {
            if (builder == null)
            {
                buffer = new StringBuilder();
            }
            else
            {
                buffer = builder;
            }
        }

        /// <summary>
        /// Create a empty tag eg "<input type='text'/>".
        /// </summary>
        /// <param name="tag">The tag eg "input"</param>
        /// <param name="heading">The extra headings eg "type='text'" (leading space will be added automatically)</param>
        public void Empty(string tag, string heading = "")
        {
            heading = String.IsNullOrWhiteSpace(heading) ? "" : " " + heading; //Add leading space
            buffer.Append($"<{tag}{heading}/>");
        }

        /// <summary>
        /// Open a tag eg "<div class='stuff'>".
        /// </summary>
        /// <param name="tag">The tag eg "div"</param>
        /// <param name="heading">The extra headings eg "class='stuff'" (leading space will be added automatically)</param>
        public void Open(string tag, string heading = "")
        {
            heading = String.IsNullOrWhiteSpace(heading) ? "" : " " + heading; //Add leading space
            buffer.Append($"<{tag}{heading}>");
            open_tags.Add(tag);
        }

        /// <summary>
        /// Open a tag eg "<p class='stuff'>Text</p>".
        /// </summary>
        /// <param name="tag">The tag eg "p"</param>
        /// <param name="heading">The extra headings eg "class='stuff'" (leading space will be added automatically)</param>
        /// <param name="content">The inner content eg "Text" (It will be HTML encoded)</param>
        public void OpenAndClose(string tag, string heading, string content)
        {
            heading = String.IsNullOrWhiteSpace(heading) ? "" : " " + heading; //Add leading space
            buffer.Append($"<{tag}{heading}>{System.Web.HttpUtility.HtmlEncode(content)}</{tag}>");
        }

        /// <summary>
        /// Close the specified tag. If there are no open tags, or this is not the tag to be closed this will raise an exception.
        /// </summary>
        /// <param name="tag">The tag to close eg "div"</param>
        public void Close(string tag)
        {
            if (open_tags.Count == 0)
            {
                throw new InvalidOperationException("Tried to close a tag while no tags are opened. Please report this error.");
            }
            else if (open_tags.Last() != tag)
            {
                throw new InvalidOperationException("Tried to close the wrong tag in HTML generation. Please report this error.");
            }
            else
            {
                buffer.Append($"</{tag}>");
                open_tags.RemoveAt(open_tags.Count - 1);
            }
        }

        /// <summary>
        /// Assert the open tags. This can be used for debugging and testing. It will raise an exception when the open tags are not equal to the given tags.
        /// </summary>
        /// <param name="tags">The tags that are expected to be open.</param>
        public void AssertOpen(string[] tags)
        {
            var open_tags_s = open_tags.Aggregate("", (acc, i) => acc + $"<{i}> ");
            var tags_s = tags.Aggregate("", (acc, i) => acc + $"<{i}> ");
            if (open_tags.Count != tags.Count())
            {
                throw new InvalidDataException($"The number of asserted tags is not equal. The HTML was asserted to have the following open tags {tags_s} but had the following open tags {open_tags_s}.");
            }
            for (int i = 0; i < open_tags.Count; i++)
            {
                if (open_tags[i] != tags[i])
                {
                    throw new InvalidDataException($"The HTML was asserted to have the following open tags {tags_s} but had the following open tags {open_tags_s}. The wrong tag is at index {i}.");
                }
            }
        }

        /// <summary>
        /// Close all remaining tags. The usage of this method should be avoided. It is better to close all tags in the known order,
        /// because any bugs would surface if any tags would be missed.
        /// </summary>
        public void CloseAll()
        {
            open_tags.Reverse();
            foreach (var tag in open_tags)
            {
                buffer.Append($"</{tag}>");
            }
            open_tags.Clear();
        }

        /// <summary>
        /// Add content to the HTML.
        /// </summary>
        /// <param name="content">The content to add, this will be HTML encoded.</param>
        public void Content(string content)
        {
            buffer.Append(System.Web.HttpUtility.HtmlEncode(content));
        }

        /// <summary>
        /// !!UNSAFE!! Add content to the HTML, while NOT encoding it for safe use in HTML. You should feel bad about using this.
        /// </summary>
        /// <param name="content">The content to add.</param>
        public void UnsafeContent(string content)
        {
            buffer.Append(content);
        }

        /// <summary>
        /// Add the contents of another HTML builder to this HTML builder. This will raise an exception if some tags are still open.
        /// </summary>
        /// <param name="html"></param>
        public void Add(HTMLBuilder html)
        {
            if (html.open_tags.Count > 0)
            {
                throw new InvalidOperationException("Tried to get add another HTML that has unclosed tags to this HTML. Please report this error.");
            }
            else
            {
                buffer.Append(html.ToString());
            }
        }

        /// <summary>
        /// Remove the specified number of elements from the end of this HTML. It will raise an exception if this number of elements was not found.
        /// Note by using this you could remove tags that where open and in that way invalidate the information stored in this class. For safe usage
        /// callers must make sure that no open tags are removed by calling this function.
        /// </summary>
        /// <param name="number">The number of elements to remove.</param>
        public void UnsafeRemoveElementsFromEnd(uint number)
        {
            if (number == 0) return;
            var content = buffer.ToString();
            var reverse = content.Reverse();
            var found = 0;
            var index = -1;
            var backslash = false;
            var i = 0;
            foreach (char c in reverse)
            {
                if (c == '<' && !backslash)
                {
                    found += 1;
                    index = i;
                    backslash = false;
                    if (found == number)
                    {
                        break;
                    }
                }
                else if (c == '/')
                {
                    backslash = true;
                }
                else
                {
                    backslash = false;
                }
                i++;
            }
            if (found != number)
            {
                throw new InvalidOperationException($"Tried to remove {number} elements from the HTML, but could not find so many. Please report this error.");
            }
            else
            {
                buffer.Clear();
                buffer.Append(content.Substring(0, content.Length - index - 1));
            }
        }

        /// <summary>
        /// Get the HTML code as a string. This will raise an exception if some tags are still open.
        /// </summary>
        /// <returns>The HTML code as a string.</returns>
        public override string ToString()
        {
            if (open_tags.Count > 0)
            {
#pragma warning disable CA1065 // The exception here is needed to fulfil the guarantees of this class.
                throw new InvalidOperationException("Tried to get the HTML string while some tags where not properly closed. Please report this error.");
#pragma warning restore CA1065
            }
            else
            {
                return buffer.ToString();
            }
        }
    }
}