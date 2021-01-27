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
        public void TwoGaps(string x, string y)
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
        [DataRow("ACBCCACBABCA", "ACBCDDCACBABCA")]
        [DataRow("ABBCABAAABCA", "ABBCDDABAAABCA")]
        [DataTestMethod]
        public void LongGap_Double(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(9, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("4M2I8M", r.Alignment.CIGAR());
        }
        [DataRow("ACACCACACCAABCA", "ACACCACDDDACCAABCA")]
        [DataRow("ABBCABAABCAACCA", "ABBCABADDDABCAACCA")]
        [DataTestMethod]
        public void LongGap_Triple(string x, string y)
        {
            var a = StringToSequence(x);
            var b = StringToSequence(y);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(11, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("7M3I8M", r.Alignment.CIGAR());
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
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 2, 1);
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
        public void AlreadyAddedGap()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 2, 1);
            var a = StringToSequence("VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSP*VYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK", alp);
            var b = StringToSequence("VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSPGLEWIGSIYKSGSTYHNPSLKSRVTISVYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK", alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(454, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("45M26I52M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void AlreadyAddedGapViaTemplate01()
        {
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var a = StringToSequence("VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSP*VYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK", alp);
            var b = StringToSequence("VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSPGLEWIGSIYKSGSTYHNPSLKSRVTISVYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK", alp);

            TemplateDatabase db = new TemplateDatabase(new List<Template>(), alp, "TEST DB", 0);
            var namefilter = new NameFilter();
            Template template = new Template("", a, new MetaData.Simple(new MetaData.FileIdentifier(), namefilter), db);
            db.Templates.Add(template);

            db.Match(new List<GraphPath> { new GraphPath(b.ToList()) });
            var r = db.Templates[0].Matches[0];

            Console.WriteLine(r.ToString());

            Assert.AreEqual(419, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("45M26I52M", r.Alignment.CIGAR());

            const string expected = "VKAFEALQITSNLYGKCLPRIIMAKVNARVLKIGQKTRCMLLLSPGLEWIGSIYKSGSTYHNPSLKSRVTISVYWNSLFLLKGNYKAQMRGRTVWALRVVLGIRVSEVRQRFIVGAVQEALTK";
            var seq = db.Templates[0].ConsensusSequence().Item1;
            Console.Write($"\rExpected: {expected}\nActual:   {seq}");
            Assert.AreEqual(expected, seq);
        }
        [TestMethod]
        public void AlreadyAddedGapViaTemplate02()
        {
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var a = StringToSequence("LIWYDGSNEDYTDSVKGRFTISRDNSKNTLYLQMNSLRAEDTAVYYCAR*DIWGQGTVVTVSSASTKGPSVFPLAP", alp);
            var b = StringToSequence("LIWYDGSNEDYTDSVKGRFTISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVFPLAP", alp);

            TemplateDatabase db = new TemplateDatabase(new List<Template>(), alp, "TEST DB", 0);
            var namefilter = new NameFilter();
            Template template = new Template("", a, new MetaData.Simple(new MetaData.FileIdentifier(), namefilter), db);
            db.Templates.Add(template);

            db.Match(new List<GraphPath> { new GraphPath(b.ToList()) });
            var r = db.Templates[0].Matches[0];

            Console.WriteLine(r.ToString());

            Assert.AreEqual(366, r.Score);
            Assert.AreEqual(0, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("49M10I27M", r.Alignment.CIGAR());

            const string expected = "LIWYDGSNEDYTDSVKGRFTISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVFPLAP";
            var seq = db.Templates[0].ConsensusSequence().Item1;
            Console.Write($"\rExpected: {expected}\nActual:   {seq}");
            Assert.AreEqual(expected, seq);
        }
        [TestMethod]
        public void GFPAlignment()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            Console.WriteLine($"GapChar '{Alphabet.GapChar}'");
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
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
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
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
            var template = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR*YYYYYGMDVWGQGTTVTVSSASTKGPSVFPLAPCSRSTSGGTAALGCLVKDYFPEPVTVSWNSGALTSGVHTFPAVLQSSGLYSLSSVVTVPSSSLGTQTYTCNVNHKPSNTKVDKRVELKTPLGDTTHTCPRCPEPKSCDTPPPCPRCPEPKSCDTPPPCPRCPEPKSCDTPPPCPRCPAPELLGGPSVFLFPPKPKDTLMISRTPEVTCVVVDVSHEDPEVQFKWYVDGVEVHNAKTKPREEQYNSTFRVVSVLTVLHQDWLNGKEYKCKVSNKALPAPIEKTISKTKGQPREPQVYTLPPSREEMTKNQVSLTCLVKGFYPSDIAVEWESSGQPENNYNTTPPMLDSDGSFFLYSKLTVDKSRWQQGNIFSCSVMHEALHNRFTQKSLSLSPGKDILLTQTPLSLSITPGEPASISCRSSRSLLHSNGNTYLHWLQKPGQPPQCLICKVSNRFSGVPDRFSGSGSGIDFTLKISPVEAADVGVYITACKLHTGPCTFGQGTKLEIKRTVAAPSVFIFPPSDEQLKSGTASVVCLLNNFYPREAKVQWKVDNALQSGNSQESVTEQDSKDSTYSLSNTLTLSKADYEKHKVYACEVTHQGLSSPVTKSFNRGEC";
            var path = "TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF";
            var a = StringToSequence(template, alp);
            var b = StringToSequence(path, alp);
            var r = HelperFunctionality.SmithWaterman(a, b, alp);
            Console.WriteLine(r.ToString());
            Assert.AreEqual(241, r.Score);
            Assert.AreEqual(68, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("31M1D6M4I22M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void RealWorldIGHV()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
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
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var tem = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR";
            var path = "TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF";
            var a = StringToSequence(tem, alp);
            var b = StringToSequence(path, alp);

            TemplateDatabase db = new TemplateDatabase(new List<Template>(), alp, "TEST DB", 0);
            var namefilter = new NameFilter();
            Template template = new Template("", a, new MetaData.Simple(new MetaData.FileIdentifier(), namefilter), db);
            db.Templates.Add(template);

            db.Match(new List<GraphPath> { new GraphPath(b.ToList()) });
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
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var path = "TISRDNSKNTLYLQMNSLRAEDTAVYYCARWGMVRGVIDVFDIWGQGTVVTVSSASTKGPSVF";
            var b = StringToSequence(path, alp);
            var namefilter = new NameFilter();

            TemplateDatabase db = new TemplateDatabase(OpenReads.Simple(namefilter, new MetaData.FileIdentifier(Globals.Root + "examples/013/template.txt", "TEMPLATE")).ReturnOrFail(), alp, "TEST DB", 0, 0);

            db.Match(new List<GraphPath> { new GraphPath(b.ToList()) });
            var r = db.Templates[0].Matches[0];

            Console.WriteLine(r.ToString());
            Assert.AreEqual(147, r.Score);
            Assert.AreEqual(68, r.StartTemplatePosition);
            Assert.AreEqual(0, r.StartQueryPosition);
            Assert.AreEqual("30M", r.Alignment.CIGAR());
        }
        [TestMethod]
        public void TestGapInConsensusSequence()
        {
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var tem = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR";
            var path1 = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVANIKQDGSEKYYVDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR";
            var path2 = "EVQLVESGGGLVQPGGSLRLSCAASGFTFSSYWMSWVRQAPGKGLEWVQWTDSVKGRFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR";
            var a = StringToSequence(tem, alp);
            var b = StringToSequence(path1, alp);
            var c = StringToSequence(path2, alp);

            TemplateDatabase db = new TemplateDatabase(new List<Template>(), alp, "TEST DB", 0);
            var namefilter = new NameFilter();
            Template template = new Template("", a, new MetaData.Simple(new MetaData.FileIdentifier(), namefilter), db);
            db.Templates.Add(template);

            db.Match(new List<GraphPath> { new GraphPath(b.ToList()), new GraphPath(b.ToList()), new GraphPath(c.ToList()) });
            var r = db.Templates[0].Matches[0];

            Console.WriteLine(r.ToString());
            var gaps = db.Templates[0].CombinedSequence()[47].Gaps;
            foreach (var gap in gaps)
            {
                Console.WriteLine($"{gap.ToString()} {gap.GetHashCode()} {gap.Value.Count}");
            }
            Assert.AreEqual(path1, db.Templates[0].ConsensusSequence().Item1);
        }
        /*
        [TestMethod]
        public void TestForceOnSingleTemplate()
        {
            var alp = new Alphabet(Globals.Root + "examples/alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 12, 2);
            var tem1 = StringToSequence("EVQLVESGGGLVQPGGSLRLSCAASGFTFSSY", alp);
            var tem2 = StringToSequence("RFTISRDNAKNSLYLQMNSLRAEDTAVYYCAR", alp);
            var path1 = StringToSequence("ESGGGLVQPGGSLRLSCAASGFTF", alp);
            var path2 = StringToSequence("SRDNAKNSLYLQMNSLRAEDTAVYYC", alp);

            TemplateDatabase db = new TemplateDatabase(new List<Template>(), alp, "TEST DB", 0);
            var namefilter = new NameFilter();
            Template template1 = new Template("", tem1, new MetaData.Simple(new MetaData.FileIdentifier("not empty", ""), namefilter), db);
            Template template2 = new Template("", tem2, new MetaData.Simple(new MetaData.FileIdentifier("not empty", ""), namefilter), db);
            db.Templates.Add(template1);
            db.Templates.Add(template2);

            db.Match(new List<GraphPath> { new GraphPath(path1.ToList()), new GraphPath(path2.ToList()) }, 1, true);

            Console.WriteLine($"Templates: {db.Templates.Count}");
            Console.WriteLine($" 0: matches {db.Templates[0].Matches.Count()}");
            Console.WriteLine($" 1: matches {db.Templates[1].Matches.Count()}");

            //Console.WriteLine(db.Templates[0].Matches[0]);
            Console.WriteLine(db.Templates[1].Matches[0]);

            Assert.AreEqual(db.Templates[0].Matches.Count(), db.Templates[1].Matches.Count());
            Assert.IsTrue(AminoAcid.ArrayEquals(db.Templates[0].Matches[0].QuerySequence, path1));
            Assert.IsTrue(AminoAcid.ArrayEquals(db.Templates[1].Matches[0].QuerySequence, path2));
        }*/
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
