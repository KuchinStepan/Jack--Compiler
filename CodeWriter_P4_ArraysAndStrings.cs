using System;

namespace JackCompiling
{
    public partial class CodeWriter
    {
        /// <summary>
        /// "string constant"
        /// </summary>
        private bool TryWriteStringValue(TermSyntax term)
        {
            if (term is not ValueTermSyntax)
                return false;
            var valueTerm = (ValueTermSyntax)term;
            if (valueTerm.Value.TokenType != TokenType.StringConstant)
                return false;
            var str = valueTerm.Value.Value;
            resultVmCode.Add($"push constant {str.Length}");
            resultVmCode.Add($"call String.new 1");
            foreach (var c in str)
            {
                resultVmCode.Add($"push constant {(byte)c}");
                resultVmCode.Add("call String.appendChar 1");
            }

            return true;
        }

        /// <summary>
        /// arr[index]
        /// </summary>
        private bool TryWriteArrayAccess(TermSyntax term)
        {
            if (term is not ValueTermSyntax)
                return false;
            var valueTerm = (ValueTermSyntax)term;
            if (valueTerm.Indexing == null)
                return false;
            WriteArrayAddress(valueTerm);
            resultVmCode.Add("pop pointer 1");
            resultVmCode.Add("push that 0");

            return true;
        }

        private void WriteArrayAddress(ValueTermSyntax valueTerm)
        {
            var varInfo = FindVarInfo(valueTerm.Value.Value);
            resultVmCode.Add($"push {varInfo.SegmentName} {varInfo.Index}");
            WriteExpression(valueTerm.Indexing.Index);
            resultVmCode.Add("add");
        }

        /// <summary>
        /// let arr[index] = expr;
        /// </summary>
        private bool TryWriteArrayAssignmentStatement(StatementSyntax statement)
        {
            if (statement is not LetStatementSyntax)
                return false;
            var letStatement = (LetStatementSyntax)statement;
            if (letStatement.Index == null)
                return false;

            WriteArrayAddress(new ValueTermSyntax(letStatement.VarName, letStatement.Index));

            WriteExpression(letStatement.Value);
            resultVmCode.Add("pop temp 0");
            resultVmCode.Add("pop pointer 1");
            resultVmCode.Add("push temp 0");
            resultVmCode.Add("pop that 0");

            return true;
        }
    }
}
