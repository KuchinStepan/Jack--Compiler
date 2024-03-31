using System;
using System.Collections.Generic;
using System.Linq;

namespace JackCompiling
{
    public partial class CodeWriter
    {
        /// <summary>
        /// class Name { ... }
        /// </summary>
        public void WriteClass(ClassSyntax classSyntax)
        {
            currentClassName = classSyntax.Name.Value;
            FillClassSymbols(classSyntax.ClassVars);
            foreach (var subroutine in classSyntax.SubroutineDec)
            {
                switch (subroutine.KindKeyword.Value)
                {
                    case "constructor": WriteConstructor(subroutine); break;
                    case "method": WriteMethod(subroutine); break;
                    case "function": WriteFunction(subroutine); break;

                    default: throw 
                            new ArgumentException($"Unknown subroutine {subroutine.KindKeyword.Value}");
                }
            }
        }

        private void FillClassSymbols(IReadOnlyList<ClassVarDecSyntax> vars)
        {
            var counter = new Dictionary<VarKind, int>() { { VarKind.Field, 0 }, { VarKind.Static, 0 } };
            foreach (var varDecSyntax in vars)
            {
                var kind = varDecSyntax.KindKeyword.Value == "static" ? VarKind.Static : VarKind.Field;
                foreach (var name in varDecSyntax.DelimitedNames)
                {
                    var varInfo = new VarInfo(counter[kind], kind, varDecSyntax.Type.Value);
                    classSymbols.Add(name.Value, varInfo);
                    counter[kind]++;
                }
            }
        }

        /// <summary>
        /// method Type Name ( ParameterList ) { Body }
        /// </summary>
        private void WriteMethod(SubroutineDecSyntax subroutine)
        {
            var localsCount = 0;
            foreach (var varDec in subroutine.SubroutineBody.VarDec)
                foreach (var i in varDec.DelimitedNames)
                    localsCount++;
            resultVmCode.Add($"function {currentClassName}.{subroutine.Name.Value} {localsCount}");
            resultVmCode.Add("push argument 0");
            resultVmCode.Add("pop pointer 0");

            WriteSubroutineBody(subroutine, true);
        }

        /// <summary>
        /// function Type Name ( ParameterList ) { Body }
        /// </summary>
        private void WriteFunction(SubroutineDecSyntax subroutine)
        {
            var localsCount = 0;
            foreach (var varDec in subroutine.SubroutineBody.VarDec)
                foreach (var i in varDec.DelimitedNames)
                    localsCount++;
            resultVmCode.Add($"function {currentClassName}.{subroutine.Name.Value} {localsCount}");
            WriteSubroutineBody(subroutine);
        }

        /// <summary>
        /// constructor Type Name ( ParameterList ) { Body }
        /// </summary>
        private void WriteConstructor(SubroutineDecSyntax subroutine)
        {
            var localsCount = 0;
            foreach (var varDec in subroutine.SubroutineBody.VarDec)
                foreach (var i in varDec.DelimitedNames)
                    localsCount++;
            resultVmCode.Add($"function {currentClassName}.{subroutine.Name.Value} {localsCount}");
            var fieldsCount = classSymbols.Values.Where(x => x.Kind == VarKind.Field).Count();
            resultVmCode.Add($"push constant {fieldsCount}");
            resultVmCode.Add("call Memory.alloc 1");
            resultVmCode.Add("pop pointer 0");

            WriteSubroutineBody(subroutine);
        }

        private void WriteSubroutineBody(SubroutineDecSyntax subroutine, bool isMethod=false)
        {
            var counter = new Dictionary<string, VarInfo>();
            AddArgsToCounter(counter, subroutine.ParameterList, isMethod);
            AddLocalsToCounter(counter, subroutine.SubroutineBody.VarDec);
            methodSymbols = counter;

            WriteStatements(subroutine.SubroutineBody.Statements);
        }

        private void AddArgsToCounter(Dictionary<string, VarInfo> counter, ParameterListSyntax args, 
            bool isMethod=false)
        {
            var argsCounter = 0;
            if (isMethod)
            {
                var varInfo = new VarInfo(argsCounter, VarKind.Parameter, currentClassName);
                counter["this"] = varInfo;
                argsCounter++;
            }
            foreach (var arg in args.DelimitedParameters)
            {
                var varInfo = new VarInfo(argsCounter, VarKind.Parameter, arg.Type.Value);
                counter[arg.Name.Value] = varInfo;
                argsCounter++;
            }
        }

        private void AddLocalsToCounter(Dictionary<string, VarInfo> counter, 
            IReadOnlyList<VarDecSyntax> varDecs)
        {
            var localsCounter = 0; // Для метода нужно сохранить this как arg[0]
            foreach (var varDec in varDecs)
            {
                foreach (var name in varDec.DelimitedNames)
                {
                    var varInfo = new VarInfo(localsCounter, VarKind.Local, varDec.Type.Value);
                    counter[name.Value] = varInfo;
                    localsCounter++;
                }
            }
        }

        /// <summary>
        /// ObjOrClassName . SubroutineName ( ExpressionList ) 
        /// </summary>
        private bool TryWriteSubroutineCall(TermSyntax term)
        {
            if (term is not SubroutineCallTermSyntax)
                return false;
            var subroutineTerm = (SubroutineCallTermSyntax)term;
            WriteSubroutineCall(subroutineTerm.Call);

            return true;
        }

        private void WriteSubroutineCall(SubroutineCall sCall)
        {
            var callingFunction = "";
            var argsCount = 0;
            if (sCall.ObjectOrClass == null)
            {
                callingFunction = currentClassName;
                argsCount++;
                resultVmCode.Add("push pointer 0");
            }
            else
            {
                var objOrClass = sCall.ObjectOrClass.Name.Value;
                if (methodSymbols.ContainsKey(objOrClass) || classSymbols.ContainsKey(objOrClass))
                {
                    var varInfo = FindVarInfo(objOrClass);
                    resultVmCode.Add($"push {varInfo.SegmentName} {varInfo.Index}");
                    argsCount++;

                    callingFunction = varInfo.Type;
                }
                else
                {
                    callingFunction = objOrClass;
                }

            }
            callingFunction += "." + sCall.SubroutineName.Value;
            foreach (var expr in sCall.Arguments.DelimitedExpressions)
            {
                WriteExpression(expr);
                argsCount++;
            }
            resultVmCode.Add($"call {callingFunction} {argsCount}");
        }

        /// <summary>
        /// do SubroutineCall ; 
        /// </summary>
        private bool TryWriteDoStatement(StatementSyntax statement)
        {
            if (statement is not DoStatementSyntax)
                return false;
            var doStatement = (DoStatementSyntax)statement;
            WriteSubroutineCall(doStatement.SubroutineCall);

            resultVmCode.Add("pop temp 0");

            return true;
        }

        /// <summary>
        /// return ;
        /// return Expression ;
        /// </summary>
        private bool TryWriteReturnStatement(StatementSyntax statement)
        {
            if (statement is not ReturnStatementSyntax)
                return false;
            var retStatement = (ReturnStatementSyntax)statement;
            if (retStatement.ReturnValue != null)
            {
                if (retStatement.ReturnValue.Term is ValueTermSyntax &&
                    ((ValueTermSyntax)retStatement.ReturnValue.Term).Value.Value == "this")
                {
                        resultVmCode.Add("push pointer 0");
                }
                else
                    WriteExpression(retStatement.ReturnValue); 
            }
            else
            {
                resultVmCode.Add("push constant 0");
            }
            resultVmCode.Add("return");
            return true;
        }

        /// <summary>
        /// this | null
        /// </summary>
        private bool TryWriteObjectValue(TermSyntax term)
        {
            if (term is not ValueTermSyntax)
                return false;
            var valueTerm = (ValueTermSyntax)term;
            if (valueTerm.Value.Value == "this")
            {
                resultVmCode.Add("push pointer 0");
                return true;
            }
            else if (valueTerm.Value.Value == "null")
            {
                resultVmCode.Add("push constant 0");
                return true;
            }
            return false;
        }
    }
}
