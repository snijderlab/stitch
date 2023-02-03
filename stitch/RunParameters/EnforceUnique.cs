using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Stitch {
    public static class EnforceUnique {
        /// <summary>
        /// EnforceUnique of the given matches. In place so all non-uniquely placed entries will be removed where applicable.
        /// </summary>
        /// <param name="matches"> All matches. </param>
        /// <param name="unique_threshold"> The threshold for being considered unique, as fraction of the max score. </param>
        /// <param name="localised"> Whether the unique placement should be done considering localised placement. </param>
        public static void Enforce(List<List<(int GroupIndex, int SegmentIndex, int TemplateIndex, Alignment Match)>> matches, double unique_threshold, bool localised) {
            if (matches == null || unique_threshold == 0.0) return;
            if (localised) {
                foreach (var read_matches in matches) {
                    var transformed = read_matches.Select(i => (i.Item1, i.Item2, i.Item3, i.Item4, GetSpan(i.Item4)));
                    var parts = new List<(int, int)> { (0, read_matches[0].Match.ReadB.Sequence.Length) };
                    var placed = Localised(transformed, unique_threshold, new(), parts, new());
                    read_matches.Clear();
                    read_matches.AddRange(placed);
                }
            } else {
                General(matches, unique_threshold);
            }
        }

        /// <summary> Filter matches based on the unique threshold. </summary>
        /// <param name="matches"> All matches as a List per Read per Template. </param>
        static void General(List<List<(int GroupIndex, int SegmentIndex, int TemplateIndex, Alignment Match)>> matches, double unique_threshold) {
            for (int read_index = 0; read_index < matches.Count; read_index++) {
                var best = new List<(int, int, int, Alignment Match)>();
                var best_score = 0;
                if (matches[read_index] == null) continue;
                for (int template_index = 0; template_index < matches[read_index].Count; template_index++) {
                    var match = matches[read_index][template_index];
                    if (match.Match.Score > best_score) {
                        best = best.Where(m => m.Match.Score >= match.Match.Score * unique_threshold).ToList();
                        best.Add(match);
                        best_score = match.Match.Score;
                    } else if (match.Match.Score >= best_score * unique_threshold) {
                        best.Add(match);
                    }
                }
                if (best.Count == 1) best[0].Item4.Unique = true;
                matches[read_index].Clear();
                matches[read_index].AddRange(best);
            }
        }

        /// <summary> Filter matches for a single read based on the unique threshold and the already placed parts. </summary>
        static List<(int, int SegmentIndex, int, Alignment Match)> Localised(IEnumerable<(int GroupIndex, int SegmentIndex, int TemplateIndex, Alignment Match, (int Start, int End) Span)> matches, double unique_threshold, HashSet<(int, int)> placed_segments, List<(int Start, int End)> open_parts, List<(int, int SegmentIndex, int, Alignment Match)> placed) {
            if (matches.Count() == 0) return placed;

            var best = new List<(int GroupIndex, int SegmentIndex, int, Alignment Match, (int Start, int End) Span)>();
            var best_score = 0;

            foreach (var match in matches) {
                if (match.Match.Score > best_score) {
                    best_score = match.Match.Score;
                    best = best.Where(m => m.Match.Score >= best_score * unique_threshold).ToList();
                    best.Add(match);
                } else if (match.Match.Score >= best_score * unique_threshold) {
                    best.Add(match);
                }
            }

            if (best.Count == 1) best[0].Match.Unique = true;
            for (var i = 0; i < best.Count; i++) {
                var item = best[i];
                placed_segments.Add((item.GroupIndex, item.SegmentIndex));
                UpdateOpenParts(item.Span, open_parts);
                placed.Add((item.Item1, item.Item2, item.Item3, item.Item4));
            }

            // Redo the analysis but now with this segment(s) and locations of the reads off limits
            return Localised(matches.Where(match => !placed_segments.Contains((match.GroupIndex, match.SegmentIndex)) && Fits(match.Span, open_parts)), unique_threshold, placed_segments, open_parts, placed);
        }

        /// <summary>
        /// Check if the given span fits in any of the open parts left os this read. If it fits this means that this match can be chosen without any overlap with any uniquely picked matches before.
        /// </summary>
        public static bool Fits((int Start, int End) Span, List<(int Start, int End)> open_parts) {
            foreach (var place in open_parts) {
                if (place.Start <= Span.Start && place.End >= Span.End)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Update the open parts of a read in place given a new placed span.
        /// </summary>
        public static void UpdateOpenParts((int Start, int End) Span, List<(int Start, int End)> open_parts) {
            var output = new List<(int, int)>(open_parts.Count);
            foreach (var place in open_parts) {
                if (place.End > Span.Start && place.Start < Span.End) {
                    if (Span.Start - place.Start >= 5) {
                        output.Add((place.Start, Span.Start));
                    }
                    if (place.End - Span.End >= 5) {
                        output.Add((Span.End, place.End));
                    }
                } else {
                    output.Add(place);
                }
            }
            open_parts.Clear();
            open_parts.AddRange(output);
        }

        /// <summary>
        /// Get the region of a read that this match spans. Taking care to adjust for Xs on the template (these could overlap with adjacent segments).
        /// </summary>
        static (int Start, int End) GetSpan(Alignment match) {
            var start = match.StartB;
            if (match.StartA == 0) {
                var count_x = match.ReadA.Sequence.AminoAcids.TakeWhile(a => a.Character == 'X').Count();
                var insertions = Math.Min(count_x, match.Path.TakeWhile(i => i.StepB == 0).Select(i => (int)i.StepA).Sum());
                start = start + count_x - insertions;
            }
            var end = match.StartB + match.LenB;
            if (match.StartA + match.LenA == match.ReadA.Sequence.Length) {
                var count_x = match.ReadA.Sequence.AminoAcids.Reverse().TakeWhile(a => a.Character == 'X').Count();
                var insertions = Math.Min(count_x, match.Path.AsEnumerable().Reverse().TakeWhile(i => i.StepB == 0).Select(i => (int)i.StepA).Sum());
                end = end - count_x;
            }

            return (start, end);
        }
    }
}