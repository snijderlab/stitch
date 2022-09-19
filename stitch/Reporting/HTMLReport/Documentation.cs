namespace HTMLNameSpace
{
    /// <summary>
    /// To contain all help messages related to tables
    /// </summary>
    public class HTMLHelp
    {
        public const string AnnotatedConsensusSequence = "The consensus sequence aligned with the germline sequence. The differences are accentuated by displaying the changes from the germline sequence below. The format follows the format of Domain Gap Align.";
        public const string ConsensusSequence = "The sequence obtained by taking the highest scoring amino acid for each position. The score is calculated as the sum of all positional scores for that amino acid on this position. Any position where no reads where mapped are filled in by the sequence from the template, otherwise the template does not have any direct effect on the scoring. Insertions into the consensus sequence are only taken into account if the total score for that insertion is higher than the 'default' score, which is calculated by summing all positional scores of all amino acids not followed by an insertion from the previous position.";
        public const string DOCGraph = "The depth of coverage reported is the sum of all positional scores for that position. This is reported with the same position numbering as the consensus sequence.";
        public const string Order = "The chosen templates for this recombination.";
        public const string OverviewOfScores = "The scores for all templates in this segment. All groups are displayed as a transparent box with its name in the right hand corner. The groups are sorted on their highest score. Within each group they are sorted on the score. You can use the buttons to toggle certain scores and their unique counterparts on or off.";
        public const string ReadLookup = "All places where this read could be placed.";
        public const string ReadsAlignment = "The exported data is a FASTA file containing all reads spaced by '~' and gaps are indicated by '.'. The first sequence is the template and has as header '>{id} template'. All other sequences are reads which have as header '>{id} score:{score} alignment:{alignment/CIGAR}'.";
        public const string RecombinedSequence = "The recombined sequence as generated from the Templates that can be seen under 'Order'.";
        public const string SegmentJoining = "This are the scores for all tested overlaps. The X-axis displays the overlaps the Y-axis the scores. If there are multiple overlaps with a high score make sure to manually check if stitch did indeed pick the best one.";
        public const string SequenceConsensusOverview = "This displays the found diversity of amino acids for each position. The amino acids are linearly scaled to the respective score. The score is calculated as the sum of all positional scores (if present otherwise the general scores) for this position in all aligned reads. Any positions where no reads where mapped are displayed as dots '.'.";
        public const string SequenceConsensusOverviewData = "A TSV file with for each position in the consensus sequence all found amino acids with its score. This par is saved as two consecutive columns filled with first the amino acid and second the score.";
        public const string Spectrum = "The raw spectrum of this peptide. The fragments are coloured according to ion type (see legend). Any peaks with a star '*' as text can be hovered over to see the full details, first the ion type second the mass shift type.";
        public const string TemplateIdentifier = "The identifier for this template.";
        public const string TemplateLength = "The length of the sequence in amino acids, excluding any 'X's added by 'GapHead' or 'GapTail'.";
        public const string TemplateMatches = "The total number of placed reads on this template.";
        public const string TemplateScore = "The total score calculated by summing all sums for all placed reads on this template.";
        public const string TemplateSequence = "The template sequence as read from the original file (without any annotations).";
        public const string TemplateTotalArea = "The sum of the area of all amino acids placed on this template.";
        public const string TemplateUniqueArea = "The sum of the area of all amino acids uniquely placed on this templates.";
        public const string TemplateUniqueMatches = "The total number of reads uniquely placed on this template.";
        public const string TemplateUniqueScore = "The total score calculated by summing all sums for all reads uniquely placed on this template.";
    }
}