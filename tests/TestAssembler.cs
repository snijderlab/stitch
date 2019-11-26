using Microsoft.VisualStudio.TestTools.UnitTesting;
using AssemblyNameSpace;

namespace AssemblyTestNameSpace
{
    [TestClass]
    public class Alphabet_Test
    {
        Alphabet alp;
        public Alphabet_Test()
        {
            alp = new Alphabet("*;A;B\nA;1;0\nB;0;1", Alphabet.AlphabetParamType.Data);
        }
        [DataRow('A', 'B')]
        [DataRow('B', 'A')]
        [DataTestMethod]
        public void InvariantNotEqual(char x, char y)
        {
            int a = alp.getIndexInAlphabet(x);
            int b = alp.getIndexInAlphabet(y);
            Assert.AreNotEqual(a, b);
        }
        [DataRow('A', 'A')]
        [DataRow('B', 'B')]
        [DataTestMethod]
        public void InvariantEqual(char x, char y)
        {
            int a = alp.getIndexInAlphabet(x);
            int b = alp.getIndexInAlphabet(y);
            Assert.AreEqual(a, b);
        }
        [TestMethod]
        public void InvariantNotInAlphabet()
        {
            string input = "abcdefghijklmnopqrstuvwxyzCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            foreach (char c in input)
            {
                int a = alp.getIndexInAlphabet(c);
                Assert.AreEqual(a, -1);
            }
        }
        [DataRow("*;A;B\nA;1;0", "Missing row")]
        [DataRow("*;A;B\nA;1;0\nB;0;1\nC;0;0", "Extra row")]
        [DataRow("*;A;B\nA;1;0;0\nB;0;1;0\nC;0;0;1", "Missing Column")]
        [DataRow("*;A;B\nA;1;0\nB;o;1", "Non Integer value")]
        [DataRow("*;A;B\nA;1;0\nB;;1", "Missing value")]
        [DataTestMethod]
        public void InvariantNotValidAlphabet(string a, string msg)
        {
            Assert.ThrowsException<ParseException>(() => new Alphabet(a, Alphabet.AlphabetParamType.Data), msg);
        }
        [TestMethod]
        public void OpenViaFile()
        {
            //Expect to be running inside the /bin/debug/netcoreapp2.2 folder
            Alphabet alp2 = new Alphabet(@"../../../testalphabet.csv", Alphabet.AlphabetParamType.Path);
            string input = "AB";
            foreach (char c in input)
            {
                Assert.AreEqual(alp.getIndexInAlphabet(c), alp2.getIndexInAlphabet(c));
            }
        }
    }
}
