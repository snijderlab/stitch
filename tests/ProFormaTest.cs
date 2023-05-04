using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Stitch;
using Stitch.RunParameters;

namespace StitchTest {
    [TestClass]
    public class ProForma_Test {
        [DataRow("AAA", "AAA")]
        [DataRow("A[+12]AA", "A[+12]AA")]
        [DataRow("A(+12)AA", "A[+12]AA")]
        [DataRow("A+12AA", "A[+12]AA")]
        [DataRow("A12AA", "A[12]AA")]
        [DataRow("[+12]AA", "[+12]-AA")]
        [DataRow("(+12)AA", "[+12]-AA")]
        [DataRow("+12AA", "[+12]-AA")]
        [DataRow("12AA", "[12]-AA")]
        [DataRow("AAA[+12]", "AAA[+12]")]
        [DataRow("AAA(+12)", "AAA[+12]")]
        [DataRow("AAA+12", "AAA[+12]")]
        [DataRow("AAA12", "AAA[12]")]
        [DataRow("AA(Oxidation (M))A", "AA[Oxidation (M)]A")]
        [DataRow("AA-12A", "AA[-12]A")]
        [DataRow("AA-12.123A", "AA[-12.123]A")]
        [DataRow("-12.1AA", "[-12.1]-AA")]
        [DataRow("_AAA_", "AAA")]
        [DataRow("_AA(Oxidation_W)A_", "AA[Oxidation_W]A")]
        [DataTestMethod]
        public void ProperCorrection(string a, string b) {
            Assert.AreEqual(b, HelperFunctionality.FromSloppyProForma(a).Unwrap().Modified, $"Original: {a}");
        }
    }
}