using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using HeckLib.ConvenienceInterfaces.SpectrumMatch;
using HtmlGenerator;
using Stitch;
using static HTMLNameSpace.CommonPieces;
using static Stitch.HelperFunctionality;

namespace HTMLNameSpace {
    public static class HTMLAsides {
        const char gap_char = '-';
        const char non_breaking_space = '\u00A0'; // &nbsp; in html

        /// <summary> Returns an aside for details viewing of a read. </summary>
        public static HtmlBuilder CreateReadAside(Read.IRead MetaData, ReadOnlyCollection<(string, List<Segment>)> segments, ReadOnlyCollection<Segment> recombined, string AssetsFolderName, Dictionary<string, List<AnnotatedSpectrumMatch>> Fragments) {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, $"id='{GetAsideIdentifier(MetaData)}' class='info-block read-info'");
            html.OpenAndClose(HtmlTag.h1, "", "Read " + GetAsideIdentifier(MetaData, true));
            html.OpenAndClose(HtmlTag.h2, "", $"Sequence (length={MetaData.Sequence.Sequence.Length})");
            html.OpenAndClose(HtmlTag.p, "class='aside-seq'", AminoAcid.ArrayToString(MetaData.Sequence.Sequence));

            if (Fragments != null && Fragments.ContainsKey(MetaData.EscapedIdentifier)) {
                foreach (var spectrum in Fragments[MetaData.EscapedIdentifier]) {
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

            foreach (var group in segments) {
                foreach (var segment in group.Item2) {
                    foreach (var template in segment.Templates) {
                        foreach (var match in template.Matches.ToList()) {
                            if (match.ReadB.Identifier == MetaData.Identifier) {
                                html.Open(HtmlTag.tr);
                                html.OpenAndClose(HtmlTag.td, "class='center'", group.Item1);
                                html.OpenAndClose(HtmlTag.td, "class='center'", segment.Name);
                                html.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(template.MetaData, AsideType.Template, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(MetaData)));
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.StartA.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Score.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Unique.ToString());
                                html.Close(HtmlTag.tr);
                            }
                        }
                    }
                }
            }
            html.Close(HtmlTag.table);
            if (recombined != null && recombined.Count > 0) {
                html.Open(HtmlTag.table, "class='wide-table'");
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.th, "", "Recombined");
                html.OpenAndClose(HtmlTag.th, "", "Location");
                html.OpenAndClose(HtmlTag.th, "", "Score");
                html.OpenAndClose(HtmlTag.th, "", "Unique");
                html.Close(HtmlTag.tr);
                foreach (var segment in recombined) {
                    foreach (var template in segment.Templates) {
                        foreach (var match in template.Matches) {
                            if (match.ReadB.Identifier == MetaData.Identifier) {
                                html.Open(HtmlTag.tr);
                                html.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(template.MetaData, AsideType.RecombinedTemplate, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(MetaData)));
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.StartA.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Score.ToString());
                                html.OpenAndClose(HtmlTag.td, "class='center'", match.Unique.ToString());
                                html.Close(HtmlTag.tr);
                            }
                        }
                    }
                }
                html.Close(HtmlTag.table);
            }

            html.Add(MetaData.ToHTML());
            html.Close(HtmlTag.div);
            return html;
        }

        /// <summary> Returns an aside for details viewing of a template. </summary>
        public static HtmlBuilder CreateTemplateAside(Template template, AsideType type, string AssetsFolderName, int totalReads) {
            var html = new HtmlBuilder();
            string id = GetAsideIdentifier(template.MetaData);
            string human_id = GetAsideIdentifier(template.MetaData, true);
            var location = new List<string>() { AssetsFolderName, GetAsideName(type) + "s" };

            var (consensus_sequence, consensus_doc) = template.ConsensusSequence();

            HtmlBuilder based = new HtmlBuilder();
            string title = "Segment";
            switch (type) {
                case AsideType.RecombinedTemplate:
                    if (template.Recombination != null) {
                        var first = true;
                        var order = template.Recombination.Aggregate(
                            new HtmlBuilder(),
                            (acc, seg) => {
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
            html.OpenAndClose(HtmlTag.p, "class='aside-seq'", AminoAcid.ArrayToString(consensus_sequence.SelectMany(i => i.Sequence)));
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
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Matches.Count.ToString("G4"));
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

        static HtmlBuilder CreateAnnotatedSequence(string id, Template template) {
            // Create an overview of the alignment from consensus with germline.
            // Also highlight differences and IMGT regions

            // HERECOMESTHECONSENSUSSEQUENCE  (coloured to IMGT region)
            // HERECOMESTHEGERMLINE.SEQUENCE
            //             CONSENSUS          (differences)
            var annotated = template.ConsensusSequenceAnnotation(); // TODO: is having the annotation based on the consensus the best way forward? Or should it be done based on template?
            var match = template.AlignConsensusWithTemplate();
            var columns = new List<(string Template, string Query, string Difference, Annotation Class)>();
            var data_buffer = new StringBuilder();
            var html = new HtmlBuilder();

            int pos_a = match.StartA;
            int pos_b = match.StartB;

            foreach (var piece in match.Path) {
                var a = AminoAcid.ArrayToString(match.ReadA.Sequence.Sequence.SubArray(pos_a, piece.StepA));
                var b = AminoAcid.ArrayToString(match.ReadB.Sequence.Sequence.SubArray(pos_b, piece.StepB));
                if (!(String.IsNullOrEmpty(a) && b == "X")) {
                    columns.Add((
                        a.Length == 0 ? ScoringMatrix.GapChar.ToString() : a,
                        b.Length == 0 ? ScoringMatrix.GapChar.ToString() : b,
                        a == b ? new string(non_breaking_space, piece.StepA) : b,
                        pos_b >= annotated.Length ? Annotation.None : annotated[pos_b]));
                }
                pos_a += piece.StepA;
                pos_b += piece.StepB;
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
            foreach (var column in columns) {
                if (column.Template == "X" && (column.Query == ScoringMatrix.GapChar.ToString() || column.Query == "X")) continue;
                html.Open(HtmlTag.div, $"class='{column.Class}'");
                if (column.Class.IsAnyCDR())
                    if (!present.Contains(column.Class)) {
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

        static public (HtmlBuilder, int[]) CreateTemplateAlignment(Template template, string id, List<string> location, string AssetsFolderName) {
            //var alignedSequences = template.AlignedSequences();
            var placed_ids = new HashSet<string>(); // To make sure to only give the first align-link its ID
            var html = new HtmlBuilder();
            var gaps = new int[template.Sequence.Length + 1];

            if (template.Matches.Count == 0)
                return (html, gaps);

            html.Open(HtmlTag.div, "class='alignment'");
            html.OpenAndClose(HtmlTag.h2, "", "Alignment");
            html.CopyData("Reads Alignment (FASTA)", new HtmlBuilder(HTMLHelp.ReadsAlignment));

            var depthOfCoverage = new List<double>();
            var data_buffer = new StringBuilder();
            var consensus = template.ConsensusSequence();

            data_buffer.AppendLine($">{template.MetaData.EscapedIdentifier} template\n{AminoAcid.ArrayToString(consensus.Item1.SelectMany(i => i.Sequence)).Replace(gap_char, '.')}");

            var localMatches = template.Matches.ToList();
            localMatches.Sort((a, b) => b.LenA.CompareTo(a.LenA)); // Try to keep the longest matches at the top.

            // Find the longest gaps for each position.
            foreach (var match in localMatches) {
                var pos_a = match.StartA;
                var pos_b = match.StartB;
                var gap = 0;
                foreach (var piece in match.Path) {
                    if (piece.StepA == 0)
                        gap += pos_a == match.StartA || pos_a == match.StartA + match.LenA ? 0 : piece.StepB;
                    else if (gap > 0) {
                        gaps[pos_a] = Math.Max(gaps[pos_a], gap);
                        gap = 0;
                    }
                    pos_a += piece.StepA;
                    pos_b += piece.StepB;
                }
                if (gap > 0)
                    gaps[pos_a] = Math.Max(gaps[pos_a], gap);
            }
            var total_length = gaps.Sum() + template.Sequence.Length + 1;

            (string, Annotation[]) DisplayTemplateWithGaps() {
                var sequence = new LinkedList<string>(template.Sequence.Select(a => a.Character.ToString()));
                var annotation = new LinkedList<Annotation>(template.ConsensusSequenceAnnotation());
                var node = sequence.First;
                var annotation_node = annotation.First;
                for (int pos = 0; pos < template.Sequence.Length; pos++) {
                    if (gaps[pos] != 0) {
                        var current_annotation = annotation_node.Value;
                        if (pos > 0 && annotation_node.Previous.Value == Annotation.None || annotation_node.Value == Annotation.None)
                            current_annotation = Annotation.None;
                        else if (pos > 0 && annotation_node.Previous.Value != annotation_node.Value)
                            current_annotation = Annotation.None;
                        annotation.AddBefore(annotation_node, current_annotation);
                        sequence.AddBefore(node, new string(gap_char, gaps[pos]));
                    }
                    node = node.Next;
                    if (annotation_node.Next == null)
                        annotation.AddAfter(annotation_node, Annotation.None);
                    annotation_node = annotation_node.Next;
                }
                return (string.Concat(sequence), annotation.ToArray());
            }

            // Find template sequence and numbering
            var (template_sequence, annotatedSequence) = DisplayTemplateWithGaps();
            var numbering = new StringBuilder();
            var last_size = 1;
            const int block_size = 10;
            for (int i = 10; i < total_length - block_size; i += block_size) {
                var i_string = i.ToString();
                numbering.Append(new string(non_breaking_space, block_size - last_size));
                numbering.Append(i_string);
                last_size = i_string.Length;
            }

            html.Open(HtmlTag.div, "class='buttons'");
            html.OpenAndClose(HtmlTag.button, "onclick='ToggleCDRReads()'", "Only show CDR reads");
            html.OpenAndClose(HtmlTag.button, "onclick='ToggleAlignmentComic()'", "Show as comic");
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div, "class='alignment-wrapper'");
            html.Open(HtmlTag.div, $"class='alignment-body' style='grid-template-columns:repeat({total_length}, 1ch);{TemplateAlignmentAnnotation(annotatedSequence)}'");
            html.OpenAndClose(HtmlTag.div, $"class='numbering' style='grid-column-end:{total_length}'", numbering.ToString());
            html.OpenAndClose(HtmlTag.div, $"class='template' style='grid-column-end:{total_length}'", template_sequence);

            var ambiguous = template.SequenceAmbiguityAnalysis();
            for (var alignment_index = 0; alignment_index < localMatches.Count; alignment_index++) {
                var alignment = localMatches[alignment_index];

                // Determine classes for this read
                var classes = new List<string>();
                if (alignment.Unique) classes.Add("unique");

                // Detect it this read is part of any CDR
                for (int i = 0; i < alignment.LenA; i++)
                    if (annotatedSequence[alignment.StartA + i].IsAnyCDR()) {
                        classes.Add("cdr");
                        break;
                    }

                foreach (var a in ambiguous)
                    if (a.SupportingReads.Contains(alignment.ReadB.Identifier))
                        classes.Add($"a{a.Position}");

                var classes_string = string.Join(' ', classes);
                classes_string = string.IsNullOrEmpty(classes_string) ? "" : $"class='{classes_string}' ";

                // Generate the actual HTMl and Fasta lines
                var len_start = alignment.Path.TakeWhile(a => a.StepA == 0).Select(a => (int)a.StepB).Sum();
                var reverse_path = new List<AlignmentPiece>(alignment.Path);
                reverse_path.Reverse();
                var len_end = reverse_path.TakeWhile(a => a.StepA == 0).Select(a => (int)a.StepB).Sum();

                var start = Math.Max(0, 1 + alignment.StartA + gaps.SubArray(0, alignment.StartA + 1).Sum() - len_start);
                var end = start + alignment.LenA + gaps.SubArray(alignment.StartA, alignment.LenA).Sum() + 1 + len_start + len_end;
                html.Open(
                    HtmlTag.a,
                    $"{classes_string}id='aligned-{GetAsideIdentifier(alignment.ReadB)}' href='{CommonPieces.GetAsideRawLink(alignment.ReadB, AsideType.Read, AssetsFolderName, location)}' target='_blank' style='grid-column:{start} / {end};' onmouseover='AlignmentDetails(\"{alignment.ReadB.EscapedIdentifier}\")' onmouseout='AlignmentDetailsClear()'");
                var pos_a = alignment.StartA;
                var pos_b = alignment.StartB;
                var inserted = 0;
                foreach (var piece in alignment.Path) {
                    // TODO: fix the accompanying css
                    var content = AminoAcid.ArrayToString(alignment.ReadB.Sequence.Sequence.SubArray(pos_b, piece.StepB));
                    var total_gaps = piece.StepA == 0 ? 0 : gaps.SubArray(pos_a, piece.StepA).Sum();
                    var positional_gap_char = pos_a == alignment.StartA ? non_breaking_space : gap_char;
                    if (piece.StepA == piece.StepB && piece.StepA > 1 && template.Parent.Alphabet.Swap != 0 && piece.LocalScore == piece.StepA * template.Parent.Alphabet.Swap) {
                        if (inserted > total_gaps) inserted = total_gaps;
                        // Display swaps with internal gaps
                        var already_inserted = gaps[pos_a];
                        html.Content(new string(positional_gap_char, gaps[pos_a]));
                        html.Open(HtmlTag.span, $"class='swap' style='--i:{piece.StepB + total_gaps - inserted - already_inserted};--w:{piece.StepA + total_gaps - already_inserted};'");
                        for (int i = 0; i < piece.StepA; i++) {
                            if (i != 0) html.Content(new string(gap_char, gaps[pos_a + i]));
                            html.Content(content[i].ToString());
                        }
                        html.Close(HtmlTag.span);
                        inserted = 0;
                    } else if (piece.StepA == 0) {
                        if (pos_a == 0 && pos_b == 0) {
                            html.Content(new string(non_breaking_space, Math.Max(0, gaps[pos_a] - len_start)));
                            inserted += Math.Max(0, gaps[pos_a] - len_start);
                        }
                        html.Content(AminoAcid.ArrayToString(alignment.ReadB.Sequence.Sequence.SubArray(pos_b, piece.StepB)));
                        inserted += piece.StepB;
                    } else if (piece.StepB == 0) {
                        if (inserted > total_gaps) inserted = total_gaps;
                        html.Content(new string(positional_gap_char, piece.StepA + total_gaps - inserted));
                        inserted = 0;
                    } else if (piece.StepA != piece.StepB) {
                        // Display unequal sets stretched (or squashed) including all gaps
                        if (inserted > total_gaps) inserted = total_gaps;
                        if (pos_a == alignment.StartA) total_gaps -= gaps[pos_a];
                        var already_inserted = gaps[pos_a];
                        html.Content(new string(positional_gap_char, gaps[pos_a]));
                        html.OpenAndClose(HtmlTag.span, $"style='--i:{piece.StepB};--w:{piece.StepA + total_gaps - inserted - already_inserted};'", content);
                        inserted = 0;
                    } else {
                        if (inserted > total_gaps) inserted = total_gaps;
                        if (pos_a != alignment.StartA) html.Content(new string(gap_char, total_gaps - inserted));
                        html.Content(content);
                        inserted = 0;
                    }
                    pos_a += piece.StepA;
                    pos_b += piece.StepB;
                }
                html.Close(HtmlTag.a);
                // TODO: think about output, because no other program accepts unequal length matches
                //data_buffer.AppendLine($">{alignment.ReadB.EscapedIdentifier} score:{alignment.Score} alignment:{alignment.ShortPath()} unique:{alignment.Unique}\n{new string('~', start)}{seq.Replace(gap_char, '.')}{new string('~', Math.Max(total_length - end, 0))}");
            }

            html.Close(HtmlTag.div);
            html.Close(HtmlTag.div);

            // Index menus
            html.Open(HtmlTag.div, "id='index-menus'");
            for (var i = 0; i < template.Matches.Count; i++) {
                html.Add(AlignmentDetails(template.Matches[i], template));
            }
            html.Close(HtmlTag.div);
            html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", data_buffer.ToString());
            html.Close(HtmlTag.div);
            return (html, gaps);
        }

        /// <summary> Create the background colour annotation for the reads alignment blocks. </summary>
        /// <param name="annotated">The annotation, a list of all Types for each position as finally aligned.</param>
        /// <returns>The colour as a style element to directly put in a HTML element.</returns>
        static string TemplateAlignmentAnnotation(Annotation[] annotated) {
            string Color(Annotation Type) {
                if (Type.IsAnyCDR())
                    return "var(--color-secondary-o)";
                else if (Type == Annotation.Conserved)
                    return "var(--color-tertiary-o)";
                else
                    return "var(--color-primary-o)";
            }
            var annotatedSequence = annotated.ToArray();
            var grouped = new List<(Annotation, uint)>() { (annotated[0], 1) };
            for (int i = 1; i < annotated.Length; i++) {
                var last = grouped.Count - 1;
                if (annotated[i] == grouped[last].Item1) {
                    grouped[last] = (grouped[last].Item1, grouped[last].Item2 + 1);
                } else {
                    grouped.Add((annotated[i], 1));
                }
            }
            var output1 = new StringBuilder("background-image:");
            var output2 = new StringBuilder("background-size:");
            var output3 = new StringBuilder("background-position:");
            uint len = 0;
            foreach (var piece in grouped) {
                if (piece.Item1 != Annotation.None) {
                    output1.Append("linear-gradient(to right, " + Color(piece.Item1) + ", " + Color(piece.Item1) + "),");
                    output2.Append($"{piece.Item2}ch 100%,");
                    output3.Append($"{len}ch,");
                }
                len += piece.Item2;
            }
            output1.Append("linear-gradient(to right, var(--color-primary-o), var(--color-primary-o))");
            output2.Append("1ch 100%");
            output3.Append("var(--highlight-pos)");
            return output1 + ";" + output2 + ";" + output3 + ";background-repeat:no-repeat;";
        }

        static HtmlBuilder AlignmentDetails(Alignment match, Template template) {
            var doc_title = "Positional Score";
            var type = "Read";
            var html = new HtmlBuilder();

            void Row(string name, string content) {
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.td, "", name);
                html.OpenAndClose(HtmlTag.td, "", content);
                html.Close(HtmlTag.tr);
            }

            html.Open(HtmlTag.div, $"class='alignment-details' id='alignment-details-{match.ReadB.EscapedIdentifier}'");
            html.OpenAndClose(HtmlTag.h4, "", match.ReadB.Identifier);
            html.Open(HtmlTag.table);
            Row("Type", type);
            Row("Score", match.Score.ToString());
            Row("Total area", match.ReadB.TotalArea.ToString("G4"));
            Row("Length on Template", match.LenA.ToString());
            Row("Position on Template", match.StartA.ToString());
            Row($"Start on {type}", match.StartB.ToString());
            Row($"Length of {type}", match.LenB.ToString());

            if (template.ForcedOnSingleTemplate) {
                Row("Unique", match.Unique ? "Yes" : "No");
            }
            if (match.ReadB is Read.Peaks p) {
                Row("Peaks ALC", p.DeNovoScore.ToString());
            }
            if (match.ReadB.Sequence.PositionalScore.Length != 0) {
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.td, "", doc_title);
                html.Open(HtmlTag.td, "class='doc-plot'");
                html.Add(HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(match.ReadB.Sequence.PositionalScore.SubArray(match.StartB, match.LenB).Select(a => (double)a).ToList(), match.StartB, true)));
                html.Close(HtmlTag.td);
                html.Close(HtmlTag.tr);
            }
            html.Open(HtmlTag.tr);
            html.OpenAndClose(HtmlTag.td, "", "Alignment graphic");
            html.Open(HtmlTag.td, "class='sequence-match-graphic'");
            html.Add(SequenceMatchGraphic(match, template));
            html.Close(HtmlTag.td);
            html.Close(HtmlTag.tr);
            html.Close(HtmlTag.table);
            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder SequenceMatchGraphic(Alignment match, Template template) {
            var id = "none";
            var html = new HtmlBuilder();
            foreach (var piece in match.Path) {
                // TODO: fix the accompanying css
                if (piece.StepA == piece.StepB && piece.StepA > 1 && template.Parent.Alphabet.Swap != 0 && piece.LocalScore == piece.StepA * template.Parent.Alphabet.Swap) {
                    id = "swap";
                } else if (piece.StepA == 0) {
                    id = "gap-in-template";
                } else if (piece.StepB == 0) {
                    id = "gap-in-query";
                } else if (piece.StepA != piece.StepB) {
                    id = "set";
                } else {
                    id = "match";
                }

                html.OpenAndClose(HtmlTag.span, $"class='{id}' style='flex-grow:{Math.Max(piece.StepA, piece.StepB)}'");
            }
            return html;
        }

        static HtmlBuilder SequenceConsensusOverview(Template template, string title = null, HtmlBuilder help = null) {
            var consensus_sequence = template.CombinedSequence();
            var diversity = new List<Dictionary<(string, int), double>>(consensus_sequence.Count * 2);

            for (int i = 0; i < consensus_sequence.Count; i++) {
                var items = new Dictionary<(string, int), double>();
                foreach (var item in consensus_sequence[i].AminoAcids) {
                    items.Add((AminoAcid.ArrayToString(item.Key.Sequence), item.Key.Length), item.Value);
                }
                diversity.Add(items);
                var gaps = new Dictionary<string, double>();
                foreach (var item in consensus_sequence[i].Gaps) {
                    if (item.Key == (Template.IGap)new Template.None()) {
                        gaps.Add("~", item.Value.Count);
                    } else {
                        gaps.Add(item.Key.ToString(), item.Value.Count);
                    }
                }
            }
            return HTMLTables.SequenceConsensusOverview(diversity, title, help, template.ConsensusSequenceAnnotation(), template.SequenceAmbiguityAnalysis().Select(a => a.Position).ToArray());
        }

        static HtmlBuilder SequenceAmbiguityOverview(Template template, int[] gaps) {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, "class='ambiguity-overview'");
            html.TagWithHelp(HtmlTag.h2, "Ambiguity Overview", new HtmlBuilder(HTMLHelp.AmbiguityOverview.Replace("{threshold}", Template.AmbiguityThreshold.ToString("P0"))));

            var ambiguous = template.SequenceAmbiguityAnalysis();

            if (ambiguous.Length == 0 || ambiguous.All(n => n.Support.Count == 0)) {
                html.OpenAndClose(HtmlTag.i, "", "No ambiguous positions for analysis.");
            } else {
                var max_support = ambiguous.SelectMany(n => n.Support.Values).Max();

                // Find the position of each aminoacid node by determining the total support for that AA
                // at that position and sorting on highest at the top.
                var placed = new List<(AminoAcid, double)>[ambiguous.Length];
                for (int i = 0; i < placed.Length; i++) placed[i] = new();

                void Update(int i, AminoAcid key, double value) {
                    var pos = placed[i].FindIndex(p => p.Item1 == key);
                    if (pos == -1) placed[i].Add((key, value));
                    else placed[i][pos] = (key, placed[i][pos].Item2 + value);
                }

                for (int i = 0; i < ambiguous.Length - 1; i++) {
                    foreach (var set in ambiguous[i].Support) {
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
                const int bar_size = 5; // The width of the support bar
                var w = (placed.Length - 1) * width_option + width_option / 2 + pad * 2;
                var h = (max_nodes + 1) * height_option + pad;
                svg.Open(SvgTag.svg, $"viewBox='0 0 {w} {h}' width='{w}px' height='{h}px'");
                var max_higher_order_support = 0.0;

                for (var node_pos = 0; node_pos < placed.Length; node_pos++) {
                    var position = ambiguous[node_pos].Position;
                    svg.OpenAndClose(SvgTag.rect, $"class='node a{position}' onclick='HighlightAmbiguous(\"a{position}\", {gaps.Take(position + 1).Sum() + position})' x='{pad + node_pos * width_option - clearing}px' y='{clearing}px' width='{width_option}px' height='{h - clearing * 2}px'");
                    svg.OpenAndClose(SvgTag.text, $"x='{pad + node_pos * width_option}px' y='{height_option}px' class='position'", (position + 1).ToString());
                    var max_local = ambiguous[node_pos].Support.Count > 0 ? ambiguous[node_pos].Support.Values.Max() : 0;

                    for (var option_pos = 0; option_pos < placed[node_pos].Count; option_pos++) {
                        var higher_order_trees = ambiguous[node_pos].SupportTrees[placed[node_pos][option_pos].Item1];
                        var support = higher_order_trees.Backward.TotalIntensity() + higher_order_trees.Forward.TotalIntensity();
                        max_higher_order_support = Math.Max(max_higher_order_support, support);
                        svg.OpenAndClose(SvgTag.rect, $"x='{pad + node_pos * width_option + text_pad}px' y='{(option_pos + 2) * height_option}px' width='{bar_size}px' height='{text_pad}px' class='support-bar' style='--support:{support}'", placed[node_pos][option_pos].Item1.Character.ToString());
                        svg.OpenAndClose(SvgTag.text, $"x='{pad + node_pos * width_option}px' y='{(option_pos + 2) * height_option}px' class='option'", placed[node_pos][option_pos].Item1.Character.ToString());
                    }

                    foreach (var set in ambiguous[node_pos].Support) {
                        var y1 = placed[node_pos].FindIndex(p => p.Item1 == set.Key.Item1);
                        var y2 = placed[node_pos + 1].FindIndex(p => p.Item1 == set.Key.Item2);
                        svg.OpenAndClose(SvgTag.line, $"x1={pad + node_pos * width_option + text_pad + bar_size + clearing}px y1={(y1 + 2) * height_option - text_pad / 2}px x2={pad + (node_pos + 1) * width_option - clearing}px y2={(y2 + 2) * height_option - text_pad / 2}px class='support-arrow' style='--support-global:{set.Value / max_support};--support-local:{set.Value / max_local}'");
                    }
                }
                svg.OpenAndClose(SvgTag.line, $"x1='0px' y1='{height_option + clearing * 2}px' x2='{w}px' y2='{height_option + clearing * 2}px' class='baseline'");
                svg.Close(SvgTag.svg);

                html.OpenAndClose(HtmlTag.p, "", "Determine stroke width:");
                html.Empty(HtmlTag.input, "type='checkbox' id='ambiguity-stroke-selector'");
                html.Open(HtmlTag.label, "for='ambiguity-stroke-selector'");
                html.OpenAndClose(HtmlTag.span, "", "Local");
                html.OpenAndClose(HtmlTag.span, "", "Global");
                html.Close(HtmlTag.label);
                html.Open(HtmlTag.div, $"id='ambiguity-wrapper' style='--max-support:{max_higher_order_support}'");
                html.Add(svg);
                html.Close(HtmlTag.div);
                html.Open(HtmlTag.div, $"class='higher-order-graphs'");
                html.TagWithHelp(HtmlTag.h2, "Higher Order Ambiguity Graph", new HtmlBuilder(HTMLHelp.HigherOrderAmbiguityGraph));
                for (int ambiguous_position = 0; ambiguous_position < ambiguous.Length; ambiguous_position++) {
                    var position = ambiguous[ambiguous_position];
                    var graphs = position.SupportTrees.Select(t => (RenderAmbiguityTree(t.Value), t.Value.Forward.Variant)).Select((a) => (a.Item1.Graph, a.Item1.BackwardLength, a.Variant)).ToList();
                    var max_backward_length = graphs.Count == 0 ? 0 : graphs.Max(g => g.BackwardLength);

                    html.Open(HtmlTag.div, $"class='position a{position.Position}' style='--max-backward-length:{max_backward_length};'");
                    if (graphs.Count == 0) {
                        html.OpenAndClose(HtmlTag.p, "class='error'", "No higher order graphs found.");
                        html.Close(HtmlTag.div);
                        continue;
                    }

                    // Find the support for all graphs on this position
                    var order = graphs.Select(i => (i.Variant, 0.0)); // All graphs that were drawn (sometimes one of these has no 1st order support but it has to be included anyway)
                    if (position.Support.Count > 0) order = order.Concat(position.Support.Select(a => (a.Key.Item1, a.Value))); // Forward 1st order support
                    if (ambiguous_position > 0) order = order.Concat(ambiguous[ambiguous_position - 1].Support.Select(a => (a.Key.Item2, a.Value))); // backward 1st order support

                    // Deduplicate order
                    var sorted_order = order.GroupBy(a => a.Item1).Select(a => (a.Key, a.Select(i => i.Item2).Sum())).ToList();
                    sorted_order.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                    var local_max_support = Math.Max(1, sorted_order.Select(i => i.Item2).Max());

                    html.OpenAndClose(HtmlTag.p, "class='title'", "1st order support");
                    html.OpenAndClose(HtmlTag.p, "class='title'", "Higher order graph");
                    foreach (var variant in sorted_order) {
                        var graph = graphs.Find(a => a.Variant == variant.Item1);
                        html.OpenAndClose(HtmlTag.div, $"class='bar' style='width:{variant.Item2 / local_max_support:P}'");
                        html.Add(graph.Graph);
                    }
                    html.Close(HtmlTag.div);
                }
                html.Close(HtmlTag.div);
            }

            html.Close(HtmlTag.div);
            return html;
        }

        private static (SvgBuilder Graph, int BackwardLength) RenderAmbiguityTree((AmbiguityTreeNode Backward, AmbiguityTreeNode Forward) root) {
            var svg = new SvgBuilder();

            AmbiguityTreeNode[][] GetLevels(AmbiguityTreeNode root) {
                var levels = new List<List<(AmbiguityTreeNode Node, double Ordering)>>() { new List<(AmbiguityTreeNode, double)>() { (root, root.TotalIntensity()) } };
                var to_scan = new Stack<(int Level, AmbiguityTreeNode Node)>();
                var already_placed = new HashSet<AmbiguityTreeNode>();
                to_scan.Push((0, root));

                while (to_scan.Count > 0) {
                    var element = to_scan.Pop();
                    foreach (var child in element.Node.Connections) {
                        if (already_placed.Contains(child.Next)) continue;
                        already_placed.Add(child.Next);

                        while (levels.Count < element.Level + 2) {
                            levels.Add(new List<(AmbiguityTreeNode, double)>());
                        }
                        to_scan.Push((element.Level + 1, child.Next));
                        levels[element.Level + 1].Add((child.Next, element.Node.TotalIntensity() + child.Next.TotalIntensity()));
                    }
                }

                foreach (var level in levels)
                    level.Sort((a, b) => b.Ordering.CompareTo(a.Ordering));

                return levels.Select(level => level.Select(node => node.Node).ToArray()).ToArray();
            }

            var backward_levels = GetLevels(root.Backward);
            var forward_levels = GetLevels(root.Forward);
            backward_levels = backward_levels.Reverse().ToArray();
            var levels = backward_levels.Concat(forward_levels.Skip(1)).ToArray(); // Skip the first level of the forward_levels because it contains the same node (from a human perspective) as the last from backwards_levels (the root).

            const int item_width = 80; // Total width of a level (option + arrow)
            const int item_height = 20; // Height of an option
            const int padding = 10; // Padding top and option char width
            const int clearing = 2; // Clearing around the option
            var max_options = levels.Max(l => l.Length);
            var w = (levels.Length - 1) * item_width + padding * 2;
            var h = max_options * item_height + padding;
            var max_intensity = levels.Max(level => level.Max((node) => node.Connections.Count > 0 ? node.Connections.Max(connection => connection.Intensity) : 0));
            svg.Open(SvgTag.svg, $"viewBox='0 0 {w} {h}' width='{w}px' height='{h}px' style='--backward-length:{backward_levels.Count()};'");

            // Save the position of all nodes
            var position = new Dictionary<AmbiguityTreeNode, (int X, int Y)>();
            for (int level = 0; level < levels.Length; level++) {
                var vertical_padding = max_options - levels[level].Length;
                for (int node = 0; node < levels[level].Length; node++) {
                    var x = level * item_width;
                    var y = node * item_height + padding + padding / 2 + vertical_padding * item_height / 2;
                    if (!position.ContainsKey(levels[level][node])) position.Add(levels[level][node], (x, y));
                    var classes = "";
                    if (levels[level][node].Equals(root.Backward)) {
                        classes = " root";
                        position.Add(root.Forward, (x, y)); // Forward is skipped but is at the same position as backward
                        svg.OpenAndClose(SvgTag.rect, $"class='root' x='{x - clearing * 2}px' y='{y - padding - padding / 2}px' width='{item_height}px' height='{item_height}px'");
                    }
                    svg.OpenAndClose(SvgTag.text, $"class='option{classes}' x='{x}px' y='{y}px'", levels[level][node].Variant.Character.ToString());
                }
            }

            // Go over all connections and draw the arrows. Add the first level of the forward_levels back in to keep those connections (it does not matter in which order these arrows are generated).
            Array.ForEach(levels.Concat(forward_levels.Take(1)).ToArray(), level => Array.ForEach(level, (node) =>
            node.Connections.ForEach(connection => {
                var pos1 = position[node];
                var pos2 = position[connection.Next];
                if (pos2.X < pos1.X) {
                    var c = pos1;
                    pos1 = pos2;
                    pos2 = c;
                }
                svg.OpenAndClose(SvgTag.line, $"class='support-arrow' x1='{pos1.X + padding + clearing}px' y1='{pos1.Y - padding / 2}px' x2='{pos2.X - clearing}px' y2='{pos2.Y - padding / 2}px' style='stroke-width:{connection.Intensity / max_intensity * 10}px'");
            })));

            svg.Close(SvgTag.svg);

            return (svg, backward_levels.Length);
        }
    }
}