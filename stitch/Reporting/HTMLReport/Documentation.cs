namespace HTMLNameSpace
{
    /// <summary>
    /// To contain all help messages related to tables
    /// </summary>
    public class HTMLHelp
    {
        public const string ConsensusSequence = "The sequence obtained by taking the highest scoring amino acid for each position. The score is calculated as the sum of all positional scores for that amino acid on this position. Any position where no reads where mapped are filled in by the sequence from the template, otherwise the template does not have any direct effect on the scoring. Insertions into the consensus sequence are only taken into account if the total score for that insertion is higher then the 'default' score, which is calculated by summing all positional scores of all amino acids not followed by an insertion from the previous position.";
        public const string DOCGraph = "The depth of coverage reported is the sum of all positional scores for that position. This is reported with the same position numbering as the consensus sequence.";
        public const string SequenceConsensusOverview = "This displays the found diversity of amino acids for each position. The amino acids are linearly scaled to the respective score. The score is calculated as the sum of all positional scores (if present otherwise the general scores) for this position in all aligned reads. Any positions where no reads where mapped are displayed as dots '.'.";
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