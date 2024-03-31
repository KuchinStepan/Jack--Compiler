using NUnit.Framework.Internal.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace JackCompiling
{
    public class Tokenizer
    {
        private int currentIndex = -1;
        private int wordsCount;
        private readonly List<string> words;
        private readonly Stack<Token> tokensStack = new Stack<Token>();

        private readonly IReadOnlySet<string> symbols = new HashSet<string>() { "{", "}", "(", ")",
                "[", "]", ".", ",", ";", "+", "-", "*", "/", "&", "|", "<", ">", "=", "~" };
        private readonly IReadOnlySet<string> keywords = new HashSet<string>() { "class", "constructor", 
                "function", "method", "field", "static", "var", "int", "char", "boolean", "void", "true",
                "false", "null", "this", "let", "do", "if", "else", "while", "return"};

        public Tokenizer(string text)
        {
            words = RemoveComments(text);
            wordsCount = words.Count;
        }

        private List<string> RemoveComments(string text)
        {
            var length = text.Length;
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                if (text[i] == '/' && i + 1 < length && text[i + 1] == '*')
                {
                    while (i < length && !(text[i] == '/' && i - 2 > -1 
                        && text[i - 1] == '*' && text[i - 2] != '/'))
                        i++;
                    i++;
                }
                if (i >= length)
                    break;
                stringBuilder.Append(text[i]);
            }
            text = stringBuilder.ToString();
            stringBuilder.Clear();
            length = text.Length;
            var inStringConstant = false;
            for (var i = 0; i < length; i++)
            {
                if (text[i] == '/' && i + 1 < length && text[i + 1] == '/' && !inStringConstant)
                    while (i < length && text[i] != '\n')
                        i++;
                if (i == length)
                    break;

                if (!inStringConstant)
                    if (text[i] == '"' || symbols.Contains(text[i].ToString()))
                        stringBuilder.Append(' ');
                if (text[i] == '"')
                    inStringConstant = !inStringConstant;
                if (text[i] == ' ' && inStringConstant)
                    stringBuilder.Append('@');
                else
                    stringBuilder.Append(text[i]);

                if (!inStringConstant && i + 1 < length)
                {
                    if (text[i] == '"' || symbols.Contains(text[i].ToString()))
                        stringBuilder.Append(' ');
                    if (char.IsDigit(text[i]) && symbols.Contains(text[i+1].ToString()))
                        stringBuilder.Append(' ');
                    if (char.IsDigit(text[i+1]) && symbols.Contains(text[i].ToString()))
                        stringBuilder.Append(' ');
                }
            }
            text = stringBuilder.ToString();

            return text.Split(new char[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }


        /// <summary>
        /// Сначала возвращает все токены, которые вернули методом PushBack в порядке First In Last Out.
        /// Потом читает и возвращает один следующий токен, либо null, если больше токенов нет.
        /// Пропускает пробелы и комментарии.
        ///
        /// Хорошо, если внутри Token сохранит ещё и строку и позицию в исходном тексте. Но это не проверяется тестами.
        /// </summary>
        public Token? TryReadNext()
        {
            if (tokensStack.Count > 0)
                return tokensStack.Pop();
            currentIndex++;
            if (currentIndex >= wordsCount)
                return null;

            var word = words[currentIndex];
            var isDidgit = true;
            for (var i = 0; i < word.Length && isDidgit; i++)
                if (!char.IsDigit(word[i]))
                    isDidgit = false;
            if (isDidgit)
                return new Token(TokenType.IntegerConstant, word, 0, 0);
            if (!isDidgit && char.IsDigit(word[0]))
                throw new ArgumentException($"Wrong identificator {word}");

            if (symbols.Contains(word))
                return new Token(TokenType.Symbol, word, 0, 0);

            if (keywords.Contains(word))
                return new Token(TokenType.Keyword, word, 0, 0);

            if (word[0] == '"')
            {
                if (word[word.Length - 1] != '"')
                    throw new ArgumentException("Wrong StringConstant");
                word = word.Replace('@', ' ');
                var stringConstant = word.Substring(1, word.Length - 2);
                return new Token(TokenType.StringConstant, stringConstant, 0, 0);
            }

            return new Token(TokenType.Identifier, word, 0, 0);
        }

        /// <summary>
        /// Откатывает токенайзер на один токен назад.
        /// Если token - null, то игнорирует его и никуда не возвращает.
        /// Поддержка null сделана
        /// </summary>
        public void PushBack(Token? token)
        {
            tokensStack.Push(token);
        }
    }
}
