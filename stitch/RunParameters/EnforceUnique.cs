using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Stitch {
    public static class EnforceUnique {
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

            return Localised(matches.Where(match => !placed_segments.Contains((match.GroupIndex, match.SegmentIndex)) && Fits(match.Span, open_parts)), unique_threshold, placed_segments, open_parts, placed);
        }

        public static bool Fits((int Start, int End) Span, List<(int Start, int End)> open_parts) {
            foreach (var place in open_parts) {
                if (place.Start <= Span.Start && place.End >= Span.End)
                    return true;
            }
            return false;
        }

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
            //return output;
        }

        static (int Start, int End) GetSpan(Alignment match) {
            if (match.ReadA.Sequence.Length == match.StartB + match.LenB) {
                var count_x = match.ReadA.Sequence.AminoAcids.Reverse().TakeWhile(a => a.Character == 'X').Count();
                return (match.StartB, Math.Max(match.StartB, match.StartB + match.LenB - count_x));
            } else {
                return (match.StartB, match.StartB + match.LenB);
            }
        }
    }
}