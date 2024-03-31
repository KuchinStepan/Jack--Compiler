using System;
using System.Collections.Generic;

namespace JackCompiling
{
    public partial class CodeWriter
    {
        /// <summary>2+x</summary>
        public void WriteExpression(ExpressionSyntax expression)
        {
            WriteTerm(expression.Term);
            foreach (var tail in expression.Tail)
            {
                WriteTerm(tail.Term);
                resultVmCode.Add(opCommands[tail.Operator.Value]);
            }
        }

        private readonly static IReadOnlyDictionary<string, string> opCommands =
            new Dictionary<string, string>() {{ "-", "sub" }, { "+", "add"}, { ">", "gt"},
                { "<", "lt"}, { "=", "eq"}, {"&","and"}, {"|", "or" }, { "*", "call Math.multiply 2"},
                { "/", "call Math.divide 2"} };

        private void WriteTerm(TermSyntax term)
        {
            var ok = TryWriteNumericTerm(term)
                     || TryWriteObjectValue(term) // будет реализована в следующих задачах
                     || TryWriteSubroutineCall(term) // будет реализована в следующих задачах
                     || TryWriteStringValue(term) // будет реализована в следующих задачах
                     || TryWriteArrayAccess(term); // будет реализована в следующих задачах
            if (!ok)
                throw new FormatException($"Unknown term [{term}]");
        }

        /// <summary>42 | true | false | varName | -x | ( x )</summary>
        private bool TryWriteNumericTerm(TermSyntax term)
        {
            var termType = term.GetType();
            if (termType == typeof(ValueTermSyntax))
            {
                return WriteValueTerm((ValueTermSyntax)term);
            }
            if (termType == typeof(UnaryOpTermSyntax))
            {
                WriteUnaryOpTerm((UnaryOpTermSyntax)term);
                return true;
            }
            if (termType == typeof(ParenthesizedTermSyntax))
            {
                var innerExpression = ((ParenthesizedTermSyntax)term).Expression;
                WriteExpression(innerExpression);
                return true;
            }

            return false;
        }

        private bool WriteValueTerm(ValueTermSyntax term)
        {
            if (term.Indexing != null) return false;
            var termValue = term.Value;
            switch (termValue.TokenType)
            {
                case TokenType.IntegerConstant:
                    {
                        resultVmCode.Add($"push constant {termValue.Value}");
                        return true;
                    }
                case TokenType.Identifier:
                    {
                        if (!methodSymbols.TryGetValue(termValue.Value, out var variable))
                            if (!classSymbols.TryGetValue(termValue.Value, out variable))
                                throw new ArgumentException();
                        resultVmCode.Add($"push {variable.SegmentName} {variable.Index}");
                        return true;
                    }
                case TokenType.Keyword:
                    {
                        if (termValue.Value == "true")
                        {
                            resultVmCode.Add($"push constant -1");
                            return true;
                        }
                        if (termValue.Value == "false")
                        {
                            resultVmCode.Add($"push constant 0");
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        private void WriteUnaryOpTerm(UnaryOpTermSyntax term)
        {
            var symbol = term.UnaryOp.Value;
            WriteTerm(term.Term);
            if (symbol == "-")
                resultVmCode.Add("neg");
            if (symbol == "~")
                resultVmCode.Add("not");
        }
    }
}
