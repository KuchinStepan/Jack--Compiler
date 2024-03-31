using System;

namespace JackCompiling
{
    public partial class CodeWriter
    {
        /// <summary>Statement; Statement; ...</summary>
        public void WriteStatements(StatementsSyntax statements)
        {
            foreach (var statement in statements.Statements)
                WriteStatement(statement);
        }

        private void WriteStatement(StatementSyntax statement)
        {
            var ok = TryWriteVarAssignmentStatement(statement)
                     || TryWriteProgramFlowStatement(statement)
                     || TryWriteDoStatement(statement) // будет реализована в следующий задачах
                     || TryWriteArrayAssignmentStatement(statement)  // будет реализована в следующий задачах
                     || TryWriteReturnStatement(statement);  // будет реализована в следующий задачах
            if (!ok)
                throw new FormatException($"Unknown statement [{statement}]");
        }

        /// <summary>let VarName = Expression;</summary>
        private bool TryWriteVarAssignmentStatement(StatementSyntax statement)
        {
            if (statement.GetType() != typeof(LetStatementSyntax)) return false;
            var letSt = (LetStatementSyntax)statement;
            if (letSt.Index != null)
                return false;

            WriteExpression(letSt.Value);

            if (!methodSymbols.TryGetValue(letSt.VarName.Value, out var variable))
                if (!classSymbols.TryGetValue(letSt.VarName.Value, out variable))
                    throw new ArgumentException();
            resultVmCode.Add($"pop {variable.SegmentName} {variable.Index}");

            return true;
        }

        /// <summary>
        /// if ( Expression ) { Statements } [else { Statements }
        /// while ( Expression ) { Statements }
        /// </summary>
        private bool TryWriteProgramFlowStatement(StatementSyntax statement)
        {
            if (statement.GetType() == typeof(WhileStatementSyntax))
            {
                WriteWhileStatement((WhileStatementSyntax)statement);
                return true;
            }
            if (statement.GetType() == typeof(IfStatementSyntax))
            {
                WriteIfStatement((IfStatementSyntax)statement);
                return true;
            }
            return false;
        }

        private int whileLabelCounter = 0;

        private void WriteWhileStatement(WhileStatementSyntax statement)
        {
            var L1 = $"while1Label{whileLabelCounter}";
            var L2 = $"while2Label{whileLabelCounter}";
            whileLabelCounter++;

            resultVmCode.Add($"label {L1}");
            WriteExpression(statement.Condition);
            resultVmCode.Add($"not");
            resultVmCode.Add($"if-goto {L2}");
            WriteStatements(statement.Statements);
            resultVmCode.Add($"goto {L1}");
            resultVmCode.Add($"label {L2}");
        }

        private int ifLabelCounter = 0;

        private void WriteIfStatement(IfStatementSyntax statement)
        {
            var L1 = $"if1Label{ifLabelCounter}";
            var L2 = $"if2Label{ifLabelCounter}";
            ifLabelCounter++;

            WriteExpression(statement.Condition);
            resultVmCode.Add("not");
            resultVmCode.Add($"if-goto {L1}");
            WriteStatements(statement.TrueStatements);
            if (statement.ElseClause == null)
                resultVmCode.Add($"label {L1}");
            else
            {
                resultVmCode.Add($"goto {L2}");
                resultVmCode.Add($"label {L1}");
                WriteStatements(statement.ElseClause.FalseStatements);
                resultVmCode.Add($"label {L2}");
            }
        }
    }
}
