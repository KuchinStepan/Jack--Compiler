using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;

namespace JackCompiling
{
    public class Parser
    {
        private readonly Tokenizer tokenizer;

        public Parser(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
        }

        public ClassSyntax ReadClass()
        {
            var classToken = tokenizer.Read("class");
            var name = tokenizer.Read(TokenType.Identifier);
            var open = tokenizer.Read("{");
            var classVars = GetClassVarDecList();
            var subroutineDec = ReadSubroutineDecList();
            var close = tokenizer.Read("}");

            return new ClassSyntax(classToken, name, open, classVars, subroutineDec, close);
        }

        private ClassVarDecSyntax ReadClassVarDec()
        {
            var kindKeyword = tokenizer.TryReadNext();
            if (!IsVarKindKeyword(kindKeyword))
            {
                tokenizer.PushBack(kindKeyword);
                return null;
            }
            var type = tokenizer.TryReadNext();
            var delimitedNames = ReadDelimetedNames();
            var semicolon = tokenizer.Read(";");
            return new ClassVarDecSyntax(kindKeyword, type, delimitedNames, semicolon);
        }

        private List<ClassVarDecSyntax> GetClassVarDecList()
        {
            var result = new List<ClassVarDecSyntax>();
            var kindKeyword = tokenizer.TryReadNext();
            while (IsVarKindKeyword(kindKeyword))
            {
                var type = tokenizer.TryReadNext();
                var delimitedNames = ReadDelimetedNames();
                var semicolon = tokenizer.Read(";");
                var classVarDec = new ClassVarDecSyntax(kindKeyword, type, delimitedNames, semicolon);
                result.Add(classVarDec);
                kindKeyword = tokenizer.TryReadNext();
            }
            tokenizer.PushBack(kindKeyword);

            return result;
        }

        private List<Token> ReadDelimetedNames()
        {
            var result = new List<Token>();
            var name = tokenizer.TryReadNext();
            result.Add(name);
            var delimiter = tokenizer.TryReadNext();
            while (delimiter != null && delimiter.Value == ",")
            {
                name = tokenizer.TryReadNext();
                delimiter = tokenizer.TryReadNext();
                result.Add(name);
            }
            tokenizer.PushBack(delimiter);
            return result;
        }

        private static bool IsVarKindKeyword(Token token)
            => token != null && (token.Value == "field" || token.Value == "static");

        private static readonly IReadOnlySet<string> subroutineKindKeyword = new HashSet<string>()
            { "constructor", "function", "method" };

        private static bool IsSubroutineKindKeyword(Token token)
            => token != null && subroutineKindKeyword.Contains(token.Value);

        private List<SubroutineDecSyntax> ReadSubroutineDecList()
        {
            var result = new List<SubroutineDecSyntax>();
            var kindKeyword = tokenizer.TryReadNext();
            while (IsSubroutineKindKeyword(kindKeyword))
            {
                var returnType = tokenizer.TryReadNext();
                var name = tokenizer.TryReadNext();
                var open = tokenizer.Read("(");
                var parameterList = ReadParameterList();
                var close = tokenizer.Read(")");
                var subroutineBody = ReadSubroutineBody();

                var subrDec = new SubroutineDecSyntax(kindKeyword, returnType, name, open,
                                                    parameterList, close, subroutineBody);
                result.Add(subrDec);
                kindKeyword = tokenizer.TryReadNext();
            }
            tokenizer.PushBack(kindKeyword);

            return result;
        }

        private SubroutineBodySyntax ReadSubroutineBody()
        {
            var open = tokenizer.Read("{");
            var varDecList = GetVarDecList();
            var statements = ReadStatements();
            var close = tokenizer.Read("}");

            return new SubroutineBodySyntax(open, varDecList, statements, close);
        }

        private List<VarDecSyntax> GetVarDecList()
        {
            var result = new List<VarDecSyntax>();
            var varToken = tokenizer.TryReadNext();
            while (varToken != null && varToken.Value == "var")
            {
                var type = tokenizer.TryReadNext();
                var delimitedNames = ReadDelimetedNames();
                var semicolon = tokenizer.Read(";");
                var varDec = new VarDecSyntax(varToken, type, delimitedNames, semicolon);
                result.Add(varDec);
                varToken = tokenizer.TryReadNext();
            }
            tokenizer.PushBack(varToken);

            return result;
        }

        public StatementsSyntax ReadStatements()
        {
            var flag = true;
            var statementList = new List<StatementSyntax>();
            while (flag)
            {
                var firstToken = tokenizer.TryReadNext();
                if (firstToken == null)
                    return new StatementsSyntax(statementList);
                tokenizer.PushBack(firstToken);
                switch (firstToken.Value)
                {
                    case "return": WriteReturnStatement(statementList); break;
                    case "do": WriteDoStatement(statementList); break;
                    case "let": WriteLetStatement(statementList); break;
                    case "if": WriteIfStatement(statementList); break;
                    case "while": WriteWhileStatement(statementList); break;

                    default: flag = false; break;
                }
            }
            return new StatementsSyntax(statementList);
        }

        private void WriteWhileStatement(List<StatementSyntax> statementList)
        {
            var whileToken = tokenizer.Read("while");
            var open = tokenizer.Read("(");
            var condition = ReadExpression();
            var close = tokenizer.Read(")");
            var openStatements = tokenizer.Read("{");
            var statements = ReadStatements();
            var closeStatements = tokenizer.Read("}");

            var whileStatement = new WhileStatementSyntax(whileToken, open, condition,
                                                        close, openStatements, statements, closeStatements);
            statementList.Add(whileStatement);
        }

        private void WriteIfStatement(List<StatementSyntax> statementList)
        {
            var ifToken = tokenizer.Read("if");
            var open = tokenizer.Read("(");
            var condition = ReadExpression();
            var close = tokenizer.Read(")");
            var openTrue = tokenizer.Read("{");
            var trueStatements = ReadStatements();
            var closeTrue = tokenizer.Read("}");

            var elseClause = ReadElseClause();

            var ifStatement = new IfStatementSyntax(ifToken, open, condition, close, openTrue,
                                                    trueStatements, closeTrue, elseClause);
            statementList.Add(ifStatement);
        }

        private ElseClause ReadElseClause()
        {
            var elseToken = tokenizer.TryReadNext();
            if (elseToken == null || elseToken.Value != "else")
            {
                tokenizer.PushBack(elseToken);
                return null;
            }
            var open = tokenizer.Read("{");
            var falseStatements = ReadStatements();
            var close = tokenizer.Read("}");
            return new ElseClause(elseToken, open, falseStatements, close);
        }

        private void WriteLetStatement(List<StatementSyntax> statementList)
        {
            var letToken = tokenizer.Read("let");
            var varName = tokenizer.Read(TokenType.Identifier);
            var indexing = ReadIndexing();
            var eq = tokenizer.Read("=");
            var expression = ReadExpression();
            var semicolon = tokenizer.Read(";");

            var letStatement = new LetStatementSyntax(letToken, varName, indexing,
                                                        eq, expression, semicolon);
            statementList.Add(letStatement);
        }

        private void WriteDoStatement(List<StatementSyntax> statementList)
        {
            var doToken = tokenizer.Read("do");
            var subroutineCall = ReadSubroutineCall();
            var semicolonToken = tokenizer.Read(";");
            var doStatement = new DoStatementSyntax(doToken, subroutineCall, semicolonToken);
            statementList.Add(doStatement);
        }

        private void WriteReturnStatement(List<StatementSyntax> statementList)
        {
            var firstToken = tokenizer.Read("return");
            var semicolon = tokenizer.TryReadNext();
            ExpressionSyntax expression = null;
            if (semicolon != null && semicolon.Value != ";")
            {
                tokenizer.PushBack(semicolon);
                expression = ReadExpression();
                semicolon = tokenizer.Read(";");
            }
            var returnStatement = new ReturnStatementSyntax(firstToken, expression, semicolon);
            statementList.Add(returnStatement);
        }

        public ExpressionSyntax ReadExpression()
        {
            var term = ReadTerm();
            var expressionTailList = new List<ExpressionTail>();
            WriteToExpressionTail(expressionTailList);

            return new ExpressionSyntax(term, expressionTailList);
        }

        private void WriteToExpressionTail(List<ExpressionTail> tail)
        {
            var firstToken = tokenizer.TryReadNext();
            while (firstToken != null && firstToken.Value != ";"
                && firstToken.Value != ")" && firstToken.Value != "]"
                && firstToken.Value != ",")
            {
                var term = ReadTerm();
                var expressionTail = new ExpressionTail(firstToken, term);
                tail.Add(expressionTail);
                firstToken = tokenizer.TryReadNext();
            }
            // Проверить что нужно пушить назад
            tokenizer.PushBack(firstToken);
        }

        public TermSyntax ReadTerm()
        {
            TermSyntax result = null;
            var token = tokenizer.TryReadNext();
            tokenizer.PushBack(token);
            if (IsUnaryOp(token))
                return ReadUnaryOpTerm();
            if (token != null && token.Value == "(")
                return ReadParenthesizedTerm();
            if (IsValueToken(token) || IsLastVariable(tokenizer))
                return ReadValueTerm();
            if (IsIdentifierToken(token))
            {
                var subroutineCall = ReadSubroutineCall();
                return new SubroutineCallTermSyntax(subroutineCall);
            }
            return result;
        }

        private TermSyntax ReadValueTerm()
        {
            var valueToken = tokenizer.TryReadNext();
            var indexing = ReadIndexing();

            return new ValueTermSyntax(valueToken, indexing);
        }

        private static bool IsLastVariable(Tokenizer tokenizer)
        {
            var result = true;
            var currentToken = tokenizer.TryReadNext();
            var nextToken = tokenizer.TryReadNext();
            if (nextToken != null && (nextToken.Value == "." || nextToken.Value == "("))
                result = !result;
            tokenizer.PushBack(nextToken);
            tokenizer.PushBack(currentToken);
            return result;
        }

        private Indexing ReadIndexing()
        {
            var open = tokenizer.TryReadNext();
            if (open == null || open.Value != "[")
            {
                tokenizer.PushBack(open);
                return null;
            }
            var expression = ReadExpression();
            var close = tokenizer.TryReadNext();

            return new Indexing(open, expression, close);
        }

        private static bool IsIdentifierToken(Token? token)
            => token != null && token.TokenType == TokenType.Identifier;

        private static bool IsValueToken(Token? token)
            => token != null && (token.TokenType == TokenType.IntegerConstant
                                || token.TokenType == TokenType.StringConstant
                                || token.TokenType == TokenType.Keyword);

        private static readonly IReadOnlySet<string> unaryOp = new HashSet<string>()
                { "-", "~"};

        private static bool IsUnaryOp(Token? token)
            => token != null && unaryOp.Contains(token.Value);

        private UnaryOpTermSyntax ReadUnaryOpTerm()
        {
            var unOp = tokenizer.TryReadNext();
            var term = ReadTerm();

            return new UnaryOpTermSyntax(unOp, term);
        }

        private ParenthesizedTermSyntax ReadParenthesizedTerm()
        {
            var open = tokenizer.TryReadNext();
            var expression = ReadExpression();
            var close = tokenizer.TryReadNext();

            return new ParenthesizedTermSyntax(open, expression, close);
        }

        public SubroutineCall ReadSubroutineCall()
        {
            var methodOrClass = ReadMethodOrClass();
            var name = tokenizer.Read(TokenType.Identifier);
            var open = tokenizer.Read("(");
            var expressionsList = ReadExpressionsList();
            var close = tokenizer.Read(")");

            return new SubroutineCall(methodOrClass, name, open, expressionsList, close);
        }

        private ExpressionListSyntax ReadExpressionsList()
        {
            var expressionsList = new List<ExpressionSyntax>();
            var lastToken = tokenizer.TryReadNext();
            while (lastToken != null && lastToken.Value != ")")
            {
                if (lastToken != null && lastToken.Value != ",")
                    tokenizer.PushBack(lastToken);
                var expression = ReadExpression();
                expressionsList.Add(expression);
                lastToken = tokenizer.TryReadNext();
            }
            tokenizer.PushBack(lastToken);
            return new ExpressionListSyntax(expressionsList);
        }

        private MethodObjectOrClass ReadMethodOrClass()
        {
            var name = tokenizer.Read(TokenType.Identifier);
            var dot = tokenizer.TryReadNext();
            if (dot != null && dot.Value == ".")
                return new MethodObjectOrClass(name, dot);
            tokenizer.PushBack(dot);
            tokenizer.PushBack(name);
            return null;
        }

        public ParameterListSyntax ReadParameterList()
        {
            var parameters = new List<Parameter>();
            var typeToken = tokenizer.TryReadNext();
            while (IsTypeToken(typeToken))
            {
                var varName = tokenizer.TryReadNext();
                var delimeter = tokenizer.TryReadNext();
                if (delimeter != null && delimeter.Value == ")")
                    tokenizer.PushBack(delimeter);
                var parameter = new Parameter(typeToken, varName);
                parameters.Add(parameter);
                typeToken = tokenizer.TryReadNext();
            }
            tokenizer.PushBack(typeToken);

            return new ParameterListSyntax(parameters);
        }

        private static readonly IReadOnlySet<string> types = new HashSet<string>()
            { "int", "string", "boolean", "char" , "Array"};

        private static bool IsTypeToken(Token token)
            => token != null && (token.TokenType == TokenType.Identifier ||
                               types.Contains(token.Value));
    }
}
