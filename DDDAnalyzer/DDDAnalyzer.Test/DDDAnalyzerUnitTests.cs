using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using DDDAnalyzer;
using DDDAnalyzer.ValueObjectAnalyzers;
using DDDAnalyzer.ValueObjectCodeFixProvider;
using NUnit.Framework;

namespace DDDAnalyzer.Test
{
    [TestFixture]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [Test]
        public void Test1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        protected override Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ValueObjectMustBeSealedCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ValueObjectAnalyzer();
        }
    }
}
