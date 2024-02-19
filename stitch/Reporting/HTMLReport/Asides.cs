using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using HeckLib.ConvenienceInterfaces.SpectrumMatch;
using HtmlGenerator;
using Stitch;
using static HTMLNameSpace.CommonPieces;
using static Stitch.Fragmentation;
using static Stitch.HelperFunctionality;

namespace HTMLNameSpace {
    public static class HTMLAsides {
        const char gap_char = '-';
        const char non_breaking_space = '\u00A0'; // &nbsp; in html

        /// <summary> Returns an aside for details viewing of a read. </summary>
        public static HtmlBuilder CreateReadAside(ReadFormat.General MetaData, ReadOnlyCollection<(string, List<Segment>)> segments, ReadOnlyCollection<Segment> recombined, string AssetsFolderName) {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, $"id='{GetAsideIdentifier(MetaData)}' class='info-block read-info'");
            html.OpenAndClose(HtmlTag.h1, "", "Read " + GetAsideIdentifier(MetaData, true));
            html.OpenAndClose(HtmlTag.h2, "", $"Sequence (length={MetaData.Sequence.Length})");
            html.OpenAndClose(HtmlTag.p, "class='aside-seq'", AminoAcid.ArrayToString(MetaData.Sequence.AminoAcids));

            if (MetaData.SupportingSpectra.Count() > 0) {
                for (var i = 0; i < MetaData.SupportingSpectra.Count; i++) {
                    var spectrum = MetaData.SupportingSpectra[i];
                    html.Add(spectrum.ToHtml(MetaData, i));
                }
            }

            // Search for all position this read was placed
            var locations = segments.SelectMany(group => group.Item2.SelectMany(segment => segment.Templates.SelectMany(template => template.Matches.Where(match => match.ReadB.Identifier == MetaData.Identifier).Select(match => (group, segment, template, match)))));
            html.TagWithHelp(HtmlTag.h2, "Reverse Lookup", new HtmlBuilder(HTMLHelp.ReadLookup));
            if (locations.Count() > 0) {
                html.Open(HtmlTag.table, "class='wide-table'");
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.th, "", "Group");
                html.OpenAndClose(HtmlTag.th, "", "Segment");
                html.OpenAndClose(HtmlTag.th, "", "Template");
                html.OpenAndClose(HtmlTag.th, "", "Template Part");
                html.OpenAndClose(HtmlTag.th, "", "Read Part");
                html.OpenAndClose(HtmlTag.th, "", "Score");
                html.OpenAndClose(HtmlTag.th, "", "Unique");
                html.Close(HtmlTag.tr);

                foreach (var location in locations) {
                    html.Open(HtmlTag.tr);
                    html.OpenAndClose(HtmlTag.td, "class='center'", location.group.Item1);
                    html.OpenAndClose(HtmlTag.td, "class='center'", location.segment.Name);
                    html.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(location.template.MetaData, AsideType.Template, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(MetaData)));
                    html.OpenAndClose(HtmlTag.td, "class='center'", $"[{location.match.StartA}..{location.match.StartA + location.match.LenA}]");
                    html.OpenAndClose(HtmlTag.td, "class='center'", $"[{location.match.StartB}..{location.match.StartB + location.match.LenB}]");
                    html.OpenAndClose(HtmlTag.td, "class='center'", location.match.Score.ToString());
                    html.OpenAndClose(HtmlTag.td, "class='center'", location.match.Unique.ToString());
                    html.Close(HtmlTag.tr);
                }
                html.Close(HtmlTag.table);
            } else {
                html.OpenAndClose(HtmlTag.p, "class='message'", "Not placed on any template.");
            }

            // Search for all positions on recombined templates this read was placed
            if (recombined != null && recombined.Count > 0) {
                var recombined_locations = recombined.SelectMany(segment => segment.Templates.SelectMany(template => template.Matches.Where(match => match.ReadB.Identifier == MetaData.Identifier).Select(match => (template, match))));
                if (recombined_locations.Count() > 0) {
                    html.Open(HtmlTag.table, "class='wide-table'");
                    html.Open(HtmlTag.tr);
                    html.OpenAndClose(HtmlTag.th, "", "Recombined");
                    html.OpenAndClose(HtmlTag.th, "", "Template Part");
                    html.OpenAndClose(HtmlTag.th, "", "Read Part");
                    html.OpenAndClose(HtmlTag.th, "", "Score");
                    html.OpenAndClose(HtmlTag.th, "", "Unique");
                    html.Close(HtmlTag.tr);
                    foreach (var location in recombined_locations) {
                        html.Open(HtmlTag.tr);
                        html.OpenAndClose(HtmlTag.td, "class='center'", GetAsideLinkHtml(location.template.MetaData, AsideType.RecombinedTemplate, AssetsFolderName, new List<string> { "report-monoclonal", "reads" }, "aligned-" + GetAsideIdentifier(MetaData)));
                        html.OpenAndClose(HtmlTag.td, "class='center'", $"[{location.match.StartA}..{location.match.StartA + location.match.LenA}]");
                        html.OpenAndClose(HtmlTag.td, "class='center'", $"[{location.match.StartB}..{location.match.StartB + location.match.LenB}]");
                        html.OpenAndClose(HtmlTag.td, "class='center'", location.match.Score.ToString());
                        html.OpenAndClose(HtmlTag.td, "class='center'", location.match.Unique.ToString());
                        html.Close(HtmlTag.tr);
                    }
                    html.Close(HtmlTag.table);
                }
            } else {
                html.OpenAndClose(HtmlTag.p, "class='message'", "Not placed on any recombined template.");
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
            var location = new Location(new List<string>() { AssetsFolderName, GetAsideName(type) + "s" }, AssetsFolderName);
            bool displayArea = template.TotalArea != 0 || template.TotalUniqueArea != 0;

            var (consensus_sequence, consensus_doc) = template.ConsensusSequence();
            var based = new HtmlBuilder();
            var title = "Segment";
            switch (type) {
                case AsideType.RecombinedTemplate:
                    based = CreateRecombinationOrder(template, type, location);
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
            (var alignment, var gaps) = CreateTemplateAlignment(template, id, location, displayArea);
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
            if (displayArea) html.TagWithHelp(HtmlTag.th, "Total Area", new HtmlBuilder(HTMLHelp.TemplateTotalArea.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Unique Score", new HtmlBuilder(HTMLHelp.TemplateUniqueScore.ToString()), "small-cell");
            html.TagWithHelp(HtmlTag.th, "Unique Matches", new HtmlBuilder(HTMLHelp.TemplateUniqueMatches.ToString()), "small-cell");
            if (displayArea) html.TagWithHelp(HtmlTag.th, "Unique Area", new HtmlBuilder(HTMLHelp.TemplateUniqueArea.ToString()), "small-cell");
            html.Close(HtmlTag.tr);
            html.Open(HtmlTag.tr);
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Sequence.Length.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Score.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.Matches.Count.ToString("G4"));
            if (displayArea) html.OpenAndClose(HtmlTag.td, "class='center'", template.TotalArea.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.UniqueScore.ToString("G4"));
            html.OpenAndClose(HtmlTag.td, "class='center'", template.UniqueMatches.ToString("G4"));
            if (displayArea) html.OpenAndClose(HtmlTag.td, "class='center'", template.TotalUniqueArea.ToString("G4"));
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

        private static HtmlBuilder CreateRecombinationOrder(Template template, AsideType type, Location location) {
            var based = new HtmlBuilder();
            if (template.Recombination != null) {
                var first = true;
                var order = template.Recombination.Aggregate(
                    new HtmlBuilder(),
                    (acc, seg) => {
                        if (first) first = false;
                        else acc.Content(" → ");
                        acc.Add(GetAsideLinkHtml(seg.MetaData, AsideType.Template, location));
                        return acc;
                    }
                );
                based.OpenAndClose(HtmlTag.h2, "", "Order");
                based.OpenAndClose(HtmlTag.p, "", order);
            }
            return based;
        }

        static HtmlBuilder CreateAnnotatedSequence(string id, Template template) {
            // Create an overview of the alignment from consensus with germline.
            // Also highlight differences and IMGT regions

            // HERECOMESTHECONSENSUSSEQUENCE  (coloured to IMGT region)
            // HERECOMESTHEGERMLINE.SEQUENCE
            //             CONSENSUS          (differences)
            var annotated = template.ConsensusSequenceAnnotation();
            var match = template.AlignConsensusWithTemplate();
            var columns = new List<(string Template, string Query, string Difference, Annotation Class)>();
            var data_buffer = new StringBuilder();
            var html = new HtmlBuilder();

            int pos_a = match.StartA;
            int pos_b = match.StartB;

            foreach (var piece in match.Path) {
                var a = AminoAcid.ArrayToString(match.ReadA.Sequence.AminoAcids.SubArray(pos_a, piece.StepA));
                var b = AminoAcid.ArrayToString(match.ReadB.Sequence.AminoAcids.SubArray(pos_b, piece.StepB));
                if (!(String.IsNullOrEmpty(a) && b == "X")) {
                    columns.Add((
                        a.Length == 0 ? template.Parent.Alphabet.GapChar.ToString() : a,
                        b.Length == 0 ? template.Parent.Alphabet.GapChar.ToString() : b,
                        a == b ? new string(non_breaking_space, piece.StepA) : b,
                        pos_b >= annotated.Length ? Annotation.None : annotated[pos_b]));
                }
                pos_a += piece.StepA;
                pos_b += piece.StepB;
            }
            //html.OpenAndClose(HtmlTag.pre, "style='white-space: pre-wrap'", string.Join(", ", annotated.Zip(template.ConsensusSequence().Item1).Select((an, i) => { var a = an.First == Annotation.None ? "" : $" {an.First}"; return $"{i}: {AminoAcid.ArrayToString(an.Second.Sequence)}{a}"; })));
            //html.OpenAndClose(HtmlTag.pre, "", match.Summary());
            html.Open(HtmlTag.div, "class='annotated-consensus-sequence'");
            html.UnsafeContent(CommonPieces.TagWithHelp("h2", "Annotated Consensus Sequence", HTMLHelp.AnnotatedConsensusSequence.ToString()));
            html.UnsafeContent(CommonPieces.CopyData("Annotated Consensus Sequence (TXT)"));
            html.Open(HtmlTag.div, "class='annotated'");
            html.Open(HtmlTag.div, "class='names'");
            html.OpenAndClose(HtmlTag.span, "", "Consensus");
            html.OpenAndClose(HtmlTag.span, "", "Template");
            html.Close(HtmlTag.div);

            var present = new HashSet<Annotation>();
            foreach (var column in columns) {
                if (column.Template == "X" && (column.Query == template.Parent.Alphabet.GapChar.ToString() || column.Query == "X")) continue;
                html.Open(HtmlTag.div, $"class='{column.Class}'");
                if (column.Class.IsAnyCDR() && !present.Contains(column.Class)) {
                    present.Add(column.Class);
                    html.OpenAndClose(HtmlTag.span, "class='title'", column.Class.ToString());
                }
                html.OpenAndClose(HtmlTag.span, "", column.Query.ToString());
                html.OpenAndClose(HtmlTag.span, "", column.Template.ToString());
                html.OpenAndClose(HtmlTag.span, "class='dif'", column.Difference.ToString());
                html.Close(HtmlTag.div);
            }
            html.Close(HtmlTag.div);
            if (present.Count > 1) {
                html.Open(HtmlTag.div, "class='annotated legend'");
                html.OpenAndClose(HtmlTag.p, "class='names'", "Legend");
                html.OpenAndClose(HtmlTag.span, "class='CDR'", "CDR");
                html.OpenAndClose(HtmlTag.span, "class='Conserved'", "Conserved");
                html.OpenAndClose(HtmlTag.span, "class='Glycosylationsite'", "Possible glycosylation site");
                html.Close(HtmlTag.div);
            }
            html.Open(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'");
            var (c, g, d) = columns.Aggregate(("", "", ""), (acc, c) => (acc.Item1 + c.Template, acc.Item2 + c.Query, acc.Item3 + c.Difference));
            html.Content($"Consensus  {c}\nTemplate   {g}\nDifference {d}");
            html.Close(HtmlTag.textarea);
            html.Close(HtmlTag.div);
            return html;
        }

        static public (HtmlBuilder, int[]) CreateTemplateAlignment(Template template, string id, Location location, bool displayArea) {
            //var alignedSequences = template.AlignedSequences();
            var placed_ids = new HashSet<string>(); // To make sure to only give the first align-link its ID
            var html = new HtmlBuilder();
            var gaps = template.Gaps();

            if (template.Matches.Count == 0)
                return (html, gaps);

            html.Open(HtmlTag.div, "class='alignment'");
            html.TagWithHelp(HtmlTag.h2, "Alignment", new HtmlBuilder(HTMLHelp.ReadsAlignment));
            html.CopyData("Reads Alignment (FASTA)", new HtmlBuilder(HTMLHelp.ReadsAlignmentData));

            var depthOfCoverage = new List<double>();
            var data_buffer = new StringBuilder();
            var consensus = template.ConsensusSequence();

            var localMatches = template.Matches.ToList();
            localMatches.Sort((a, b) => b.LenA.CompareTo(a.LenA)); // Try to keep the longest matches at the top.
            // Figure out the maximal size of the insertion before the template starts
            var max_start_insertion = localMatches.Aggregate(0, (acc, item) => Math.Max(acc, item.StartA == 1 ? item.Path.TakeWhile(a => a.StepA == 0).Select(a => (int)a.StepB).Sum() : 0));
            var total_length = max_start_insertion + gaps.Sum() + template.Sequence.Length + 1;
            data_buffer.AppendLine($">{template.MetaData.EscapedIdentifier} template\n{new string('.', max_start_insertion)}{AminoAcid.ArrayToString(consensus.Item1.SelectMany(i => i.Sequence)).Replace(gap_char, '.')}");

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
                        for (int c = 0; c < gaps[pos]; c++) // Add the correct number of annotations for bigger gaps
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
            for (int i = block_size; i < gaps.Length - block_size; i += block_size) {
                var i_string = i.ToString();
                numbering.Append(new string(non_breaking_space, block_size - last_size + gaps.SubArray(i - block_size, block_size).Sum()));
                numbering.Append(i_string);
                last_size = i_string.Length;
            }

            html.Open(HtmlTag.div, "class='buttons'");
            if (annotatedSequence.Any(a => a.IsAnyCDR())) html.OpenAndClose(HtmlTag.button, "onclick='ToggleCDRReads()'", "Only show CDR reads");
            html.OpenAndClose(HtmlTag.button, "onclick='ToggleAlignmentComic()'", "Show as comic");
            html.Close(HtmlTag.div);
            html.Open(HtmlTag.div, "class='alignment-wrapper'");
            html.Open(HtmlTag.div, $"class='alignment-body' style='grid-template-columns:repeat({total_length}, 1ch);{TemplateAlignmentAnnotation(annotatedSequence)}'");
            html.OpenAndClose(HtmlTag.div, $"class='numbering'", numbering.ToString());
            html.OpenAndClose(HtmlTag.div, $"class='template'", new string(non_breaking_space, max_start_insertion) + template_sequence);

            var ambiguous = template.SequenceAmbiguityAnalysis();
            for (var alignment_index = 0; alignment_index < localMatches.Count; alignment_index++) {
                var alignment = localMatches[alignment_index];
                CreateSingleReadAlignment(alignment, template.Parent.Alphabet.Swap, location, html, gaps, data_buffer, max_start_insertion, total_length, annotatedSequence, ambiguous);
            }

            html.Close(HtmlTag.div);
            html.Close(HtmlTag.div);

            // Index menus
            html.Open(HtmlTag.div, "id='index-menus'");
            for (var i = 0; i < template.Matches.Count; i++) {
                html.Add(AlignmentDetails(template.Matches[i], template, displayArea));
            }
            html.Close(HtmlTag.div);
            html.OpenAndClose(HtmlTag.textarea, "class='graph-data hidden' aria-hidden='true'", data_buffer.ToString());
            html.Close(HtmlTag.div);
            return (html, gaps);
        }

        private static void CreateSingleReadAlignment(Alignment alignment, int SwapScore, Location location, HtmlBuilder html, int[] gaps, StringBuilder data_buffer, int max_start_insertion, int total_length, Annotation[] annotatedSequence, AmbiguityNode[] ambiguous) {
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

            var start = Math.Max(1, 1 + alignment.StartA + gaps.SubArray(0, alignment.StartA + 1).Sum() - len_start + max_start_insertion);
            var end = start + alignment.LenA + gaps.SubArray(alignment.StartA, alignment.LenA).Sum() + 1 + len_start + len_end;
            html.Open(
                HtmlTag.a,
                $"{classes_string}id='aligned-{GetAsideIdentifier(alignment.ReadB)}' href='{CommonPieces.GetAsideRawLink(alignment.ReadB, AsideType.Read, location)}' target='_blank' style='grid-column:{start} / {end};' onmouseover='AlignmentDetails(\"{alignment.ReadB.EscapedIdentifier}\")' onmouseout='AlignmentDetailsClear()'");
            var pos_a = alignment.StartA;
            var pos_b = alignment.StartB;
            var inserted = 0;
            var seq = new StringBuilder();
            foreach (var piece in alignment.Path) {
                var content = AminoAcid.ArrayToString(alignment.ReadB.Sequence.AminoAcids.SubArray(pos_b, piece.StepB));
                var total_gaps = piece.StepA == 0 ? 0 : gaps.SubArray(pos_a, piece.StepA).Sum();
                var positional_gap_char = pos_a == alignment.StartA ? non_breaking_space : gap_char;
                if (piece.StepA == piece.StepB && piece.StepA > 1 && SwapScore != 0 && piece.LocalScore == piece.StepA * SwapScore) {
                    if (inserted > total_gaps) inserted = total_gaps;
                    // Display swaps with internal gaps
                    var already_inserted = gaps[pos_a];
                    if (pos_a != alignment.StartA && gaps[pos_a] > 0) {
                        html.OpenAndClose(HtmlTag.span, "class='template-gap'", new string(positional_gap_char, gaps[pos_a]));
                        seq.Append(new string(positional_gap_char, gaps[pos_a]));
                    }

                    var insert_size = piece.StepB + total_gaps - inserted - already_inserted;
                    var template_size = piece.StepA + total_gaps - already_inserted;
                    html.Open(HtmlTag.span, $"class='swap' style='--i:{insert_size};--w:{template_size};'");
                    for (int i = 0; i < piece.StepA; i++) {
                        if (i != 0) {
                            html.Content(new string(gap_char, gaps[pos_a + i]));
                            seq.Append(new string(gap_char, gaps[pos_a + i]));
                        }
                        html.Content(content[i].ToString());
                        seq.Append(content[i].ToString());
                    }
                    html.Close(HtmlTag.span);
                    inserted = 0;
                } else if (piece.StepA == 0) {
                    if (pos_a == 0 && pos_b == 0) {
                        html.Content(new string(non_breaking_space, Math.Max(0, gaps[pos_a] - len_start)));
                        seq.Append(new string(non_breaking_space, Math.Max(0, gaps[pos_a] - len_start)));
                        inserted += Math.Max(0, gaps[pos_a] - len_start);
                    }
                    if (pos_a == alignment.StartA) html.Open(HtmlTag.span, "class='insertion'");
                    html.Content(AminoAcid.ArrayToString(alignment.ReadB.Sequence.AminoAcids.SubArray(pos_b, piece.StepB)));
                    if (pos_a == alignment.StartA) html.Close(HtmlTag.span);
                    seq.Append(AminoAcid.ArrayToString(alignment.ReadB.Sequence.AminoAcids.SubArray(pos_b, piece.StepB)));
                    inserted += piece.StepB;
                } else if (piece.StepB == 0) {
                    if (inserted > total_gaps) inserted = total_gaps;
                    if (total_gaps - inserted > 0) {
                        html.OpenAndClose(HtmlTag.span, "class='template-gap'", new string(positional_gap_char, total_gaps - inserted));
                    }
                    html.Content(new string(positional_gap_char, piece.StepA));
                    seq.Append(new string(positional_gap_char, piece.StepA + total_gaps - inserted));
                    inserted = 0;
                } else if (piece.StepA != piece.StepB) {
                    // Display unequal sets stretched (or squashed) including all gaps
                    if (inserted > total_gaps) inserted = total_gaps;
                    if (pos_a == alignment.StartA) total_gaps -= gaps[pos_a];
                    var already_inserted = gaps[pos_a]; // Gaps before the start of this set
                    if (total_gaps - inserted > 0) {
                        html.OpenAndClose(HtmlTag.span, "class='template-gap'", new string(positional_gap_char, total_gaps - inserted));
                    }
                    html.OpenAndClose(HtmlTag.span, $"style='--i:{piece.StepB};--w:{piece.StepA + total_gaps - already_inserted};'", content);
                    seq.Append(new string(positional_gap_char, already_inserted) + content);
                    inserted = 0;
                } else {
                    if (inserted > total_gaps) inserted = total_gaps;
                    if (pos_a != alignment.StartA && total_gaps - inserted > 0) {
                        html.OpenAndClose(HtmlTag.span, "class='template-gap'", new string(positional_gap_char, total_gaps - inserted));
                        seq.Append(new string(gap_char, total_gaps - inserted));
                    }
                    html.Content(content);
                    seq.Append(content);
                    inserted = 0;
                }
                pos_a += piece.StepA;
                pos_b += piece.StepB;
            }
            html.Close(HtmlTag.a);
            data_buffer.AppendLine($">{alignment.ReadB.EscapedIdentifier} score:{alignment.Score} alignment:{alignment.VeryShortPath()} unique:{alignment.Unique}\n{new string('~', start)}{seq.ToString().Replace(gap_char, '.')}{new string('~', Math.Max(total_length - end, 0))}");
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

        static HtmlBuilder AlignmentDetails(Alignment match, Template template, bool displayArea) {
            var doc_title = "Positional Score";
            var type = "Read";
            var html = new HtmlBuilder();

            void Row(string name, string content) {
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.td, "", name);
                html.OpenAndClose(HtmlTag.td, "", content);
                html.Close(HtmlTag.tr);
            }

            void RowHtml(string name, string header, HtmlBuilder content) {
                html.Open(HtmlTag.tr);
                html.OpenAndClose(HtmlTag.td, "", name);
                html.OpenAndClose(HtmlTag.td, header, content);
                html.Close(HtmlTag.tr);
            }

            html.Open(HtmlTag.div, $"class='alignment-details' id='alignment-details-{match.ReadB.EscapedIdentifier}'");
            html.OpenAndClose(HtmlTag.h4, "", match.ReadB.Identifier);
            html.Open(HtmlTag.table);
            Row("Type", type);
            Row("Score", match.Score.ToString());
            if (displayArea) Row("Total area", match.ReadB.TotalArea.ToString("G4"));
            Row("Start on Template", match.StartA.ToString());
            Row("Length on Template", match.LenA.ToString());
            Row($"Start on {type}", match.StartB.ToString());
            Row($"Length of {type}", match.LenB.ToString());
            if (template.ForcedOnSingleTemplate) Row("Unique", match.Unique ? "Yes" : "No");
            if (match.ReadB is ReadFormat.Peaks p) Row("Peaks ALC", p.DeNovoScore.ToString());

            if (match.ReadB.Sequence.PositionalScore.Length != 0)
                RowHtml(doc_title, "class='doc-plot'", HTMLGraph.Bargraph(HTMLGraph.AnnotateDOCData(match.ReadB.Sequence.PositionalScore.SubArray(match.StartB, match.LenB).Select(a => (double)a).ToList(), match.StartB, true)));

            RowHtml("Alignment graphic", "class='sequence-match-graphic'", SequenceMatchGraphic(match, template.Parent.Alphabet.Swap));
            html.Close(HtmlTag.table);
            html.Close(HtmlTag.div);
            return html;
        }

        static HtmlBuilder SequenceMatchGraphic(Alignment match, int SwapScore) {
            var id = "none";
            var html = new HtmlBuilder();
            foreach (var piece in match.Path) {
                if (piece.StepA == 0) {
                    id = "gap-in-template";
                } else if (piece.StepB == 0) {
                    id = "gap-in-query";
                } else if (piece.StepA != piece.StepB) {
                    id = "set";
                } else if (SwapScore != 0 && piece.LocalScore == piece.StepA * SwapScore) {
                    id = "swap";
                } else {
                    id = "match";
                }

                html.OpenAndClose(HtmlTag.span, $"class='{id}' style='flex-grow:{Math.Max(piece.StepA, piece.StepB)}'");
            }
            return html;
        }

        static HtmlBuilder SequenceConsensusOverview(Template template, string title = null, HtmlBuilder help = null) {
            var combined_sequence = template.CombinedSequence();
            var consensus = template.ConsensusSequence();
            var diversity = new List<Dictionary<(string, int), double>>(consensus.Item1.Count);

            for (int i = 0; i < combined_sequence.Count; i++) {
                var items = combined_sequence[i].AminoAcids.ToDictionary(
                    item => (AminoAcid.ArrayToString(item.Key.Sequence), item.Key.Length),
                    item => item.Value);

                // If this is an empty place just ignore it
                if (!(items.Count == 1 && items.Keys.All(k => k.Item1.Length == 0)))
                    diversity.Add(items);

                var max = ((Template.IGap)new Template.None(), -1);
                var gaps = combined_sequence[i].Gaps.ToDictionary(
                    item => item.Key == (Template.IGap)new Template.None() ? ("~", 1) : (item.Key.ToString(), item.Key.ToString().Length),
                    item => {
                        if (item.Value.Count > max.Item2) {
                            max = (item.Key, item.Value.Count);
                        }
                        return (double)item.Value.Count;
                    });

                // Add the gaps, if they score more than the empty one (so if they are included in the consensus sequence)
                if (max.Item2 >= 1 && max.Item1.GetType() != typeof(Template.None))
                    diversity.Add(gaps);
            }
            var html = new HtmlBuilder();
            //html.OpenAndClose(HtmlTag.pre, "style='white-space: pre-wrap'", string.Join(", ", diversity.Zip(template.ConsensusSequenceAnnotation()).Select((an, i) => $"{i}: {string.Join(';', an.First.Select((key) => $"{key.Key}=>{key.Value}"))} {an.Second}")));
            html.Add(HTMLTables.SequenceConsensusOverview(diversity, title, help, template.ConsensusSequenceAnnotation(), template.SequenceAmbiguityAnalysis().Select(a => a.Position).ToArray(), template.Gaps()));
            return html;
        }

        static HtmlBuilder SequenceAmbiguityOverview(Template template, int[] gaps) {
            var html = new HtmlBuilder();
            html.Open(HtmlTag.div, "class='ambiguity-overview'");
            html.TagWithHelp(HtmlTag.h2, "Variant Graph", new HtmlBuilder(HTMLHelp.AmbiguityOverview.Replace("{threshold}", Template.AmbiguityThreshold.ToString("P0"))));

            var ambiguous = template.SequenceAmbiguityAnalysis();

            if (ambiguous.Length == 0 || ambiguous.All(n => n.Support.Count == 0)) {
                html.OpenAndClose(HtmlTag.i, "", "No ambiguous positions for analysis.");
            } else {
                var max_support = ambiguous.SelectMany(n => n.Support.Values).Max();

                // Find the position of each aminoacid node by determining the total support for that AA
                // at that position and sorting on highest at the top.
                var placed = new List<(AminoAcid, double)>[ambiguous.Length];
                for (var i = 0; i < placed.Length; i++) placed[i] = new();

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
                html.TagWithHelp(HtmlTag.h2, "Higher Order Variant Graph", new HtmlBuilder(HTMLHelp.HigherOrderAmbiguityGraph));
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