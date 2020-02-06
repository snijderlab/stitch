using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AssemblyNameSpace;
using AssemblyNameSpace.RunParameters;
using static AssemblyNameSpace.SequenceMatch;

namespace AssemblyTestNameSpace
{
    [TestClass]
    public class SmithWaterman_Test
    {
        readonly Alphabet alp;
        public SmithWaterman_Test()
        {
            alp = new Alphabet("*;A;B;C;D\nA;1;0;0;0\nB;0;1;0;0\nC;0;0;1;0\nD;0;0;0;1", Alphabet.AlphabetParamType.Data, 2, 1);
        }
        [DataRow("CAAAACCCABBC", "CAAAACDCCABBC")]
        [DataRow("CCCBAAACACBB", "CCCBAADACACBB")]
        [DataTestMethod]
        public void SingleGap(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(10, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("6M1I6M", r.Alignment.CIGAR());
        }
        [DataRow("ACACCACCACCA", "ACACDCACCDACCA")]
        [DataRow("ABBCABAAABCA", "ABBCDABAADABCA")]
        [DataTestMethod]
        public void DoubleGap(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(8, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("4M1I4M1I4M", r.Alignment.CIGAR());
        }
        [DataRow("ACACACA", "ACABACA")]
        [DataRow("ACACACA", "ACACBCA")]
        [DataTestMethod]
        public void Mismatch(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(6, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("7M", r.Alignment.CIGAR());
        }
        [DataRow("ABBA")]
        [DataRow("AAABBA")]
        [DataRow("AAAAAAAAA")]
        [DataRow("AAABBAAAABBA")]
        [DataTestMethod]
        public void EqualSequences(string x)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(x);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(x.Length, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual($"{x.Length}M", r.Alignment.CIGAR());
        }
        [DataRow("BABACBAAABCB", "CBAAA")]
        [DataRow("CBABABACCBCA", "ABACC")]
        [DataTestMethod]
        public void EqualPart(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(5, r.Score);
            Assert.AreEqual(4, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("5M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void MismatchAndGap()
        {
            //ABBAABBCB ABAACCBBAAB
            //     BBCBDABCACC
            var a = StringToSequence("ABBAABBCBABAACCBBAAB");
            var b = StringToSequence("BBCBDABCACC");
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(7, r.Score);
            Assert.AreEqual(5, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("4M1I6M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void GenomicTest01()
        {
            // I have not found the right value yet
            // Random sequences
            var alp = new Alphabet("*;A;C;G;T\nA;5;-4;-4;-4\nC;-4;5;-4;-4\nG;-4;-4;5;-4\nT;-4;-4;-4;5", Alphabet.AlphabetParamType.Data, 10, 10);
            var a = StringToSequence("GGTGCCGACCGCGGACTGCT", alp);
            var b = StringToSequence("CCCCGGGTGTGGCTCCTTCA", alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(21, r.Score);
            Assert.AreEqual(8, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("6M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void GenomicTest02()
        {
            // I have not found the right value yet
            // Random sequences
            var alp = new Alphabet("*;A;C;G;T\nA;5;-4;-4;-4\nC;-4;5;-4;-4\nG;-4;-4;5;-4\nT;-4;-4;-4;5", Alphabet.AlphabetParamType.Data, 1, 2);
            var a = StringToSequence("TCTGACAACGTGCAACCGCTATCGCCATCGATTGATTCAGCGGACGGTGT", alp);
            var b = StringToSequence("TGTCGTCATAGTTTGGGCATGTTTCCCTTGTAGGTGTGAAATCACTTAGC", alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(112, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(2, r.StartQueryPosition);
            Assert.AreEqual("2M1D1M1D1I2M1I1M1D2M2I1M2I2M1I3D1M1D1M1D1M1I1M1D2M1D1M1I1D1M1I1M2I1M1I3M1I1D1I3M1D1M1I1D1I1D1M1I1M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void LongerSequence()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 2, 1);
            var a = StringToSequence("VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSPVYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK", alp);
            var b = StringToSequence("VKAFEALQITSNLYGKCLPRIIMAKVSPVYWNSLFLLKGNYKAQMRGRTVNARVLKIGQKTRCMLLLWALRVVLGIRVSEVRQRFIVGAVQEALTK", alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(361, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("26M17D24M17I29M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void GFPAlignment()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            //Uniprot - P42212
            var gfp = "MSKGEELFTGVVPILVELDGDVNGHKFSVSGEGEGDATYGKLTLKFICTTGKLPVPWPTLVTTFSYGVQCFSRYPDHMKQHDFFKSAMPEGYVQERTIFFKDDGNYKTRAEVKFEGDTLVNRIELKGIDFKEDGNILGHKLEYNYNSHNVYIMADKQKNGIKVNFKIRHNIEDGSVQLADHYQQNTPIGDGPVLLPDNHYLSTQSALSKDPNEKRDHMVLLEFVTAAGITHGMDELYK";
            //Uniprot - P80893
            //var bfp = "MFKGNVQGVGTVENIDKGAKFQSLHGVSLLPIDADLQSHDIIFPEDILEGVTSGELIAINGVRLTVVHTDKSIVRFDINDALELTTLGQLKVGDKVNIEKSFKFGDMTGGRSLSGIVTGVADIVEFIEKENNRQIWIEAPEHLTEFLVEKKYIGVDGVYLVIDAIENNRFCINLLLETDMRWYKKGSKVNIEIPDIAGNW";
            //Uniprot - X5DSL3
            var mCherry = "MVSKGEEDNMAIIKEFMRFKVHMEGSVNGHEFEIEGEGEGRPYEGTQTAKLKVTKGGPLPFAWDILSPQFMYGSKAYVKHPADIPDYLKLSFPEGFKWERVMNFEDGGVVTVTQDSSLQDGEFIYKVKLRGTNFPSDGPVMQKKTMGWEASSERMYPEDGALKGEIKQRLKLKDGGHYDAEVKTTYKAKKPVQLPGAYNVNIKLDITSHNEDYTIVEQYERAEGRHSTGGMDELYK";
            var a = StringToSequence(gfp, alp);
            var b = StringToSequence(mCherry, alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(283, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(1, r.StartQueryPosition);
            Assert.AreEqual("13M4I22M1D3M1I12M1I28M2D59M1I11M4D20M5D2M2I6M1I22M1D2M2D13M2I10M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void IgGAlignment()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            //IgG4-K-002
            var igg4 = "QLQLQESGPGLLKPSETLSLTCTVSGGSISSPGYYGGWIRQPPGKGLEWIGSIYKSGSTYHNPSLKSRVTISVDTSKNQFSLKLSSVTAADTAVYYCTRPVVRYFGWFDPWGQGTLVTVSSASTKGPSVFPLAPCSRSTSESTAALGCLVKDYFPEPVTVSWNSGALTSGVHTFPAVLQSSGLYSLSSVVTVPSSSLGTKTYTCNVDHKPSNTKVDKRVESKYGPPCPPCPAPEFEGGPSVFLFPPKPKDTLMISRTPEVTCVVVDVSQEDPEVQFNWYVDGVEVHNAKTKPREEQFNSTYRVVSVLTVLHQDWLNGKEYKCKVSNKGLPSSIEKTISKAKGQPREPQVYTLPPSQEEMTKNQVSLTCLVKGFYPSDIAVEWESNGQPENNYKTTPPVLDSDGSFFLYSRLTVDKSRWQEGNVFSCSVMHEALHNHYTQKSLSLSLGKAIQLTQSPSSLSASVGDRVTITCRASQGISSALAWYQQKPGKAPKLLIYDASNLESGVPSRFSGSGSGTDFTLTISSLQPEDFATYYCQQFNSYPTFGQGTKVEIKRTVAAPSVFIFPPSDEQLKSGTASVVCLLNNFYPREAKVQWKVDNALQSGNSQESVTEQDSKDSTYSLSSTLTLSKADYEKHKVYACEVTHQGLSSPVTKSFNRGEC";
            //IgG2-K-002
            var igg2 = "QVQLQESGPGLVKPSQTLSLTCTVSGGSISSGEYYWNWIRQHPGKGLEWIGYIYYSGSTYYNPSLKSRVTISVDTSKNQFSLKLSSVTAADTAVYYCARESVAGFDYWGQGTLVTVSSASTKGPSVFPLAPCSRSTSESTAALGCLVKDYFPEPVTVSWNSGALTSGVHTFPAVLQSSGLYSLSSVVTVPSSNFGTQTYTCNVDHKPSNTKVDKTVERKCCVECPPCPAPPVAGPSVFLFPPKPKDTLMISRTPEVTCVVVDVSHEDPEVQFNWYVDGVEVHNAKTKPREEQFNSTFRVVSVLTVVHQDWLNGKEYKCKVSNKGLPAPIEKTISKTKGQPREPQVYTLPPSREEMTKNQVSLTCLVKGFYPSDIAVEWESNGQPENNYKTTPPMLDSDGSFFLYSKLTVDKSRWQQGNVFSCSVMHEALHNHYTQKSLSLSPGKEIVLTQSPGTLSLSPGERATLSCRASQSVSSSYLAWYQQKPGQAPRLLIYGTSSRATGIPDRFSGSGSGTDFTLTISRLEPEDFAVYYCQQYGSSPITFGQGTRLEIKRTVAAPSVFIFPPSDEQLKSGTASVVCLLNNFYPREAKVQWKVDNALQSGNSQESVTEQDSKDSTYSLSSTLTLSKADYEKHKVYACEVTHQGLSSPVTKSFNRGEC";
            var a = StringToSequence(igg2, alp);
            var b = StringToSequence(igg4, alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(3070, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("103M2I1M1I128M1I244M1D63M1D118M", r.Alignment.CIGAR());
        }
        // The following tests are written based on paths that were misaligned by the full program
        // The testcases should only pass if the alignment is what should be expected as a good alignment, so not the one given by the full program
        [TestMethod]
        public void RealWorldFullTemplate()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var template = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR*YYYYYGMDVWGQGTTVTVSSASTKGPSVFPLAPCSRSTSGGTAALGCLVKDYFPEPVTVSWNSGALTSGVHTFPAVLQSSGLYSLSSVVTVPSSSLGTQTYTCNVNHKPSNTKVDKRVELKTPLGDTTHTCPRCPEPKSCDTPPPCPRCPEPKSCDTPPPCPRCPEPKSCDTPPPCPRCPAPELLGGPSVFLFPPKPKDTLMISRTPEVTCVVVDVSHEDPEVQFKWYVDGVEVHNAKTKPREEQYNSTFRVVSVLTVLHQDWLNGKEYKCKVSNKALPAPIEKTISKTKGQPREPQVYTLPPSREEMTKNQVSLTCLVKGFYPSDIAVEWESSGQPENNYNTTPPMLDSDGSFFLYSKLTVDKSRWQQGNIFSCSVMHEALHNRFTQKSLSLSPGKDILLTQTPLSLSITPGEPASISCRSSRSLLHSNGNTYLHWLQKPGQPPQCLICKVSNRFSGVPDRFSGSGSGIDFTLKISPVEAADVGVYITACKLHTGPCTFGQGTKLEIKRTVAAPSVFIFPPSDEQLKSGTASVVCLLNNFYPREAKVQWKVDNALQSGNSQESVTEQDSKDSTYSLSNTLTLSKADYEKHKVYACEVTHQGLSSPVTKSFNRGEC";
            var path = "TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF";
            var a = StringToSequence(template, alp);
            var b = StringToSequence(path, alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(239, r.Score);
            Assert.AreEqual(68, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("30M1D7M4I22M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void RealWorldIGHV()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var template = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR";
            var path = "TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF";
            var a = StringToSequence(template, alp);
            var b = StringToSequence(path, alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(147, r.Score);
            Assert.AreEqual(68, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("30M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void RealWorldViaTemplateDatabaseList()
        {
            var alp = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var tem = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR";
            var path = "TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF";
            var a = StringToSequence(tem, alp);
            var b = StringToSequence(path, alp);

            Template template = new Template("", a, new MetaData.None(new MetaData.FileIdentifier("not empty", "")), alp, 0);
            TemplateDatabase db = new AssemblyNameSpace.TemplateDatabase(new List<Template> { template }, alp, "TEST DB", 0);

            db.Match(new List<List<AminoAcid>> { b.ToList() });
            var r = db.Templates[0].Matches[0];

            Console.WriteLine(r.ToString());
            Assert.AreEqual(147, r.Score);
            Assert.AreEqual(68, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("30M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void RealWorldViaTemplateDatabaseFile()
        {
            var alp = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var path = "TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF";
            var b = StringToSequence(path, alp);

            TemplateDatabase db = new AssemblyNameSpace.TemplateDatabase(new MetaData.FileIdentifier("../../../../examples/013/template.txt", "TEMPLATE"), InputType.Reads, alp, "TEST DB", 0, 0);

            db.Match(new List<List<AminoAcid>> { b.ToList() });
            var r = db.Templates[0].Matches[0];

            Console.WriteLine(r.ToString());
            Assert.AreEqual(147, r.Score);
            Assert.AreEqual(68, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("30M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void RealWorldViaBatchFile()
        {
            var parameters = ParseCommandFile.Batch("../../../../examples/batchfiles/smalltest.txt");
            var runs = parameters.CreateRuns();
            Assert.AreEqual(1, runs.Count());
            Assert.AreEqual(1, runs[0].Input.Count());
            Assert.AreEqual(1, runs[0].Template.Count());

            var run = runs[0];
            var template_parameter = run.Template[0];

            var alphabet = new Alphabet(run.Alphabet);
            var assm = new Assembler(run.K, run.DuplicateThreshold, run.MinimalHomology, run.Reverse, alphabet);

            assm.GiveReads(OpenReads.Simple(run.Input[0].File));

            assm.Assemble();

            var database = new TemplateDatabase(new MetaData.FileIdentifier(template_parameter.Path, template_parameter.Name), template_parameter.Type, new Alphabet(template_parameter.Alphabet), template_parameter.Name, template_parameter.CutoffScore, 0);

            database.MatchParallel(assm.GetAllPaths());

            // Test if something went wrong
            Assert.AreEqual(1, database.Templates.Count());
            Assert.AreEqual(1, database.Templates[0].Matches.Count());
            var template = database.Templates[0];
            var match = template.Matches[0];
            var reference_alphabet = new Alphabet("../../../../examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);

            Console.WriteLine(match);
            Console.WriteLine(template.Alphabet);
            Console.WriteLine(reference_alphabet);

            // Alphabet
            Assert.AreEqual("Blosum62", template_parameter.Alphabet.Name);
            Assert.AreEqual(12, template_parameter.Alphabet.GapStartPenalty);
            Assert.AreEqual(2, template_parameter.Alphabet.GapExtendPenalty);
            Assert.IsNotNull(template_parameter.Alphabet);
            Assert.AreEqual(reference_alphabet.ToString(), template.Alphabet.ToString());
            // Template sequence
            Assert.AreEqual("EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR", AminoAcid.ArrayToString(template.Sequence));
            // Path sequence
            Assert.AreEqual("TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF", AminoAcid.ArrayToString(match.QuerySequence));
            Assert.AreEqual("TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF", AminoAcid.ArrayToString(assm.GetAllPaths()[0].Sequence));
            // Match
            Assert.AreEqual(147, match.Score);
            Assert.AreEqual(68, match.StartTemplatePosition);
            Assert.AreEqual(0, match.StartQueryPosition);
            Assert.AreEqual("30M", match.Alignment.CIGAR());
        }
        AminoAcid[] StringToSequence(string input)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alp, input[i]);
            }
            return output;
        }
        AminoAcid[] StringToSequence(string input, Alphabet alp)
        {
            AminoAcid[] output = new AminoAcid[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = new AminoAcid(alp, input[i]);
            }
            return output;
        }
    }
}
