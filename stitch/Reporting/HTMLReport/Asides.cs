using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stitch;
using static HTMLNameSpace.CommonPieces;
using static Stitch.HelperFunctionality;
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
        public static HtmlBuilder CreateTemplateAside(Template template, AsideType type, string AssetsFolderName, int totalReads)
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

            html.Add(SequenceConsensusOverview(template, "Sequence Consensus Overview", new HtmlBuilder(HtmlTag.p, HTMLHelp.SequenceConsensusOverview)));
            (var alignment, var gaps) = CreateTemplateAlignment(template, id, location, AssetsFolderName);
            html.Add(SequenceAmbiguityOverview(template, gaps));

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
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Sequence.Length.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Score.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Matches.Count().ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.TotalArea.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.UniqueScore.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.UniqueMatches.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.TotalUniqueArea.ToString("G4"));
            html.Close(HtmlTag.tr);
            html.Close(HtmlTag.table);

            html.Add(based);

            html.Add(alignment);
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
                    case SequenceMatch.Deletion q:
                        for (int i = 0; i < q.Length; i++)
                        {
                            var t = match.TemplateSequence[template_pos].Character;
                            columns.Add((t, '.', ' ', annotated[query_pos - match.StartQueryPosition]));
                            template_pos++;
                        }
                        break;
                    case SequenceMatch.Insertion t:
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

        static public (HtmlBuilder, int[]) CreateTemplateAlignment(Template template, string id, List<string> location, string AssetsFolderName)
        {
            //var alignedSequences = template.AlignedSequences();
            var placed_ids = new HashSet<string>(); // To make sure to only give the first align-link its ID
            var html = new HtmlBuilder();
            const char gap_char = '-';
            const char non_breaking_space = '\u00A0'; // &nbsp; in html
            var gaps = new int[template.Sequence.Length];

            if (template.Matches.Count == 0)
                return (html, gaps);

            html.Open(HtmlTag.div, "class='alignment'");
            html.OpenAndClose(HtmlTag.h2, "", "Alignment");
            html.CopyData("Reads Alignment (FASTA)", new HtmlBuilder(HTMLHelp.ReadsAlignment));

            var depthOfCoverage = new List<double>();
            var data_buffer = new StringBuilder();
            var consensus = template.ConsensusSequence();

            data_buffer.AppendLine($">{template.MetaData.EscapedIdentifier} template\n{AminoAcid.ArrayToString(consensus.Item1).Replace(gap_char, '.')}");

            var localMatches = template.Matches.ToList();
            localMatches.Sort((a, b) => b.LengthOnTemplate.CompareTo(a.LengthOnTemplate)); // Try to keep the longest matches at the top.

            // Find the longest gaps for each position.
            foreach (var match in localMatches)
            {
                var pos = match.StartTemplatePosition;
                foreach (var piece in match.Alignment)
                {
                    if (piece is SequenceMatch.Match ma)
                    {
                        pos += piece.Length;
                    }
                    else if (piece is SequenceMatch.Deletion)
                    {
                        pos += piece.Length;
                    }
                    else if (piece is SequenceMatch.Insertion)
                    {
                        gaps[pos] = Math.Max(gaps[pos], piece.Length);
                    }
                }
            }
            var total_length = gaps.Sum() + template.Sequence.Length + 1;

            // Helper methods to insert the gaps in the sequences
            (string, int) DisplayWithGaps(SequenceMatch match)
            {
                var pos = 0;

                var sequence = new List<string>();
                foreach (var piece in match.Alignment)
                {
                    if (piece is SequenceMatch.Match ma)
                    {
                        sequence.AddRange(match.QuerySequence.SubArray(match.StartQueryPosition + pos, piece.Length).Select(a => a.Character.ToString()));
                        pos += piece.Length;
                    }
                    else if (piece is SequenceMatch.Deletion)
                    {
                        sequence.AddRange(Enumerable.Repeat(gap_char.ToString(), piece.Length));
                    }
                    else if (piece is SequenceMatch.Insertion)
                    {
                        sequence.Add(AminoAcid.ArrayToString(match.QuerySequence.SubArray(match.StartQueryPosition + pos, piece.Length)));
                        pos += piece.Length;
                    }
                }
                pos = match.StartTemplatePosition;
                var sequence_list = new LinkedList<string>(sequence);
                var node = sequence_list.First;
                var start_pad = 0;
                bool first = true;
                var insertion_length = 0;
                foreach (var piece in match.Alignment)
                {
                    if (piece is SequenceMatch.Insertion)
                    {
                        if (!first && gaps[pos] != 0) sequence_list.AddBefore(node, new string(gap_char, gaps[pos] - piece.Length - insertion_length));
                        insertion_length = piece.Length;
                        node = node.Next;
                        first = false;
                    }
                    else
                    {
                        for (int i = 0; i < piece.Length; i++, pos++)
                        {
                            if (first) start_pad = gaps[pos];
                            if (!first && gaps[pos] != 0) sequence_list.AddBefore(node, new string(gap_char, gaps[pos] - insertion_length));
                            insertion_length = 0;
                            node = node.Next;
                            first = false;
                        }
                    }

                }
                return (string.Concat(sequence_list), start_pad);
            }

            string DisplayTemplateWithGaps()
            {
                var sequence = new LinkedList<string>(template.Sequence.Select(a => a.Character.ToString()));
                var node = sequence.First;
                for (int pos = 0; pos < template.Sequence.Length; pos++)
                {
                    if (gaps[pos] != 0) sequence.AddBefore(node, new string(gap_char, gaps[pos]));
                    node = node.Next;
                }
                return string.Concat(sequence);
            }

            // Find template sequence and numbering
            var template_sequence = DisplayTemplateWithGaps();
            var numbering = new StringBuilder();
            var last_size = 1;
            const int block_size = 10;
            for (int i = 10; i < total_length - block_size; i += block_size)
            {
                var i_string = i.ToString();
                numbering.Append(new string(non_breaking_space, block_size - last_size));
                numbering.Append(i_string);
                last_size = i_string.Length;
            }

            var annotatedSequence = template.ConsensusSequenceAnnotation().ToList();
            // Fill in the gaps
            for (int i = 0; i < template_sequence.Length; i++)
            {
                if (template_sequence[i] == gap_char)
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
            html.Open(HtmlTag.div, "class='buttons'");
            html.OpenAndClose(HtmlTag.button, "onclick='ToggleCDRReads()'", "Only show CDR reads");
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div, "class='alignment-wrapper'");
            html.Open(HtmlTag.div, $"class='alignment-body' style='grid-template-columns:repeat({total_length}, 1ch);{TemplateAlignmentAnnotation(annotatedSequence)}'");
            html.OpenAndClose(HtmlTag.div, $"class='numbering' style='grid-column-end:{total_length}'", numbering.ToString());
            html.OpenAndClose(HtmlTag.div, $"class='template' style='grid-column-end:{total_length}'", template_sequence);

            var ambiguous = template.SequenceAmbiguityAnalysis();
            foreach (var read in localMatches)
            {
                var start = 1 + gaps.Take(read.StartTemplatePosition).Sum() + read.StartTemplatePosition;
                (var seq, var start_pad) = DisplayWithGaps(read);
                start += start_pad;
                var end = start + seq.Length + 1; // Add padding between reads
                if (start + seq.Length == total_length)
                    end -= 1;

                // Determine classes for this read
                var classes = new List<string>();
                if (read.Unique) classes.Add("unique");

                // Detect it this read is part of any CDR
                for (int i = 0; i < seq.Length && start - 1 + i < annotatedSequence.Count; i++)
                    if (annotatedSequence[start - 1 + i].IsAnyCDR())
                    {
                        classes.Add("cdr");
                        break;
                    }

                foreach (var a in ambiguous)
                    if (a.SupportingReads.Contains(read.MetaData.Identifier))
                        classes.Add($"a{a.Position}");

                var classes_string = string.Join(' ', classes);
                classes_string = string.IsNullOrEmpty(classes_string) ? "" : $"class='{classes_string}' ";

                // Generate the actual HTMl and Fasta lines
                html.OpenAndClose(
                    HtmlTag.a,
                    $"{classes_string}href='{CommonPieces.GetAsideRawLink(read.MetaData, AsideType.Read, AssetsFolderName, location)}' target='_blank' style='grid-column-start:{start};grid-column-end:{end};' onmouseover='AlignmentDetails({read.Index})' onmouseout='AlignmentDetailsClear()'",
                    seq);
                data_buffer.AppendLine($">{read.MetaData.EscapedIdentifier} score:{read.Score} alignment:{read.Alignment.CIGAR()} unique:{read.Unique}\n{new string('~', start)}{seq.Replace(gap_char, '.')}{new string('~', total_length - end)}");
            }

            html.Close(HtmlTag.div);
            html.Close(HtmlTag.div);

            // Index menus
            html.Open(HtmlTag.div, "id='index-menus'");
            foreach (var match in template.Matches)
            {
                html.Add(AlignmentDetails(match, template));
            }
            html.Close(HtmlTag.div);
            html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", data_buffer.ToString());
            html.Close(HtmlTag.div);
            return (html, gaps);
        }

        /// <summary>
        /// Create the background colour annotation for the reads alignment blocks.
        /// </summary>
        /// <param name="annotated">The annotation, a list of all Types for each position as finally aligned.</param>
        /// <returns>The colour as a style element to directly put in a HTML element.</returns>
        static string TemplateAlignmentAnnotation(List<Annotation> annotated)
        {
            string Color(Annotation Type)
            {
                if (Type.IsAnyCDR())
                    return "var(--color-secondary-o)";
                else if (Type == Annotation.Conserved)
                    return "var(--color-tertiary-o)";
                else
                    return "var(--color-primary-o)";
            }
            var annotatedSequence = annotated.ToArray();
            var grouped = new List<(Annotation, uint)>() { (annotated[0], 1) };
            for (int i = 1; i < annotated.Count; i++)
            {
                var last = grouped.Count - 1;
                if (annotated[i] == grouped[last].Item1)
                {
                    grouped[last] = (grouped[last].Item1, grouped[last].Item2 + 1);
                }
                else
                {
                    grouped.Add((annotated[i], 1));
                }
            }
            var output1 = new StringBuilder("background-image:");
            var output2 = new StringBuilder("background-size:");
            var output3 = new StringBuilder("background-position:");
            uint len = 0;
            foreach (var piece in grouped)
            {
                if (piece.Item1 != Annotation.None)
                {
                    output1.Append("linear-gradient(to right, " + Color(piece.Item1) + ", " + Color(piece.Item1) + "),");
                    output2.Append($"{piece.Item2}ch,");
                    output3.Append($"{len}ch,");
                }
                len += piece.Item2;
            }
            output1.Append("linear-gradient(to right, var(--color-primary-o), var(--color-primary-o))");
            output2.Append("1ch");
            output3.Append("var(--highlight-pos)");
            return output1 + ";" + output2 + ";" + output3 + ";background-repeat:no-repeat;";
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
            Row("Total area", match.MetaData.TotalArea.ToString("G4"));
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
                else if (piece is SequenceMatch.Deletion)
                    id = "gap-in-template";
                else if (piece is SequenceMatch.Insertion)
                    id = "gap-in-query";

                html.OpenAndClose(HtmlTag.span, $"class='{id}' style='flex-grow:{piece.Length}'");
            }
            return html;
        }

        static HtmlBuilder SequenceConsensusOverview(Template template, string title = null, HtmlBuilder help = null)
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
            return HTMLTables.SequenceConsensusOverview(diversity, title, help, template.ConsensusSequenceAnnotation(), template.SequenceAmbiguityAnalysis().Select(a => a.Position).ToArray());
        }

        static HtmlBuilder SequenceAmbiguityOverview(Template template, int[] gaps)
        {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, "class='ambiguity-overview'");
            html.TagWithHelp(HtmlTag.h2, "Ambiguity Overview", new HtmlBuilder(HTMLHelp.AmbiguityOverview.Replace("{threshold}", Template.AmbiguityThreshold.ToString("P0"))));

            var ambiguous = template.SequenceAmbiguityAnalysis();

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
                var h = (max_nodes + 1) * height_option + pad;
                svg.Open(SvgTag.svg, $"viewBox='0 0 {w} {h}' width='{w}px' height='{h}px'");
                svg.Open(SvgTag.defs);
                svg.Open(SvgTag.marker, "id='marker' viewBox='0 0 10 10' refX='1' refY='5' markerUnits='strokeWidth' markerWidth='1.5' markerHeight='1.5' orient='auto'");
                svg.OpenAndClose(SvgTag.path, "d='M 0 0 L 10 5 L 0 10 z'");
                svg.Close(SvgTag.marker);
                svg.Close(SvgTag.defs);

                for (var node_pos = 0; node_pos < placed.Length; node_pos++)
                {
                    var position = ambiguous[node_pos].Position;
                    svg.OpenAndClose(SvgTag.rect, $"class='node a{position}' onclick='HighlightAmbiguous(\"a{position}\", {gaps.Take(position + 1).Sum() + position})' x='{pad + node_pos * width_option - clearing}px' y='{clearing}px' width='{width_option}px' height='{h - clearing * 2}px'");
                    svg.OpenAndClose(SvgTag.text, $"x='{pad + node_pos * width_option}px' y='{height_option}px' class='position'", (position + 1).ToString());
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

                html.OpenAndClose(HtmlTag.p, "", "Determine stroke width:");
                html.Empty(HtmlTag.input, "type='checkbox' id='ambiguity-stroke-selector'");
                html.Open(HtmlTag.label, "for='ambiguity-stroke-selector'");
                html.OpenAndClose(HtmlTag.span, "", "Local");
                html.OpenAndClose(HtmlTag.span, "", "Global");
                html.Close(HtmlTag.label);
                html.Open(HtmlTag.div, "id='ambiguity-wrapper'");
                html.Add(svg);
                html.Close(HtmlTag.div);
                foreach (var position in ambiguous)
                {
                    html.Open(HtmlTag.pre, $"class='a{position.Position}'");
                    foreach (var variant in position.SupportTrees)
                        html.Content($"flowchart LR;\n{variant.Value.Mermaid()}\n\n");
                    html.Close(HtmlTag.pre);
                }
            }

            html.Close(HtmlTag.div);
            return html;
        }
    }
}