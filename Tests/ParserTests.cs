using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace JackCompiling.Tests
{
    [TestFixture]
    public class ParserTests
    {
        [TestCase("class X{}", "class[class X { }]")]
        [TestCase("class X{ field int a; }", "class[class X { classVarDec[field int a ;] }]")]
        [TestCase("class X{ static int b; }", "class[class X { classVarDec[static int b ;] }]")]
        [TestCase("class X{ method int m(){} }",
            "class[class X { subroutineDec[method int m ( parameterList[] ) subroutineBody[{ statements[] }]] }]")]
        public void ReadClassDecl(string text, string expectedTree)
        {
            AssertParserResult(text, p => p.ReadClass(), expectedTree);
        }

        [TestCase(")", "parameterList[]")]
        [TestCase("int x)", "parameterList[int x]")]
        [TestCase("int x, MyClass y)", "parameterList[int x , MyClass y]")]
        [TestCase("int x, char y, Array z)", "parameterList[int x , char y , Array z]")]
        public void ReadParametersList(string text, string expectedTree)
        {
            AssertParserResult(text, p => p.ReadParameterList(), expectedTree);
        }

        [TestCase("", "statements[]")]
        [TestCase("}", "statements[]")]
        [TestCase("return;", "statements[returnStatement[return ;]]")]
        public void ReadTrivialStatements(string text, string expectedTree)
        {
            AssertParserResult(text, p => p.ReadStatements(), expectedTree);
        }

        [TestCase("12;", "term[12]")]
        [TestCase("true;", "term[true]")]
        [TestCase("\"int true\";", "term[int true]")]
        [TestCase("x", "term[x]")]
        [TestCase("-42", "term[- term[42]]")]
        [TestCase("f()", "term[f ( expressionList[] )]")]
        [TestCase("C.m()", "term[C . m ( expressionList[] )]")]
        public void ReadTermWithoutExpressions(string termCode, string expectedTree)
        {
            AssertParserResult(termCode, p => p.ReadTerm(), expectedTree);
        }

        [TestCase("f(a)", "term[f ( expressionList[expression[term[a]]] )]")]
        [TestCase("C.m(a)", "term[C . m ( expressionList[expression[term[a]]] )]")]
        [TestCase("(42)", "term[( expression[term[42]] )]")]
        [TestCase("~(C.m())", "term[~ term[( expression[term[C . m ( expressionList[] )]] )]]")]
        [TestCase("x[1]", "term[x [ expression[term[1]] ]]")]
        [TestCase("x[y[1]]", "term[x [ expression[term[y [ expression[term[1]] ]]] ]]")]
        [TestCase("(1+2)", "term[( expression[term[1] + term[2]] )]")]
        [TestCase("(1+2*3)", "term[( expression[term[1] + term[2] * term[3]] )]")]
        [TestCase("(-1+~2)", "term[( expression[term[- term[1]] + term[~ term[2]]] )]")]
        public void ReadTermWithExpressions(string termCode, string expectedTree)
        {
            AssertParserResult(termCode, p => p.ReadTerm(), expectedTree);
        }

        [TestCase("f()", "f ( expressionList[] )")]
        [TestCase("f(a)", "f ( expressionList[expression[term[a]]] )")]
        [TestCase("M.f(a)", "M . f ( expressionList[expression[term[a]]] )")]
        [TestCase("M.f(a, b)", "M . f ( expressionList[expression[term[a]] , expression[term[b]]] )")]
        public void ReadSubroutineCall(string text, string expectedTree)
        {
            AssertParserResult(text, p => p.ReadSubroutineCall(), expectedTree);
        }

        [TestCase("return 42;", "statements[returnStatement[return expression[term[42]] ;]]")]
        [TestCase("let x = 2;", "statements[letStatement[let x = expression[term[2]] ;]]")]
        [TestCase("let x[1] = 2;", "statements[letStatement[let x [ expression[term[1]] ] = expression[term[2]] ;]]")]
        [TestCase("do f();", "statements[doStatement[do f ( expressionList[] ) ;]]")]
        [TestCase("do M.f();", "statements[doStatement[do M . f ( expressionList[] ) ;]]")]
        [TestCase("if (true) { }", "statements[ifStatement[if ( expression[term[true]] ) { statements[] }]]")]
        [TestCase("if (true) { } else { }",
            "statements[ifStatement[if ( expression[term[true]] ) { statements[] } else { statements[] }]]")]
        [TestCase("while (true) { }", "statements[whileStatement[while ( expression[term[true]] ) { statements[] }]]")]
        public void ReadStatements(string text, string expectedTree)
        {
            AssertParserResult(text, p => p.ReadStatements(), expectedTree);
        }

        [TestCase("Tests/01-ExpressionLessSquare/Main.jack")]
        [TestCase("Tests/01-ExpressionLessSquare/Square.jack")]
        [TestCase("Tests/01-ExpressionLessSquare/SquareGame.jack")]
        [TestCase("Tests/02-ArrayTest/Main.jack")]
        [TestCase("Tests/03-Square/Main.jack")]
        [TestCase("Tests/03-Square/Square.jack")]
        [TestCase("Tests/03-Square/SquareGame.jack")]
        public void RealTest(string jackFile)
        {
            var text = File.ReadAllText(jackFile);
            var expectedFilename = Path.ChangeExtension(jackFile, ".xml");
            var tokenizer = new Tokenizer(text);
            var parser = new Parser(tokenizer);
            var classSyntax = parser.ReadClass();
            var writer = new XmlSyntaxWriter();
            writer.Write(classSyntax);
            Console.WriteLine(string.Join("\n", writer.GetResult()));
            Assert.AreEqual(File.ReadAllLines(expectedFilename), writer.GetResult());
        }

        private void AssertParserResult(string text, Func<Parser, object> parse, string expectedTree)
        {
            var tokenizer = new Tokenizer(text);
            var parser = new Parser(tokenizer);
            var treeNode = parse(parser);
            var writer = new CompactSyntaxWriter();
            writer.Write(treeNode);
            Console.WriteLine(string.Join("\n", writer.GetResult()));
            Assert.AreEqual(expectedTree, writer.GetResult().Single());
        }
    }
}
