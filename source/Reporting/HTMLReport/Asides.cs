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
using static HTMLNameSpace.Common;

namespace HTMLNameSpace
{
    public static class HTMLAsides
    {
        /// <summary> Returns an aside for details viewing of a read. </summary>
        public static void CreateReadAside(StringBuilder buffer, (string Sequence, MetaData.IMetaData MetaData) read)
        {
            buffer.Append($@"<div id=""{GetAsideIdentifier(read.MetaData)}"" class=""info-block read-info"">
    <h1>Read {GetAsideIdentifier(read.MetaData, true)}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{read.Sequence}</p>
    <h2>Sequence Length</h2>
    <p>{read.Sequence.Length}</p>
    {read.MetaData.ToHTML()}
</div>");
        }

        /// <summary> Returns an aside for details viewing of a template. </summary>
        public static void CreateTemplateAside(StringBuilder buffer, Template template, AsideType type, string AssetsFolderName, int totalReads)
        {
            string id = GetAsideIdentifier(template.MetaData);
            string human_id = GetAsideIdentifier(template.MetaData, true);
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };

            var (consensus_sequence, consensus_doc) = template.ConsensusSequence();

            string meta = "";
            if (template.MetaData != null && type == AsideType.Template)
            {
                meta = template.MetaData.ToHTML();
            }

            string based = "";
            string title = "Segment";
            switch (type)
            {
                case AsideType.RecombinedTemplate:
                    if (template.Recombination != null)
                    {
                        var order = template.Recombination.Aggregate(
                            "",
                            (acc, seg) => acc + " → " + GetAsideLink(seg.MetaData, AsideType.Template, AssetsFolderName, location)
                        ).Substring(3);
                        based = $"<h2>Order</h2><p>{order}</p>";
                    }
                    title = "Recombined Template";
                    break;
                default:
                    break;
            }


            buffer.Append($@"<div id=""{id}"" class=""info-block template-info"">
    <h1>{title} {human_id}</h1>
    <h2>Consensus Sequence</h2>
    <p class='aside-seq'>{AminoAcid.ArrayToString(consensus_sequence)}</p>");
            CreateAnnotatedSequence(buffer, human_id, template);
            buffer.Append("<h2>Sequence Consensus Overview</h2>");

            SequenceConsensusOverview(buffer, template);
            buffer.Append("<div class='docplot'>");
            HTMLGraph.Bargraph(buffer, HTMLGraph.AnnotateDOCData(consensus_doc), "Depth of Coverage of the Consensus Sequence");
            buffer.Append($@"</div>
    <h2>Scores</h2>
    <table class='widetable'><tr>
    <th class='smallcell'>Length</th>
    <th class='smallcell'>Score</th>
    <th class='smallcell'>Matches</th>
    <th class='smallcell'>Total Area</th>
    <th class='smallcell'>Unique Score</th>
    <th class='smallcell'>Unique Matches</th>
    <th class='smallcell'>Unique Area</th>
    </tr><tr>
    <td class='center'>{template.ToString().Length}</td>
    <td class='center'>{template.Score}</td>
    <td class='center'>{template.Matches.Count()}</td>
    <td class='center'>{template.TotalArea}</td>
    <td class='center'>{template.UniqueScore}</td>
    <td class='center'>{template.UniqueMatches}</td>
    <td class='center'>{template.TotalUniqueArea}</td>
    </tr></table>
    {based}");
            var DepthOfCoverage = CreateTemplateAlignment(buffer, template, id, location, AssetsFolderName);
            CreateTemplateGraphs(buffer, template, DepthOfCoverage);
            buffer.Append($@"<h2>Template Sequence</h2>
    <p class=""aside-seq"">{AminoAcid.ArrayToString(template.Sequence)}</p>
    {meta}
</div>");
        }

        static void CreateAnnotatedSequence(StringBuilder buffer, string id, Template template)
        {
            try
            {
                // Create an overview of the alignment from consensus with germline.
                // Also highlight differences and IMGT regions

                // HERECOMESTHECONSENSUSSEQUENCE  (coloured to IMGT region)
                // HERECOMESTHEGERMLINE.SEQUENCE
                //             CONSENSUS          (differences)
                var match = template.AlignConsensusWithTemplate();
                List<(string, string)> annotated = null;
                if (template.Recombination != null)
                {
                    annotated = template.Recombination.Aggregate(new List<(string, string)>(), (acc, item) =>
                {
                    if (item.MetaData is MetaData.Fasta meta)
                        if (meta.AnnotatedSequence != null)
                            acc.AddRange(meta.AnnotatedSequence);
                    return acc;
                });
                }
                else
                {
                    if (template.MetaData is MetaData.Fasta meta)
                        if (meta.AnnotatedSequence != null)
                            annotated = meta.AnnotatedSequence;
                }

                string GetClasses(int position)
                {
                    if (annotated == null) return "";
                    int pos = -1;
                    for (int i = 0; i < annotated.Count;)
                    {
                        if (pos + annotated[i].Item2.Length >= position)
                            return annotated[i].Item1;
                        else
                        {
                            pos += annotated[i].Item2.Length;
                            i++;
                        }
                    }
                    return "";
                }

                var columns = new List<(char Template, char Query, char Difference, string Class)>();
                int template_pos = match.StartTemplatePosition;
                int query_pos = match.StartQueryPosition; // Handle overlaps (also at the end)
                foreach (var piece in match.Alignment)
                {
                    switch (piece)
                    {
                        case SequenceMatch.Match m:
                            for (int i = 0; i < m.Length; i++)
                            {
                                var t = match.TemplateSequence[template_pos].Character;
                                var q = match.QuerySequence[query_pos].Character;
                                columns.Add((t, q, t == q ? ' ' : q, GetClasses(template_pos)));
                                template_pos++;
                                query_pos++;
                            }
                            break;
                        case SequenceMatch.GapInTemplate q:
                            for (int i = 0; i < q.Length; i++)
                            {
                                var t = match.TemplateSequence[template_pos].Character;
                                columns.Add((t, '.', ' ', GetClasses(template_pos)));
                                template_pos++;
                            }
                            break;
                        case SequenceMatch.GapInQuery t:
                            for (int i = 0; i < t.Length; i++)
                            {
                                var q = match.QuerySequence[query_pos].Character;
                                columns.Add(('.', q, q, GetClasses(template_pos)));
                                query_pos++;
                            }
                            break;
                    }
                }

                buffer.Append("<h2>Annotated consensus sequence</h2><div class='annotated'><div class='names'><span>Consensus</span><span>Germline</span></div>");

                var present = new HashSet<string>();
                foreach (var column in columns)
                {
                    if (column.Template == 'X' && (column.Query == '.' || column.Query == 'X')) continue;
                    var title = "";
                    if (column.Class.StartsWith("CDR"))
                        if (!present.Contains(column.Class))
                        {
                            present.Add(column.Class);
                            title = $"<span class='title'>{column.Class}</span>";
                        }
                    buffer.Append($"<div class='{column.Class}'>{title}<span>{column.Query}</span><span>{column.Template}</span><span class='dif'>{column.Difference}</span></div>");
                }
                buffer.Append("</div><div class='annotated legend'><p class='names'>Legend</p><span class='CDR'>CDR</span><span class='Conserved'>Conserved</span><span class='Glycosylationsite'>Possible glycosylation site</span></div>");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not find regions in consensus sequence for {id}\n{e.Message}");
            }
        }

        static void CreateTemplateGraphs(StringBuilder buffer, Template template, List<double> DepthOfCoverage)
        {
            if (template.Matches.Count == 0) return;
            buffer.Append("<h3>Graphs</h3><div class='template-graphs'><div class='docplot'>");
            HTMLGraph.Bargraph(buffer, HTMLGraph.AnnotateDOCData(DepthOfCoverage), "Depth of Coverage (including gaps)");
            buffer.Append("</div><div class='docplot'>");
            HTMLGraph.Bargraph(buffer, HTMLGraph.AnnotateDOCData(DepthOfCoverage.Select(a => a == 0 ? 0 : Math.Log10(a)).ToList()), "Log10 Depth of Coverage (including gaps)");
            buffer.Append("</div>");

            if (template.ForcedOnSingleTemplate && template.UniqueMatches > 0)
            {
                // Histogram of Scores
                buffer.Append("<div>");
                HTMLGraph.GroupedHistogram(buffer, new List<(List<double>, string)> { (template.Matches.Select(a => (double)a.Score).ToList(), "Normal"), (template.Matches.FindAll(a => a.Unique).Select(a => (double)a.Score).ToList(), "Unique") }, "Score Distribution");

                // Histogram of Length On Template
                buffer.Append("</div><div>");
                HTMLGraph.GroupedHistogram(buffer, new List<(List<double>, string)> { (template.Matches.Select(a => (double)a.LengthOnTemplate).ToList(), "Normal"), (template.Matches.FindAll(a => a.Unique).Select(a => (double)a.LengthOnTemplate).ToList(), "Unique") }, "Length on Template Distribution");
            }
            else
            {
                // Histogram of Scores
                buffer.Append("<div>");
                HTMLGraph.Histogram(buffer, template.Matches.Select(a => (double)a.Score).ToList(), "Score Distribution");

                // Histogram of Length On Template
                buffer.Append("</div><div>");
                HTMLGraph.Histogram(buffer, template.Matches.Select(a => (double)a.LengthOnTemplate).ToList(), "Length on Template Distribution");
            }

            // Histogram of coverage, coverage per position excluding gaps
            buffer.Append("</div><div>");
            HTMLGraph.Histogram(buffer, template.CombinedSequence().Select(a => a.AminoAcids.Values.Sum()).ToList(), "Coverage Distribution");

            buffer.Append("<i>Excludes gaps in reference to the template sequence</i></div></div>");
        }

        static public List<double> CreateTemplateAlignment(StringBuilder buffer, Template template, string id, List<string> location, string AssetsFolderName)
        {
            var alignedSequences = template.AlignedSequences();

            if (alignedSequences.Count == 0)
                return new List<double>();

            buffer.Append("<h2>Alignment</h2>");

            // Loop over aligned
            // For each position: (creates List<string[]>, per position, per sequence + templatesequence)
            // Convert AA to string (fill in with gapchar)
            // Convert Gap to string (get max length, align all gaps, fill in with spaces)

            // Convert to lines: (creates List<string>)
            // Combine horizontally
            var totalsequences = alignedSequences[0].Sequences.Length;
            var lines = new List<(string Sequence, int Index, int SequencePosition, AsideType Type)>[totalsequences + 1];
            const char gapchar = '-';
            const char nonbreakingspace = '\u00A0';
            var depthOfCoverage = new List<double>();

            for (int i = 0; i < totalsequences + 1; i++)
            {
                lines[i] = new List<(string Sequence, int Index, int SequencePosition, AsideType Type)>();
            }

            for (int template_pos = 0; template_pos < alignedSequences.Count; template_pos++)
            {
                var (Sequences, Gaps) = alignedSequences[template_pos];
                lines[0].Add((template.Sequence[template_pos].ToString(), -1, -1, AsideType.Read));
                double depth = 0;

                // Add the aligned amino acid
                for (int i = 0; i < Sequences.Length; i++)
                {
                    int index = Sequences[i].SequencePosition;
                    depth += Sequences[i].CoverageDepth;

                    if (index == -1)
                    {
                        lines[i + 1].Add((gapchar.ToString(), -1, -1, AsideType.Read));
                    }
                    else if (index == 0)
                    {
                        lines[i + 1].Add((nonbreakingspace.ToString(), -1, -1, AsideType.Read));
                    }
                    else
                    {
                        var type = AsideType.Read;
                        var idx = Sequences[i].MatchIndex;

                        lines[i + 1].Add((template.Matches[Sequences[i].MatchIndex].QuerySequence[index - 1].ToString(), idx, index - 1, type));
                    }
                }

                depthOfCoverage.Add(depth);

                // Add the gap
                // TODO: Unaligned for now: the gaps are not aligned to each other, this could make it more good looking
                int max_length = 0;
                // Get the max length of the gaps 
                for (int i = 0; i < Gaps.Length; i++)
                {
                    if (Gaps[i].Gap != null && Gaps[i].Gap.ToString().Length > max_length)
                    {
                        max_length = Gaps[i].Gap.ToString().Length;
                    }
                }
                // Add gap to the template
                lines[0].Add((new string(gapchar, max_length), -1, -1, AsideType.Read));

                var depthGap = new List<double[]>();
                // Add gap to the lines
                for (int i = 0; i < Gaps.Length; i++)
                {
                    string seq;
                    if (Gaps[i].Gap == null)
                    {
                        seq = "";
                        depthGap.Add(Enumerable.Repeat(0.0, max_length).ToArray());
                    }
                    else
                    {
                        seq = Gaps[i].Gap.ToString();
                        var d = new double[max_length];
                        Gaps[i].CoverageDepth.CopyTo(d, max_length - Gaps[i].CoverageDepth.Length);
                        depthGap.Add(d);
                    }

                    char padchar = nonbreakingspace;
                    if (Gaps[i].InSequence) padchar = gapchar;

                    var index = Gaps[i].ContigID == -1 ? -1 : Gaps[i].MatchIndex;

                    var type = AsideType.Read;
                    var idx = index;

                    lines[i + 1].Add((seq.PadRight(max_length, padchar), idx, Sequences[i].SequencePosition - 1, type));
                }
                var depthGapCombined = new double[max_length];
                foreach (var d in depthGap)
                {
                    depthGapCombined = depthGapCombined.ElementwiseAdd(d);
                }
                depthOfCoverage.AddRange(depthGapCombined.Select(a => (double)a));
            }

            var aligned = new string[alignedSequences[0].Sequences.Length + 1];
            var types = new List<AsideType>[alignedSequences[0].Sequences.Length + 1];

            for (int i = 0; i < alignedSequences[0].Sequences.Length + 1; i++)
            {
                StringBuilder sb = new();
                var typ = new List<AsideType>();

                foreach ((var text, _, _, var type) in lines[i])
                {
                    sb.Append(text);
                    typ.AddRange(Enumerable.Repeat(type, text.Length));
                }

                types[i] = typ;
                aligned[i] = sb.ToString();
            }

            buffer.AppendLine($"<div class=\"reads-alignment\" style=\"--max-value:{depthOfCoverage.Max()}\">");

            // Create the front overhanging reads block
            var frontoverhangbuffer = new StringBuilder();
            bool frontoverhang = false;
            frontoverhangbuffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"front-overhang-toggle-{id}\"/><label for=\"front-overhang-toggle-{id}\">");
            frontoverhangbuffer.AppendFormat("<div class='align-block overhang-block front-overhang'><p><span class='front-overhang-spacing'></span>");

            for (int i = 1; i < aligned.Length; i++)
            {
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition != 0 && match.StartTemplatePosition == 0)
                {
                    frontoverhang = true;
                    frontoverhangbuffer.Append($"<a href=\"#\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(0, match.StartQueryPosition))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    frontoverhangbuffer.Append("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }

            if (frontoverhang)
            {
                buffer.Append(frontoverhangbuffer.ToString().TrimEnd("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>"));
                buffer.AppendLine($"</p></div></label></div>");
            }

            // Chop it up, add numbers etc
            const int blocklength = 5;

            if (aligned.Length > 0)
            {
                int alignedindex = 0;
                int alignedlength = 0;
                for (int block = 0; block * blocklength < aligned[0].Length; block++)
                {
                    // Get the right id's to generate the right links
                    while (alignedlength < block * blocklength && alignedindex + 1 < lines[0].Count)
                    {
                        alignedlength += lines[0][alignedindex].Sequence.Length;
                        alignedindex++;
                    }

                    var positions = new List<(int index, int position, int length)>[aligned.Length];

                    for (int i = 1; i < aligned.Length; i++)
                    {
                        int index = lines[i][alignedindex].Index;
                        int position = lines[i][alignedindex].SequencePosition;
                        int additionallength = 0;
                        int additionalindex = 1;
                        positions[i] = new List<(int index, int position, int length)>();

                        while (alignedlength + additionallength < (block + 1) * blocklength && alignedindex + additionalindex < lines[0].Count)
                        {
                            int thisindex = lines[i][alignedindex + additionalindex].Index;
                            int thisposition = lines[i][alignedindex + additionalindex].SequencePosition;

                            if (index == -1)
                            {
                                index = thisindex;
                                position = thisposition;
                            }
                            else if (thisindex != -1 && thisindex != index)
                            {
                                positions[i].Add((index, position, additionallength));
                                index = thisindex;
                                position = thisposition;
                                break;
                            }

                            additionallength += lines[0][alignedindex + additionalindex].Sequence.Length;
                            additionalindex++;
                        }

                        if (index >= 0)
                            positions[i].Add((index, position, blocklength));
                    }


                    // Add the sequence and the number to tell the position
                    string number = "";
                    if (aligned[0].Length - block * blocklength >= blocklength)
                    {
                        number = ((block + 1) * blocklength).ToString();
                        number = string.Concat(Enumerable.Repeat("&nbsp;", blocklength - number.Length)) + number;
                    }
                    buffer.Append($"<div class='align-block'><p><span class=\"number\">{number}</span><br><span class=\"seq\">{aligned[0].Substring(block * blocklength, Math.Min(blocklength, aligned[0].Length - block * blocklength))}</span><br>");

                    StringBuilder alignblock = new();
                    for (int i = 1; i < aligned.Length; i++)
                    {
                        string result = "";
                        if (positions[i].Count > 0)
                        {
                            alignblock.Append("<span class=\"align-link\">");
                            int offset = 0;
                            foreach (var piece in positions[i])
                            {
                                var rid = "none";
                                var name = GetAsideName(AsideType.Read);
                                var unique = "";
                                try
                                {
                                    var meta = template.Matches[piece.index].MetaData;
                                    if (template.Matches[piece.index].Unique) unique = " unique";
                                    rid = meta.EscapedIdentifier;
                                }
                                catch { }
                                string path = GetLinkToFolder(new List<string>() { AssetsFolderName, name + "s" }, location) + rid.Replace(':', '-') + ".html?pos=" + piece.position;
                                if (aligned[i].Length > block * blocklength + offset)
                                {
                                    // Get the block of sequence for this piece, determine if there are leading or trailing spaces and add empty text for those
                                    var seq = aligned[i].Substring(block * blocklength + offset, Math.Max(Math.Min(Math.Min(piece.length, aligned[i].Length - block * blocklength - offset), blocklength - offset), 0));
                                    var length = seq.Length;
                                    seq = seq.TrimStart(nonbreakingspace);
                                    if (length > seq.Length)
                                        alignblock.Append(string.Concat(Enumerable.Repeat("&nbsp;", length - seq.Length)));
                                    length = seq.Length;
                                    seq = seq.TrimEnd(nonbreakingspace);

                                    alignblock.Append($"<a href=\"{path}\" class=\"align-link{unique}\" onmouseover=\"AlignmentDetails({template.Matches[piece.index].Index})\" onmouseout=\"AlignmentDetailsClear()\">{seq}</a>");

                                    if (length > seq.Length)
                                        alignblock.Append(string.Concat(Enumerable.Repeat("&nbsp;", length - seq.Length)));
                                }
                                offset = piece.length;
                            }
                            alignblock.Append("</span>");
                        }
                        alignblock.Append(result);
                        alignblock.Append("<br>");
                    }
                    buffer.Append(alignblock.ToString().TrimEnd("<br>"));
                    buffer.AppendLine("</p><div class='coverage-depth-wrapper'>");

                    for (int i = block * blocklength; i < block * blocklength + Math.Min(blocklength, depthOfCoverage.Count - block * blocklength); i++)
                    {
                        buffer.Append($"<span class='coverage-depth-bar' style='--value:{depthOfCoverage[i]}'></span>");
                    }
                    buffer.Append("</div></div>");
                }
            }

            // Create the end overhanging reads block
            var endoverhangbuffer = new StringBuilder();
            bool endoverhang = false;
            endoverhangbuffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"end-overhang-toggle-{id}\"/><label for=\"end-overhang-toggle-{id}\">");
            endoverhangbuffer.AppendFormat("<div class='align-block overhang-block end-overhang'><p><span class='end-overhang-spacing'></span>");
            for (int i = 1; i < aligned.Length; i++)
            {
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition + match.TotalMatches < match.QuerySequence.Length && match.StartTemplatePosition + match.TotalMatches == match.TemplateSequence.Length)
                {
                    endoverhang = true;
                    endoverhangbuffer.Append($"<a href=\"#\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(match.StartQueryPosition + match.TotalMatches, match.QuerySequence.Length - match.StartQueryPosition - match.TotalMatches))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    endoverhangbuffer.Append("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }
            if (endoverhang)
            {
                buffer.Append(endoverhangbuffer.ToString().TrimEnd("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>"));
                buffer.AppendLine($"</p></div></label></div>");
            }

            // Index menus
            buffer.Append("<div id='index-menus'>");
            foreach (var match in template.Matches)
            {
                AlignmentDetails(buffer, match, template);
            }

            buffer.AppendLine("</div></div>");
            return depthOfCoverage;
        }

        static void AlignmentDetails(StringBuilder buffer, SequenceMatch match, Template template)
        {
            var doctitle = "Positional Score";
            var type = "Read";

            buffer.Append($@"
    <div class='alignment-details' id='alignment-details-{match.Index}'>
        <h4>{match.MetaData.Identifier}</h4>
        <table>
            <tr>
                <td>Type</td>
                <td>{type}</td>
            </tr>
            <tr>
                <td>Score</td>
                <td>{match.Score}</td>
            </tr>
            <tr>
                <td>Total Area</td>
                <td>{match.MetaData.TotalArea:G4}</td>
            </tr>
            <tr>
                <td>Length on Template</td>
                <td>{match.LengthOnTemplate}</td>
            </tr>
            <tr>
                <td>Position on Template</td>
                <td>{match.StartTemplatePosition}</td>
            </tr>
            <tr>
                <td>Start on {type}</td>
                <td>{match.StartQueryPosition}</td>
            </tr>
            <tr>
                <td>Length of {type}</td>
                <td>{match.QuerySequence.Length}</td>
            </tr>");
            if (template.ForcedOnSingleTemplate)
            {
                buffer.Append("<tr><td>Unique</td><td>");
                if (match.Unique) buffer.Append("Yes");
                else buffer.Append("No");
                buffer.Append("</td></tr>");
            }
            if (match.MetaData is MetaData.Peaks p)
            {
                buffer.Append($"<tr><td>Peaks ALC</td><td>{p.DeNovoScore}</td></tr>");
            }
            if (match.MetaData.PositionalScore.Length != 0)
            {
                buffer.Append($"<tr><td>{doctitle}</td><td class='docplot'>");
                HTMLGraph.Bargraph(buffer, HTMLGraph.AnnotateDOCData(match.MetaData.PositionalScore.SubArray(match.StartQueryPosition, match.TotalMatches).Select(a => (double)a).ToList(), match.StartQueryPosition));
                buffer.Append("</td></tr>");
            }

            buffer.Append($@"<tr><td>Alignment graphic</td><td>");
            SequenceMatchGraphic(buffer, match);
            buffer.Append("</td></tr></table></div>");
        }

        static void SequenceMatchGraphic(StringBuilder buffer, SequenceMatch match)
        {
            var id = "none";
            foreach (var piece in match.Alignment)
            {
                if (piece is SequenceMatch.Match)
                    id = "match";
                else if (piece is SequenceMatch.GapInTemplate)
                    id = "gap-in-template";
                else if (piece is SequenceMatch.GapInQuery)
                    id = "gap-in-query";

                buffer.Append($"<span class='{id}' style='flex-grow:{piece.Length}'></span>");
            }
            buffer.Append("</div>");
        }

        static void SequenceConsensusOverview(StringBuilder buffer, Template template)
        {
            var consensus_sequence = template.CombinedSequence();
            var diversity = new List<Dictionary<string, double>>(consensus_sequence.Count * 2);

            for (int i = 0; i < consensus_sequence.Count; i++)
            {
                var items = new Dictionary<string, double>();
                foreach (var item in consensus_sequence[i].AminoAcids)
                {
                    items.Add(item.Key.Character.ToString(), item.Value);
                }
                diversity.Add(items);
                var gaps = new Dictionary<string, double>();
                foreach (var item in consensus_sequence[i].Gaps)
                {
                    if (item.Key == (Template.IGap)new Template.None())
                    {
                        gaps.Add("~", item.Value.Count);
                    }
                    else
                    {
                        gaps.Add(item.Key.ToString(), item.Value.Count);
                    }
                }
            }
            HTMLTables.SequenceConsensusOverview(buffer, diversity);
        }
    }
}