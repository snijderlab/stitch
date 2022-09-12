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
    public static class HTMLAsides
    {
        /// <summary> Returns an aside for details viewing of a read. </summary>
        public static void CreateReadAside(StringBuilder buffer, (string Sequence, ReadMetaData.IMetaData MetaData) read, ReadOnlyCollection<(string, List<Segment>)> segments, ReadOnlyCollection<Segment> recombined, string AssetsFolderName)
        {
            buffer.Append($@"<div id=""{GetAsideIdentifier(read.MetaData)}"" class=""info-block read-info"">
    <h1>Read {GetAsideIdentifier(read.MetaData, true)}</h1>
    <h2>Sequence</h2>
    <p class=""aside-seq"">{read.Sequence}</p>
    <h2>Sequence Length</h2>
    <p>{read.Sequence.Length}</p>
    ");
            buffer.Append(CommonPieces.TagWithHelp("h2", "Reverse Lookup", HTMLHelp.ReadLookup));
            buffer.Append("<table class='widetable'><tr><th>Group</th><th>Segment</th><th>Template</th><th>Location</th><th>Score</th><th>Unique</th></tr>");
            foreach (var group in segments)
            {
                foreach (var segment in group.Item2)
                {
                    foreach (var template in segment.Templates)
                    {
                        foreach (var match in template.Matches.ToList())
                        {
                            if (match.MetaData.Identifier == read.MetaData.Identifier)
                            {
                                buffer.Append(
$@"<tr>
    <td class='center'>{group.Item1}</td>
    <td class='center'>{segment.Name}</td>
    <td class='center'>{GetAsideLink(template.MetaData, AsideType.Template, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(read.MetaData))}</td>
    <td class='center'>{match.StartTemplatePosition}</td>
    <td class='center'>{match.Score}</td>
    <td class='center'>{match.Unique}</td>
</tr>"
                                );
                            }
                        }
                    }
                }
            }
            if (recombined != null)
            {
                buffer.Append("</table><table class='widetable'><tr><th>Recombined</th><th>Location</th><th>Score</th><th>Unique</th></tr>");
                foreach (var segment in recombined)
                {
                    foreach (var template in segment.Templates)
                    {
                        foreach (var match in template.Matches)
                        {
                            if (match.MetaData.Identifier == read.MetaData.Identifier)
                            {
                                buffer.Append(
$@"<tr>
    <td class='center'>{GetAsideLink(template.MetaData, AsideType.RecombinedTemplate, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(read.MetaData))}</td>
    <td class='center'>{match.StartTemplatePosition}</td>
    <td class='center'>{match.Score}</td>
    <td class='center'>{match.Unique}</td>
</tr>"
                                );
                            }
                        }
                    }
                }
            }

            buffer.Append($"</table>{read.MetaData.ToHTML()}</div>");
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
    {CommonPieces.TagWithHelp("h2", "Consensus Sequence", HTMLHelp.ConsensusSequence)}
    <p class='aside-seq'>{AminoAcid.ArrayToString(consensus_sequence)}</p>");
            CreateAnnotatedSequence(buffer, human_id, template);

            SequenceConsensusOverview(buffer, template, "Sequence Consensus Overview", HTMLHelp.SequenceConsensusOverview);
            buffer.Append("<div class='docplot'>");
            HTMLGraph.Bargraph(buffer, HTMLGraph.AnnotateDOCData(consensus_doc), "Depth of Coverage of the Consensus Sequence", HTMLHelp.DOCGraph, 10, template.ConsensusSequenceAnnotation());
            buffer.Append($@"</div>
    <h2>Scores</h2>
    <table class='widetable'><tr>
        {CommonPieces.TagWithHelp("th", "Length", HTMLHelp.TemplateLength, "smallcell")}
        {CommonPieces.TagWithHelp("th", "Score", HTMLHelp.TemplateScore, "smallcell")}
        {CommonPieces.TagWithHelp("th", "Matches", HTMLHelp.TemplateMatches, "smallcell")}
        {CommonPieces.TagWithHelp("th", "Total Area", HTMLHelp.TemplateTotalArea, "smallcell")}
        {CommonPieces.TagWithHelp("th", "Unique Score", HTMLHelp.TemplateUniqueScore, "smallcell")}
        {CommonPieces.TagWithHelp("th", "Unique Matches", HTMLHelp.TemplateUniqueMatches, "smallcell")}
        {CommonPieces.TagWithHelp("th", "Unique Area", HTMLHelp.TemplateUniqueArea, "smallcell")}
    </tr><tr>
        <td class='center'>{template.Sequence.Length}</td>
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
            buffer.Append($@"{CommonPieces.TagWithHelp("h2", "Template Sequence", template.Recombination != null ? HTMLHelp.RecombinedSequence : HTMLHelp.TemplateSequence)}
    <p class=""aside-seq"">{AminoAcid.ArrayToString(template.Sequence)}</p>
    {meta}
</div>");
        }

        static void CreateAnnotatedSequence(StringBuilder buffer, string id, Template template)
        {
            // Create an overview of the alignment from consensus with germline.
            // Also highlight differences and IMGT regions

            // HERECOMESTHECONSENSUSSEQUENCE  (coloured to IMGT region)
            // HERECOMESTHEGERMLINE.SEQUENCE
            //             CONSENSUS          (differences)
            var annotated = template.ConsensusSequenceAnnotation();
            var match = template.AlignConsensusWithTemplate();
            var columns = new List<(char Template, char Query, char Difference, Annotation Class)>();

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
                            columns.Add((t, q, t == q ? ' ' : q, annotated[template_pos]));
                            template_pos++;
                            query_pos++;
                        }
                        break;
                    case SequenceMatch.GapInTemplate q:
                        for (int i = 0; i < q.Length; i++)
                        {
                            var t = match.TemplateSequence[template_pos].Character;
                            columns.Add((t, '.', ' ', annotated[template_pos]));
                            template_pos++;
                        }
                        break;
                    case SequenceMatch.GapInQuery t:
                        for (int i = 0; i < t.Length; i++)
                        {
                            var q = match.QuerySequence[query_pos].Character;
                            columns.Add(('.', q, q, annotated[template_pos]));
                            query_pos++;
                        }
                        break;
                }
            }

            buffer.Append($"<h2>Annotated consensus sequence {id}</h2><div class='annotated'><div class='names'><span>Consensus</span><span>Germline</span></div>");

            var present = new HashSet<Annotation>();
            foreach (var column in columns)
            {
                if (column.Template == 'X' && (column.Query == '.' || column.Query == 'X')) continue;
                var title = "";
                if (column.Class.IsAnyCDR())
                    if (!present.Contains(column.Class))
                    {
                        present.Add(column.Class);
                        title = $"<span class='title'>{column.Class}</span>";
                    }
                buffer.Append($"<div class='{column.Class}'>{title}<span>{column.Query}</span><span>{column.Template}</span><span class='dif'>{column.Difference}</span></div>");
            }
            buffer.Append("</div><div class='annotated legend'><p class='names'>Legend</p><span class='CDR'>CDR</span><span class='Conserved'>Conserved</span><span class='Glycosylationsite'>Possible glycosylation site</span></div>");
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
            var placed_ids = new HashSet<string>(); // To make sure to only give the first align-link its ID

            if (alignedSequences.Count == 0)
                return new List<double>();

            buffer.Append("<div class='alignment'><h2>Alignment</h2><button class='copy-data'>Copy Data</button>");

            // Loop over aligned
            // For each position: (creates List<string[]>, per position, per sequence + template_sequence)
            // Convert AA to string (fill in with gap_char)
            // Convert Gap to string (get max length, align all gaps, fill in with spaces)

            // Convert to lines: (creates List<string>)
            // Combine horizontally
            var total_sequences = alignedSequences[0].Sequences.Length;
            var lines = new List<(string Sequence, int Index, int SequencePosition, AsideType Type)>[total_sequences + 1];
            const char gap_char = '-';
            const char non_breaking_space = '\u00A0'; // &nbsp; in html
            var depthOfCoverage = new List<double>();
            var data_buffer = new StringBuilder();

            for (int i = 0; i < total_sequences + 1; i++)
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
                        lines[i + 1].Add((gap_char.ToString(), -1, -1, AsideType.Read));
                    }
                    else if (index == 0)
                    {
                        lines[i + 1].Add((non_breaking_space.ToString(), -1, -1, AsideType.Read));
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
                lines[0].Add((new string(gap_char, max_length), -1, -1, AsideType.Read));

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

                    char pad_char = non_breaking_space;
                    if (Gaps[i].InSequence) pad_char = gap_char;

                    var index = Gaps[i].ContigID == -1 ? -1 : Gaps[i].MatchIndex;

                    var type = AsideType.Read;
                    var idx = index;

                    lines[i + 1].Add((seq.PadRight(max_length, pad_char), idx, Sequences[i].SequencePosition - 1, type));
                }
                var depthGapCombined = new double[max_length];
                foreach (var d in depthGap)
                {
                    depthGapCombined = depthGapCombined.ElementwiseAdd(d);
                }
                depthOfCoverage.AddRange(depthGapCombined.Select(a => (double)a));
            }

            // The aligned reads, any space between, leading, or trailing the reads is filled with non_breaking_spaces, while gaps are indicated by the gap_char ('-')
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

            buffer.AppendLine($"<div class=\"reads-alignment\" style=\"--max-value:{Math.Max(depthOfCoverage.Max(), 1)}\">");

            // Create the front overhanging reads block
            var front_overhang_buffer = new StringBuilder();
            bool front_overhang = false;
            front_overhang_buffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"front-overhang-toggle-{id}\"/><label for=\"front-overhang-toggle-{id}\">");
            front_overhang_buffer.AppendFormat("<div class='align-block overhang-block front-overhang'><p><span class='front-overhang-spacing'></span>");

            for (int i = 1; i < aligned.Length; i++)
            {
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition != 0 && match.StartTemplatePosition == 0)
                {
                    front_overhang = true;
                    front_overhang_buffer.Append($"<a href=\"#\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(0, match.StartQueryPosition))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    front_overhang_buffer.Append("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }

            if (front_overhang)
            {
                buffer.Append(front_overhang_buffer.ToString().TrimEnd("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>"));
                buffer.AppendLine($"</p></div></label></div>");
            }

            // Chop it up, add numbers etc
            const int block_length = 5;

            if (aligned.Length > 0)
            {
                // Create a position based lookup list for the annotated sequence
                var annotatedSequence = new List<Annotation>();
                if (template.Recombination != null)
                {
                    foreach (var segment in template.Recombination)
                    {
                        // Remove Xs and overlap to get the final sequence as used in the recombination read alignment
                        var x_start = segment.ConsensusSequence().Item1.TakeWhile(a => a.Character == 'X').Count();
                        var main_sequence = segment.ConsensusSequence().Item1.Skip(x_start).TakeWhile(a => a.Character != 'X').Count();
                        annotatedSequence.AddRange(segment.ConsensusSequenceAnnotation().Skip(x_start).Take(main_sequence).Skip(segment.Overlap));
                    }
                }
                else
                {
                    annotatedSequence = template.ConsensusSequenceAnnotation().ToList();
                }

                // Fill in the gaps
                for (int i = 0; i < aligned[0].Length; i++)
                {
                    if (aligned[0][i] == gap_char)
                    {
                        if (i == 0)
                        {
                            annotatedSequence.Insert(i, annotatedSequence[i]);
                        }
                        else
                        {
                            if (annotatedSequence[i - 1] == Annotation.None || annotatedSequence[i] == Annotation.None)
                                annotatedSequence.Insert(i, Annotation.None);
                            else if (annotatedSequence[i - 1] != annotatedSequence[i])
                                annotatedSequence.Insert(i, Annotation.None);
                            else
                                annotatedSequence.Insert(i, annotatedSequence[i]);
                        }
                    }
                }


                int aligned_index = 0;
                int aligned_length = 0;
                for (int block = 0; block * block_length < aligned[0].Length; block++)
                {
                    // Get the right id's to generate the right links
                    while (aligned_length < block * block_length && aligned_index + 1 < lines[0].Count)
                    {
                        aligned_length += lines[0][aligned_index].Sequence.Length;
                        aligned_index++;
                    }

                    var positions = new List<(int index, int position, int length)>[aligned.Length];

                    for (int i = 1; i < aligned.Length; i++)
                    {
                        int index = lines[i][aligned_index].Index;
                        int position = lines[i][aligned_index].SequencePosition;
                        int additional_length = 0;
                        int additional_index = 1;
                        positions[i] = new List<(int index, int position, int length)>();

                        while (aligned_length + additional_length < (block + 1) * block_length && aligned_index + additional_index < lines[0].Count)
                        {
                            int this_index = lines[i][aligned_index + additional_index].Index;
                            int this_position = lines[i][aligned_index + additional_index].SequencePosition;

                            if (index == -1)
                            {
                                index = this_index;
                                position = this_position;
                            }
                            else if (this_index != -1 && this_index != index)
                            {
                                positions[i].Add((index, position, additional_length));
                                index = this_index;
                                position = this_position;
                                break;
                            }

                            additional_length += lines[0][aligned_index + additional_index].Sequence.Length;
                            additional_index++;
                        }

                        if (index >= 0)
                            positions[i].Add((index, position, block_length));
                    }


                    // Add the sequence and the number to tell the position
                    string number = "";
                    if (aligned[0].Length - block * block_length >= block_length)
                    {
                        number = ((block + 1) * block_length).ToString();
                        number = string.Concat(Enumerable.Repeat("&nbsp;", block_length - number.Length)) + number;
                    }
                    buffer.Append($"<div class='align-block'{TemplateAlignmentAnnotation(annotatedSequence, block, block_length)}>");
                    var align_block = new StringBuilder($"<div class='wrapper'><div class=\"number\">{number}</div><div class=\"seq\">{aligned[0].Substring(block * block_length, Math.Min(block_length, aligned[0].Length - block * block_length))}</div>");

                    const string empty_text = "<div class='empty'></div>";
                    const string begin_block = "<div class='align-link'>";
                    var empty = 0;
                    for (int i = 1; i < aligned.Length; i++)
                    {
                        if (positions[i].Count > 0)
                        {
                            align_block.Append(begin_block);
                            int offset = 0;
                            bool placed = false;
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
                                if (aligned[i].Length > block * block_length + offset)
                                {
                                    // Get the block of sequence for this piece, determine if there are leading or trailing spaces and add empty text for those
                                    var seq = aligned[i].Substring(block * block_length + offset, Math.Max(Math.Min(Math.Min(piece.length, aligned[i].Length - block * block_length - offset), block_length - offset), 0));
                                    var length = seq.Length;
                                    seq = seq.TrimStart(non_breaking_space);
                                    if (seq.Length == 0)
                                        continue; // Sequence was only whitespace so ignore
                                    var start_padding = length - seq.Length;
                                    if (start_padding > 0)
                                        align_block.Append(string.Concat(Enumerable.Repeat("&nbsp;", start_padding)));

                                    length = seq.Length;
                                    seq = seq.TrimEnd(non_breaking_space);
                                    var end_padding = length - seq.Length;

                                    var element_id = GetAsideIdentifier(template.Matches[piece.index].MetaData);
                                    var html_id = placed_ids.Contains(element_id) ? "" : " id='aligned-" + element_id + "'";
                                    align_block.Append($"<a href=\"{path}\"{html_id} class=\"align-link{unique}\" onmouseover=\"AlignmentDetails({template.Matches[piece.index].Index})\" onmouseout=\"AlignmentDetailsClear()\">{seq}</a>");
                                    placed = true;

                                    if (end_padding > 0)
                                        align_block.Append(string.Concat(Enumerable.Repeat("&nbsp;", end_padding)));

                                    if (!placed_ids.Contains(element_id))
                                    {
                                        // Retrieve the full sequence for this read and place it in the fasta expo
                                        var start = block * block_length + offset + start_padding;
                                        var next_space = aligned[i].IndexOf(non_breaking_space, start);
                                        var len = (next_space > 0 ? next_space : aligned[i].Length) - start;
                                        var fasta_seq = aligned[i].Substring(start, len);
                                        fasta_seq = fasta_seq.Replace(gap_char, '.');
                                        fasta_seq = new String('-', start) + fasta_seq + new String('-', aligned[0].Length - start - len);
                                        data_buffer.AppendLine($">{rid} score:{template.Matches[piece.index].Score} alignment:{template.Matches[piece.index].Alignment.CIGAR()}\n{fasta_seq}");
                                    }
                                    placed_ids.Add(element_id);
                                }
                                offset = piece.length;
                            }
                            if (!placed) // There are cases where the placed block would be empty, so catch that to make the trim empty work
                            {
                                align_block.Remove(align_block.Length - begin_block.Length, begin_block.Length);
                                align_block.Append(empty_text);
                                empty += 1;
                            }
                            else
                            {
                                align_block.Append("</div>");
                                empty = 0;
                            }
                        }
                        else
                        {
                            align_block.Append(empty_text);
                            empty += 1;
                        }
                    }
                    // First add the coverage depth wrapper then the align-block
                    buffer.AppendLine("<div class='coverage-depth-wrapper'>");

                    for (int i = block * block_length; i < block * block_length + Math.Min(block_length, depthOfCoverage.Count - block * block_length); i++)
                    {
                        buffer.Append($"<span class='coverage-depth-bar' style='--value:{depthOfCoverage[i]}'></span>");
                    }
                    buffer.Append("</div>");
                    var align_block_string = align_block.ToString();
                    buffer.Append(align_block_string.Substring(0, align_block_string.Length - empty_text.Length * empty)); // trim the number of empty blocks that are known to be after the alignblock
                    buffer.Append("</div></div>");
                }
            }

            // Create the end overhanging reads block
            var end_overhang_buffer = new StringBuilder();
            bool end_overhang = false;
            end_overhang_buffer.AppendLine($"<div class='align-block'><input type='checkbox' id=\"end-overhang-toggle-{id}\"/><label for=\"end-overhang-toggle-{id}\">");
            end_overhang_buffer.AppendFormat("<div class='align-block overhang-block end-overhang'><p><span class='end-overhang-spacing'></span>");
            for (int i = 1; i < aligned.Length; i++)
            {
                var match = template.Matches[i - 1];
                if (match.StartQueryPosition + match.TotalMatches < match.QuerySequence.Length && match.StartTemplatePosition + match.TotalMatches == match.TemplateSequence.Length)
                {
                    end_overhang = true;
                    end_overhang_buffer.Append($"<a href=\"#\" class='text align-link'>{AminoAcid.ArrayToString(match.QuerySequence.SubArray(match.StartQueryPosition + match.TotalMatches, match.QuerySequence.Length - match.StartQueryPosition - match.TotalMatches))}</a><span class='symbol'>...</span><br>");
                }
                else
                {
                    end_overhang_buffer.Append("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>");
                }
            }
            if (end_overhang)
            {
                buffer.Append(end_overhang_buffer.ToString().TrimEnd("<a href=\"#\" class='text align-link'></a><span class='symbol'></span><br>"));
                buffer.AppendLine($"</p></div></label></div>");
            }

            // Index menus
            buffer.Append("<div id='index-menus'>");
            foreach (var match in template.Matches)
            {
                AlignmentDetails(buffer, match, template);
            }

            buffer.AppendLine($"</div></div><textarea type='text' class='graph-data' aria-hidden='true'>{data_buffer.ToString()}</textarea></div>");
            return depthOfCoverage;
        }

        /// <summary>
        /// Create the background colour annotation for the reads alignment blocks.
        /// </summary>
        /// <param name="annotated">The annotation, a list of all Types for each position as finally aligned.</param>
        /// <param name="block">The selected block.</param>
        /// <param name="blocklength">The selected blocklength.</param>
        /// <returns>The colour as a style element to directly put in a HTML element.</returns>
        static string TemplateAlignmentAnnotation(List<Annotation> annotated, int block, int blocklength)
        {
            string Color(Annotation Type)
            {
                if (Type == Annotation.None)
                {
                    return "var(--color-background)";
                }
                else if (Type.IsAnyCDR())
                {
                    return "var(--color-secondary-o)";
                }
                else if (Type == Annotation.Conserved)
                {
                    return "var(--color-tertiary-o)";
                }
                else
                {
                    return "var(--color-primary-o)";
                }
            }
            string Point(uint point)
            {
                return Math.Round((double)point / blocklength * 100).ToString() + "%";
            }
            if (annotated == null) return "";
            var annotatedSequence = annotated.ToArray();
            var localLength = Math.Min(blocklength, annotatedSequence.Length - block * blocklength);
            var annotation = annotatedSequence.SubArray(block * blocklength, localLength);
            var grouped = new List<(Annotation, uint)>() { (annotation[0], 1) };
            for (int i = 1; i < localLength; i++)
            {
                var last = grouped.Count - 1;
                if (annotation[i] == grouped[last].Item1)
                {
                    grouped[last] = (grouped[last].Item1, grouped[last].Item2 + 1);
                }
                else
                {
                    grouped.Add((annotation[i], 1));
                }
            }
            if (grouped[0] == (Annotation.None, 5))
            {
                return "";
            }
            else if (grouped[0].Item2 == 5)
            {
                return " style='background-color:" + Color(grouped[0].Item1) + "'"; // Switch color based on type
            }
            else
            {
                var output = " style='background:linear-gradient(to right";
                uint len = 0;
                foreach (var piece in grouped)
                {
                    output += ", " + Color(piece.Item1) + " " + Point(len) + ", " + Color(piece.Item1) + " " + Point(len + piece.Item2);
                    len += piece.Item2;
                }
                return output + ");'";
            }
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
            if (match.MetaData is ReadMetaData.Peaks p)
            {
                buffer.Append($"<tr><td>Peaks ALC</td><td>{p.DeNovoScore}</td></tr>");
            }
            if (match.MetaData.PositionalScore.Length != 0)
            {
                buffer.Append($"<tr><td>{doctitle}</td><td class='docplot'>");
                HTMLGraph.Bargraph(buffer, HTMLGraph.AnnotateDOCData(match.MetaData.PositionalScore.SubArray(match.StartQueryPosition, match.TotalMatches).Select(a => (double)a).ToList(), match.StartQueryPosition));
                buffer.Append("</td></tr>");
            }

            buffer.Append($@"<tr><td>Alignment graphic</td><td class='sequence-match-graphic'>");
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

        static void SequenceConsensusOverview(StringBuilder buffer, Template template, string title = null, string help = null)
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
            HTMLTables.SequenceConsensusOverview(buffer, diversity, title, help, template.ConsensusSequenceAnnotation());
        }
    }
}