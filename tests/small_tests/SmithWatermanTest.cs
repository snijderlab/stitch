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
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 2, 1);
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
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 2, 1);
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
        public void GFPAlignment()
        {
            // Shuffled the sequence a bit
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
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
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
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
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
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
            var alp = new Alphabet(Globals.Root + "alphabets/blosum62.csv", Alphabet.AlphabetParamType.Path, 6, 2);
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
