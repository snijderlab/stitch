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
using HtmlGenerator;
using HeckLib.ConvenienceInterfaces.SpectrumMatch;

namespace HTMLNameSpace
{
    public static class HTMLAsides
    {
        /// <summary> Returns an aside for details viewing of a read. </summary>
        public static HtmlBuilder CreateReadAside((string Sequence, ReadMetaData.IMetaData MetaData) read, ReadOnlyCollection<(string, List<Segment>)> segments, ReadOnlyCollection<Segment> recombined, string AssetsFolderName, Dictionary<string, List<AnnotatedSpectrumMatch>> Fragments)
        {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, $"id='{GetAsideIdentifier(read.MetaData)}' class='info-block read-info'");
            html.OpenAndClose(HtmlTag.h1, "", "Read " + GetAsideIdentifier(read.MetaData, true));
            html.OpenAndClose(HtmlTag.h2, "", "Sequence");
            html.OpenAndClose(HtmlTag.p, "class='aside-seq'", read.Sequence);
            html.OpenAndClose(HtmlTag.h2, "", "Sequence Length");
            html.OpenAndClose(HtmlTag.p, "", read.Sequence.Length.ToString());

            if (Fragments != null && Fragments.ContainsKey(read.MetaData.EscapedIdentifier))
            {
                foreach (var spectrum in Fragments[read.MetaData.EscapedIdentifier])
                {
                    html.Add(Graph.RenderSpectrum(spectrum, new HtmlBuilder(HtmlTag.p, HTMLHelp.Spectrum)));
                }
            }
            html.TagWithHelp(HtmlTag.h2, "Reverse Lookup", new HtmlBuilder(HTMLHelp.ReadLookup));
            html.Open(HtmlTag.table, "class='wide-table'");
            html.Open(HtmlTag.tr);
            html.OpenAndClose(HtmlTag.th, "", "Group");
            html.OpenAndClose(HtmlTag.th, "", "Segment");
            html.OpenAndClose(HtmlTag.th, "", "Template");
            html.OpenAndClose(HtmlTag.th, "", "Location");
            html.OpenAndClose(HtmlTag.th, "", "Score");
            html.OpenAndClose(HtmlTag.th, "", "Unique");
            html.Close(HtmlTag.tr);

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
                                html.Open(HtmlTag.tr);
                                html.OpenAndClose(HtmlTag.td, "class='center'", group.Item1);
                                html.OpenAndClose(HtmlTag.td, "class='center'", segment.Name);
                                html.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(template.MetaData, AsideType.Template, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(read.MetaData)));
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.StartTemplatePosition.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Score.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Unique.ToString());
                                html.Close(HtmlTag.tr);
                            }
                        }
                    }
                }
            }
            if (recombined != null)
            {
                html.Close(HtmlTag.table);
                html.Open(HtmlTag.table, "class='wide-table'");
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.th, "", "Recombined");
                html.OpenAndClose(HtmlTag.th, "", "Location");
                html.OpenAndClose(HtmlTag.th, "", "Score");
                html.OpenAndClose(HtmlTag.th, "", "Unique");
                html.Close(HtmlTag.tr);
                foreach (var segment in recombined)
                {
                    foreach (var template in segment.Templates)
                    {
                        foreach (var match in template.Matches)
                        {
                            if (match.MetaData.Identifier == read.MetaData.Identifier)
                            {
                                html.Open(HtmlTag.tr);
                                html.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(template.MetaData, AsideType.RecombinedTemplate, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(read.MetaData)));
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.StartTemplatePosition.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Score.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Unique.ToString());
                                html.Close(HtmlTag.tr);
                            }
                        }
                    }
                }
            }

            html.Close(HtmlTag.table);
            html.Add(read.MetaData.ToHTML());
            html.Close(HtmlTag.div);
            return html;
        }

        /// <summary> Returns an aside for details viewing of a template. </summary>
        public static HtmlBuilder CreateTemplateAside(Template template, AsideType type, string AssetsFolderName, int totalReads, double ambiguity_threshold)
        {
            var html = new HtmlBuilder();
            string id = GetAsideIdentifier(template.MetaData);
            string human_id = GetAsideIdentifier(template.MetaData, true);
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };

            var (consensus_sequence, consensus_doc) = template.ConsensusSequence();

            HtmlBuilder based = new HtmlBuilder();
            string title = "Segment";
            switch (type)
            {
                case AsideType.RecombinedTemplate:
                    if (template.Recombination != null)
                    {
                        var first = true;
                        var order = template.Recombination.Aggregate(
                            new HtmlBuilder(),
                            (acc, seg) =>
                            {
                                if (first) first = false;
                                else acc.Content(" → ");
                                acc.Add(GetAsideLinkHtml(seg.MetaData, AsideType.Template, AssetsFolderName, location));
                                return acc;
                            }
                        );
                        based.OpenAndClose(HtmlTag.h2, "", "Order");
                        based.OpenAndClose(HtmlTag.p, "", order);
                    }
                    title = "Recombined Template";
                    break;
                default:
                    break;
            }

            html.Open(HtmlTag.div, $"id='{id} class='info-block template-info'");
            html.OpenAndClose(HtmlTag.h1, "", title + " " + human_id);
            html.TagWithHelp(HtmlTag.h2, "Consensus Sequence", new HtmlBuilder(HTMLHelp.ConsensusSequence.ToString()));
            html.OpenAndClose(HtmlTag.p, "class='aside-seq'", AminoAcid.ArrayToString(consensus_sequence));
            html.Add(CreateAnnotatedSequence(human_id, template));

            html.Add(SequenceConsensusOverview(template, ambiguity_threshold, "Sequence Consensus Overview", new HtmlBuilder(HtmlTag.p, HTMLHelp.SequenceConsensusOverview)));

            html.Add(SequenceAmbiguityOverview(template, ambiguity_threshold));

            html.Open(HtmlTag.div, "class='doc-plot'");
            html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(consensus_doc), new HtmlBuilder("Depth of Coverage of the Consensus Sequence"), new HtmlBuilder(HtmlTag.p, HTMLHelp.DOCGraph), null, 10, template.ConsensusSequenceAnnotation()));
            html.Close(HtmlTag.div);

            html.OpenAndClose(HtmlTag.h2, "", "Scores");
            html.Open(HtmlTag.table, "class='wide-table'");
            html.Open(HtmlTag.tr);
            html.TagWithHelp(HtmlTag.th, "Length", new HtmlBuilder(HTMLHelp.TemplateLength.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Score", new HtmlBuilder(HTMLHelp.TemplateScore.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Matches", new HtmlBuilder(HTMLHelp.TemplateMatches.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Total Area", new HtmlBuilder(HTMLHelp.TemplateTotalArea.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Unique Score", new HtmlBuilder(HTMLHelp.TemplateUniqueScore.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Unique Matches", new HtmlBuilder(HTMLHelp.TemplateUniqueMatches.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Unique Area", new HtmlBuilder(HTMLHelp.TemplateUniqueArea.ToString()), "small-cell");
            html.Close(HtmlTag.tr);
            html.Open(HtmlTag.tr);
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Sequence.Length.ToString());
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Score.ToString());
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Matches.Count().ToString());
            html.OpenAndClose(HtmlTag.td, "class='center'", template.TotalArea.ToString());
            html.OpenAndClose(HtmlTag.td, "class='center'", template.UniqueScore.ToString());
            html.OpenAndClose(HtmlTag.td, "class='center'", template.UniqueMatches.ToString());
            html.OpenAndClose(HtmlTag.td, "class='center'", template.TotalUniqueArea.ToString());
            html.Close(HtmlTag.tr);
            html.Close(HtmlTag.table);

            html.Add(based);

            (var doc_html, var DepthOfCoverage) = CreateTemplateAlignment(template, id, location, AssetsFolderName);
            html.Add(doc_html);
            html.Add(CreateTemplateGraphs(template, DepthOfCoverage));
            html.TagWithHelp(HtmlTag.h2, "Template Sequence", new HtmlBuilder(template.Recombination != null ? HTMLHelp.RecombinedSequence.ToString() : HTMLHelp.TemplateSequence.ToString()));
            html.OpenAndClose(HtmlTag.p, "class='aside-seq'", AminoAcid.ArrayToString(template.Sequence));

            if (template.MetaData != null && type == AsideType.Template)
                html.Add(template.MetaData.ToHTML());

            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder CreateAnnotatedSequence(string id, Template template)
        {
            // Create an overview of the alignment from consensus with germline.
            // Also highlight differences and IMGT regions

            // HERECOMESTHECONSENSUSSEQUENCE  (coloured to IMGT region)
            // HERECOMESTHEGERMLINE.SEQUENCE
            //             CONSENSUS          (differences)
            var annotated = template.ConsensusSequenceAnnotation();
            var match = template.AlignConsensusWithTemplate();
            var columns = new List<(char Template, char Query, char Difference, Annotation Class)>();
            var data_buffer = new StringBuilder();
            var html = new HtmlBuilder();

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
                            columns.Add((t, q, t == q ? ' ' : q, annotated[query_pos - match.StartQueryPosition]));
                            template_pos++;
                            query_pos++;
                        }
                        break;
                    case SequenceMatch.GapInTemplate q:
                        for (int i = 0; i < q.Length; i++)
                        {
                            var t = match.TemplateSequence[template_pos].Character;
                            columns.Add((t, '.', ' ', annotated[query_pos - match.StartQueryPosition]));
                            template_pos++;
                        }
                        break;
                    case SequenceMatch.GapInQuery t:
                        for (int i = 0; i < t.Length; i++)
                        {
                            var q = match.QuerySequence[query_pos].Character;
                            columns.Add(('.', q, q, annotated[query_pos - match.StartQueryPosition]));
                            query_pos++;
                        }
                        break;
                }
            }

            html.Open(HtmlTag.div, "class='annotated-consensus-sequence'");
            html.UnsafeContent(CommonPieces.TagWithHelp("h2", "Annotated Consensus Sequence", HTMLHelp.AnnotatedConsensusSequence.ToString()));
            html.UnsafeContent(CommonPieces.CopyData("Annotated Consensus Sequence (TXT)"));
            html.Open(HtmlTag.div, "class='annotated'");
            html.Open(HtmlTag.div, "class='names'");
            html.OpenAndClose(HtmlTag.span, "", "Consensus");
            html.OpenAndClose(HtmlTag.span, "", "Germline");
            html.Close(HtmlTag.div);

            var present = new HashSet<Annotation>();
            foreach (var column in columns)
            {
                if (column.Template == 'X' && (column.Query == '.' || column.Query == 'X')) continue;
                html.Open(HtmlTag.div, $"class='{column.Class}'");
                if (column.Class.IsAnyCDR())
                    if (!present.Contains(column.Class))
                    {
                        present.Add(column.Class);
                        html.OpenAndClose(HtmlTag.span, "class='title'", column.Class.ToString());
                    }
                html.OpenAndClose(HtmlTag.span, "", column.Query.ToString());
                html.OpenAndClose(HtmlTag.span, "", column.Template.ToString());
                html.OpenAndClose(HtmlTag.span, "class='dif'", column.Difference.ToString());
                html.Close(HtmlTag.div);
            }
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div, "class='annotated legend'");
            html.OpenAndClose(HtmlTag.p, "class='names'", "Legend");
            html.OpenAndClose(HtmlTag.span, "class='CDR'", "CDR");
            html.OpenAndClose(HtmlTag.span, "class='Conserved'", "Conserved");
            html.OpenAndClose(HtmlTag.span, "class='Glycosylationsite'", "Possible glycosylation site");
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'");
            var (c, g, d) = columns.Aggregate(("", "", ""), (acc, c) => (acc.Item1 + c.Template, acc.Item2 + c.Query, acc.Item3 + c.Difference));
            html.Content($"Consensus  {c}\nGermline   {g}\nDifference {d}");
            html.Close(HtmlTag.textarea);
            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder CreateTemplateGraphs(Template template, List<double> DepthOfCoverage)
        {
            var html = new HtmlBuilder();
            if (template.Matches.Count == 0) return html;
            html.OpenAndClose(HtmlTag.h3, "", "Graphs");
            html.Open(HtmlTag.div, "class='template-graphs'");
            html.Open(HtmlTag.div, "class='doc-plot'");
            html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(DepthOfCoverage), new HtmlBuilder("Depth of Coverage (including gaps)")));
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div, "class='doc-plot'");
            html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(DepthOfCoverage.Select(a => a == 0 ? 0 : Math.Log10(a)).ToList()), new HtmlBuilder("Log10 Depth of Coverage (including gaps)")));
            html.Close(HtmlTag.div);

            if (template.ForcedOnSingleTemplate && template.UniqueMatches > 0)
            {
                // Histogram of Scores
                html.Open(HtmlTag.div);
                html.Add(HTMLGraph.GroupedHistogram(new List<(List<double>, string)> { (template.Matches.Select(a => (double)a.Score).ToList(), "Normal"), (template.Matches.FindAll(a => a.Unique).Select(a => (double)a.Score).ToList(), "Unique") }, "Score Distribution"));

                // Histogram of Length On Template
                html.Close(HtmlTag.div);
                html.Open(HtmlTag.div);
                html.Add(HTMLGraph.GroupedHistogram(new List<(List<double>, string)> { (template.Matches.Select(a => (double)a.LengthOnTemplate).ToList(), "Normal"), (template.Matches.FindAll(a => a.Unique).Select(a => (double)a.LengthOnTemplate).ToList(), "Unique") }, "Length on Template Distribution"));
            }
            else
            {
                // Histogram of Scores
                html.Open(HtmlTag.div);
                html.Add(HTMLGraph.Histogram(template.Matches.Select(a => (double)a.Score).ToList(), new HtmlBuilder("Score Distribution")));

                // Histogram of Length On Template
                html.Close(HtmlTag.div);
                html.Open(HtmlTag.div);
                html.Add(HTMLGraph.Histogram(template.Matches.Select(a => (double)a.LengthOnTemplate).ToList(), new HtmlBuilder("Length on Template Distribution")));
            }

            // Histogram of coverage, coverage per position excluding gaps
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div);
            html.Add(HTMLGraph.Histogram(template.CombinedSequence().Select(a => a.AminoAcids.Values.Sum()).ToList(), new HtmlBuilder("Coverage Distribution")));

            html.OpenAndClose(HtmlTag.i, "", "Excludes gaps in reference to the template sequence");
            html.Close(HtmlTag.div);
            html.Close(HtmlTag.div);
            return html;
        }

        static public (HtmlBuilder, List<double>) CreateTemplateAlignment(Template template, string id, List<string> location, string AssetsFolderName)
        {
            var alignedSequences = template.AlignedSequences();
            var placed_ids = new HashSet<string>(); // To make sure to only give the first align-link its ID
            var html = new HtmlBuilder();

            if (alignedSequences.Count == 0)
                return (html, new List<double>());

            html.Open(HtmlTag.div, "class='alignment'");
            html.OpenAndClose(HtmlTag.h2, "", "Alignment");
            html.UnsafeContent(CommonPieces.CopyData("Reads Alignment (FASTA)", HTMLHelp.ReadsAlignment.ToString()));

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

            html.Open(HtmlTag.div, $"class='reads-alignment' style='--max-value:{Math.Max(depthOfCoverage.Max(), 1)}'");

            html.Add(OverHang(id, aligned, template, true));

            data_buffer.AppendLine($">{template.MetaData.EscapedIdentifier} template\n{aligned[0].Replace(gap_char, '.')}");

            // Chop it up, add numbers etc
            const int block_length = 5;

            if (aligned.Length > 0)
            {
                // Create a position based lookup list for the annotated sequence
                var annotatedSequence = template.ConsensusSequenceAnnotation().ToList();

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


                    html.Open(HtmlTag.div, $"class='align-block'{TemplateAlignmentAnnotation(annotatedSequence, block, block_length)}");
                    html.Open(HtmlTag.div, "class='coverage-depth-wrapper'");

                    for (int i = block * block_length; i < block * block_length + Math.Min(block_length, depthOfCoverage.Count - block * block_length); i++)
                    {
                        html.OpenAndClose(HtmlTag.span, $"class='coverage-depth-bar' style='--value:{depthOfCoverage[i]}'", "");
                    }
                    html.Close(HtmlTag.div);

                    html.Add(AlignBlock(aligned, template, block, block_length, placed_ids, non_breaking_space, positions, gap_char, AssetsFolderName, location, data_buffer));
                    html.Close(HtmlTag.div);
                }
            }

            html.Add(OverHang(id, aligned, template, false));

            // Index menus
            html.Open(HtmlTag.div, "id='index-menus'");
            foreach (var match in template.Matches)
            {
                html.Add(AlignmentDetails(match, template));
            }
            html.Close(HtmlTag.div);
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'");
            html.Content(data_buffer.ToString());
            html.Close(HtmlTag.textarea);
            html.Close(HtmlTag.div);
            return (html, depthOfCoverage);
        }

        static HtmlBuilder AlignBlock(string[] aligned, Template template, int block, int block_length, HashSet<string> placed_ids, char non_breaking_space, List<(int index, int position, int length)>[] positions, char gap_char, string AssetsFolderName, List<string> location, StringBuilder data_buffer)
        {
            // Add the sequence and the number to tell the position
            string number = "";
            if (aligned[0].Length - block * block_length >= block_length)
            {
                number = ((block + 1) * block_length).ToString();
                number = string.Concat(Enumerable.Repeat(non_breaking_space, block_length - number.Length)) + number;
            }
            var align_block = new HtmlBuilder();
            align_block.Open(HtmlTag.div, "class='wrapper'");
            align_block.OpenAndClose(HtmlTag.div, "class='number'", number.ToString());
            align_block.OpenAndClose(HtmlTag.div, "class='seq'", aligned[0].Substring(block * block_length, Math.Min(block_length, aligned[0].Length - block * block_length)));

            uint empty = 0;
            for (int i = 1; i < aligned.Length; i++)
            {
                if (positions[i].Count > 0)
                {
                    align_block.Open(HtmlTag.div, "class='align-link'");
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
                                align_block.Content(string.Concat(Enumerable.Repeat(non_breaking_space, start_padding)));

                            length = seq.Length;
                            seq = seq.TrimEnd(non_breaking_space);
                            var end_padding = length - seq.Length;

                            var element_id = GetAsideIdentifier(template.Matches[piece.index].MetaData);
                            var html_id = placed_ids.Contains(element_id) ? "" : " id='aligned-" + element_id + "'";
                            align_block.OpenAndClose(HtmlTag.a, $"href=\"{path}\"{html_id} class='align-link{unique}' onmouseover='AlignmentDetails({template.Matches[piece.index].Index})' onmouseout='AlignmentDetailsClear()'", seq);
                            placed = true;

                            if (end_padding > 0)
                                align_block.Content(string.Concat(Enumerable.Repeat(non_breaking_space, end_padding)));

                            if (!placed_ids.Contains(element_id))
                            {
                                // Retrieve the full sequence for this read and place it in the fasta expo
                                var start = block * block_length + offset + start_padding;
                                var next_space = aligned[i].IndexOf(non_breaking_space, start);
                                var len = (next_space > 0 ? next_space : aligned[i].Length) - start;
                                var fasta_seq = aligned[i].Substring(start, len);
                                fasta_seq = fasta_seq.Replace(gap_char, '.');
                                fasta_seq = new String('~', start) + fasta_seq + new String('~', aligned[0].Length - start - len);
                                data_buffer.AppendLine($">{rid} score:{template.Matches[piece.index].Score} alignment:{template.Matches[piece.index].Alignment.CIGAR()}\n{fasta_seq}");
                            }
                            placed_ids.Add(element_id);
                        }
                        offset = piece.length;
                    }
                    if (!placed) // There are cases where the placed block would be empty, so catch that to make the trim empty work
                    {
                        align_block.Close(HtmlTag.div);
                        align_block.UnsafeRemoveElementsFromEnd(1);
                        align_block.OpenAndClose(HtmlTag.div, "class='empty'", "");
                        empty += 1;
                    }
                    else
                    {
                        align_block.Close(HtmlTag.div);
                        empty = 0;
                    }
                }
                else
                {
                    align_block.OpenAndClose(HtmlTag.div, "class='empty'", "");
                    empty += 1;
                }
            }
            align_block.UnsafeRemoveElementsFromEnd(empty);
            align_block.Close(HtmlTag.div);
            return align_block;
        }

        static HtmlBuilder OverHang(string id, string[] aligned, Template template, bool front)
        {
            // Create the overhanging reads block
            var html = new HtmlBuilder();
            bool overhang = false;
            var name = front ? "front" : "end";

            html.Open(HtmlTag.div, "class='align-block'");
            html.Empty(HtmlTag.input, $"type='checkbox' id='{name}-overhang-toggle-{id}'");
            html.Open(HtmlTag.label, $"for='{name}-overhang-toggle-{id}'");
            html.Open(HtmlTag.div, $"class='align-block overhang-block {name}-overhang'");
            html.OpenAndClose(HtmlTag.span, $"class='{name}-overhang-spacing'", "");

            uint empty = 0;
            for (int i = 1; i < aligned.Length; i++)
            {
                var match = template.Matches[i - 1];
                html.Open(HtmlTag.div, "class='align-link'");
                if (front && match.StartQueryPosition != 0 && match.StartTemplatePosition == 0)
                {
                    overhang = true;
                    html.OpenAndClose(HtmlTag.a, "href='#' class='text align-link'", AminoAcid.ArrayToString(match.QuerySequence.SubArray(0, match.StartQueryPosition)));
                    html.OpenAndClose(HtmlTag.span, "class='symbol'", "...");
                    empty = 0;
                }
                else if (!front && match.StartQueryPosition + match.TotalMatches < match.QuerySequence.Length && match.StartTemplatePosition + match.TotalMatches == match.TemplateSequence.Length)
                {
                    overhang = true;
                    html.OpenAndClose(HtmlTag.a, "href='#' class='text align-link'", AminoAcid.ArrayToString(match.QuerySequence.SubArray(match.StartQueryPosition + match.TotalMatches, match.QuerySequence.Length - match.StartQueryPosition - match.TotalMatches)));
                    html.OpenAndClose(HtmlTag.span, "class='symbol'", "...");
                    empty = 0;
                }
                else
                {
                    html.OpenAndClose(HtmlTag.a, "class='text'", "");
                    html.OpenAndClose(HtmlTag.span, "class='symbol'", "");
                    empty += 1;
                }
                html.Close(HtmlTag.div);
            }
            if (overhang)
            {
                html.UnsafeRemoveElementsFromEnd(empty * 3);
                html.Close(HtmlTag.div);
                html.Close(HtmlTag.label);
                html.Close(HtmlTag.div);
                return html;
            }
            return new HtmlBuilder();
        }

        /// <summary>
        /// Create the background colour annotation for the reads alignment blocks.
        /// </summary>
        /// <param name="annotated">The annotation, a list of all Types for each position as finally aligned.</param>
        /// <param name="block">The selected block.</param>
        /// <param name="block_length">The selected block length.</param>
        /// <returns>The colour as a style element to directly put in a HTML element.</returns>
        static string TemplateAlignmentAnnotation(List<Annotation> annotated, int block, int block_length)
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
                return Math.Round((double)point / block_length * 100).ToString() + "%";
            }
            if (annotated == null) return "";
            var annotatedSequence = annotated.ToArray();
            var localLength = Math.Min(block_length, annotatedSequence.Length - block * block_length);
            if (localLength < 1) return "";
            var annotation = annotatedSequence.SubArray(block * block_length, localLength);
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

        static HtmlBuilder AlignmentDetails(SequenceMatch match, Template template)
        {
            var doc_title = "Positional Score";
            var type = "Read";
            var html = new HtmlBuilder();

            void Row(string name, string content)
            {
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.td, "", name);
                html.OpenAndClose(HtmlTag.td, "", content);
                html.Close(HtmlTag.tr);
            }

            html.Open(HtmlTag.div, $"class='alignment-details' id='alignment-details-{match.Index}'");
            html.OpenAndClose(HtmlTag.h4, "", match.MetaData.Identifier);
            html.Open(HtmlTag.table);
            Row("Type", type);
            Row("Score", match.Score.ToString());
            Row("Total area", match.MetaData.TotalArea.ToString());
            Row("Length on Template", match.LengthOnTemplate.ToString());
            Row("Position on Template", match.StartTemplatePosition.ToString());
            Row($"Start on {type}", match.StartQueryPosition.ToString());
            Row($"Length of {type}", match.QuerySequence.Length.ToString());

            if (template.ForcedOnSingleTemplate)
            {
                Row("Unique", match.Unique ? "Yes" : "No");
            }
            if (match.MetaData is ReadMetaData.Peaks p)
            {
                Row("Peaks ALC", p.DeNovoScore.ToString());
            }
            if (match.MetaData.PositionalScore.Length != 0)
            {
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.td, "", doc_title);
                html.Open(HtmlTag.td, "class='doc-plot'");
                html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(match.MetaData.PositionalScore.SubArray(match.StartQueryPosition, match.TotalMatches).Select(a => (double)a).ToList(), match.StartQueryPosition, true)));
                html.Close(HtmlTag.td);
                html.Close(HtmlTag.tr);
            }
            html.Open(HtmlTag.tr);
            html.OpenAndClose(HtmlTag.td, "", "Alignment graphic");
            html.Open(HtmlTag.td, "class='sequence-match-graphic'");
            html.Add(SequenceMatchGraphic(match));
            html.Close(HtmlTag.td);
            html.Close(HtmlTag.tr);
            html.Close(HtmlTag.table);
            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder SequenceMatchGraphic(SequenceMatch match)
        {
            var id = "none";
            var html = new HtmlBuilder();
            foreach (var piece in match.Alignment)
            {
                if (piece is SequenceMatch.Match)
                    id = "match";
                else if (piece is SequenceMatch.GapInTemplate)
                    id = "gap-in-template";
                else if (piece is SequenceMatch.GapInQuery)
                    id = "gap-in-query";

                html.OpenAndClose(HtmlTag.span, $"class='{id}' style='flex-grow:{piece.Length}'");
            }
            return html;
        }

        static HtmlBuilder SequenceConsensusOverview(Template template, double ambiguity_threshold, string title = null, HtmlBuilder help = null)
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
            return HTMLTables.SequenceConsensusOverview(diversity, title, help, template.ConsensusSequenceAnnotation(), template.SequenceAmbiguityAnalysis(ambiguity_threshold).Select(a => a.Position).ToArray());
        }

        static HtmlBuilder SequenceAmbiguityOverview(Template template, double threshold)
        {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, "class='ambiguity-overview'");
            html.TagWithHelp(HtmlTag.h2, "Ambiguity Overview", new HtmlBuilder(HTMLHelp.AmbiguityOverview.Replace("{threshold}", threshold.ToString("P"))));

            var ambiguous = template.SequenceAmbiguityAnalysis(threshold);

            if (ambiguous.Length == 0 || ambiguous.All(n => n.Support.Count == 0))
            {
                html.OpenAndClose(HtmlTag.i, "", "No ambiguous positions for analysis.");
            }
            else
            {
                var max_support = ambiguous.SelectMany(n => n.Support.Values).Max();

                // Find the position of each aminoacid node by determining the total support for that AA 
                // at that position and sorting on highest at the top.
                var placed = new List<(AminoAcid, double)>[ambiguous.Length];
                for (int i = 0; i < placed.Length; i++) placed[i] = new();

                void Update(int i, AminoAcid key, double value)
                {
                    var pos = placed[i].FindIndex(p => p.Item1 == key);
                    if (pos == -1) placed[i].Add((key, value));
                    else placed[i][pos] = (key, placed[i][pos].Item2 + value);
                }

                for (int i = 0; i < ambiguous.Length - 1; i++)
                {
                    foreach (var set in ambiguous[i].Support)
                    {
                        Update(i, set.Key.Item1, set.Value);
                        Update(i + 1, set.Key.Item2, set.Value);
                    }
                }

                for (int i = 0; i < placed.Length; i++) placed[i].Sort((a, b) => b.Item2.CompareTo(a.Item2));
                var max_nodes = placed.Select(n => n.Count).Max();

                // Now generate the Html + Svg
                var svg = new SvgBuilder();
                const int width_option = 80; // Horizontal size of an AA option
                const int height_option = 20; // Vertical size of an AA option
                const int pad = 10; // Padding in front of and after everything in the X axis
                const int text_pad = 12; // Approximate size of text (cube)
                const int clearing = 2; // Extra clearing in front of options
                var w = (placed.Length - 1) * width_option + width_option / 2 + pad * 2;
                var h = (max_nodes + 1) * height_option;
                svg.Open(SvgTag.svg, $"viewBox='0 0 {w} {h}' width='{w}px' height='{h}px'");
                svg.Open(SvgTag.defs);
                svg.Open(SvgTag.marker, "id='marker' viewBox='0 0 10 10' refX='1' refY='5' markerUnits='strokeWidth' markerWidth='1.5' markerHeight='1.5' orient='auto'");
                svg.OpenAndClose(SvgTag.path, "d='M 0 0 L 10 5 L 0 10 z'");
                svg.Close(SvgTag.marker);
                svg.Close(SvgTag.defs);

                for (var node_pos = 0; node_pos < placed.Length; node_pos++)
                {
                    svg.OpenAndClose(SvgTag.text, $"x='{pad + node_pos * width_option}px' y='{height_option}px' class='position'", ambiguous[node_pos].Position.ToString());
                    var max_local = ambiguous[node_pos].Support.Count > 0 ? ambiguous[node_pos].Support.Values.Max() : 0;

                    for (var option_pos = 0; option_pos < placed[node_pos].Count; option_pos++)
                    {
                        svg.OpenAndClose(SvgTag.text, $"x='{pad + node_pos * width_option}px' y='{(option_pos + 2) * height_option}px' class='option'", placed[node_pos][option_pos].Item1.Character.ToString());
                    }

                    foreach (var set in ambiguous[node_pos].Support)
                    {
                        var y1 = placed[node_pos].FindIndex(p => p.Item1 == set.Key.Item1);
                        var y2 = placed[node_pos + 1].FindIndex(p => p.Item1 == set.Key.Item2);
                        svg.OpenAndClose(SvgTag.line, $"x1={pad + node_pos * width_option + text_pad}px y1={(y1 + 2) * height_option - text_pad / 2}px x2={pad + (node_pos + 1) * width_option - clearing}px y2={(y2 + 2) * height_option - text_pad / 2}px class='support-arrow' style='--support-global:{set.Value / max_support};--support-local:{set.Value / max_local}'");
                    }
                }
                svg.OpenAndClose(SvgTag.line, $"x1='0px' y1='{height_option + 4}px' x2='{w}px' y2='{height_option + 4}px' class='baseline'");
                svg.Close(SvgTag.svg);

                html.Open(HtmlTag.div, "id='ambiguity-wrapper'");
                html.OpenAndClose(HtmlTag.p, "", "Determine stroke width:");
                html.Empty(HtmlTag.input, "type='checkbox' id='ambiguity-stroke-selector'");
                html.Open(HtmlTag.label, "for='ambiguity-stroke-selector'");
                html.OpenAndClose(HtmlTag.span, "", "Local");
                html.OpenAndClose(HtmlTag.span, "", "Global");
                html.Close(HtmlTag.label);
                html.Add(svg);
                html.Close(HtmlTag.div);
            }

            html.Close(HtmlTag.div);
            return html;
        }
    }
}