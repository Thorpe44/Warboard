using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class MiniJson
{
    public static object Deserialize(string json)
    {
        if (json == null)
            return null;

        return Parser.Parse(json);
    }

    private sealed class Parser : IDisposable
    {
        private const string WordBreak =
            "{}[],:\"";

        private StringReader json;

        private Parser(string jsonString)
        {
            json =
                new StringReader(
                    jsonString
                );
        }

        public static object Parse(
            string jsonString)
        {
            using (Parser instance =
                new Parser(jsonString))
            {
                return instance.ParseValue();
            }
        }

        public void Dispose()
        {
            json.Dispose();
            json = null;
        }

        private Dictionary<string, object>
            ParseObject()
        {
            Dictionary<string, object> table =
                new Dictionary<string, object>();

            json.Read();

            while (true)
            {
                TOKEN next =
                    NextToken;

                if (next == TOKEN.NONE)
                    return null;

                if (next == TOKEN.CURLY_CLOSE)
                {
                    json.Read();
                    return table;
                }

                string name =
                    ParseString();

                if (name == null)
                    return null;

                if (NextToken != TOKEN.COLON)
                    return null;

                json.Read();

                table[name] =
                    ParseValue();

                next =
                    NextToken;

                if (next == TOKEN.COMMA)
                {
                    json.Read();
                    continue;
                }

                if (next ==
                    TOKEN.CURLY_CLOSE)
                {
                    json.Read();
                    return table;
                }

                return null;
            }
        }

        private List<object> ParseArray()
        {
            List<object> array =
                new List<object>();

            json.Read();

            bool parsing = true;

            while (parsing)
            {
                TOKEN next =
                    NextToken;

                switch (next)
                {
                    case TOKEN.NONE:
                        return null;

                    case TOKEN.SQUARED_CLOSE:
                        json.Read();
                        parsing = false;
                        break;

                    case TOKEN.COMMA:
                        json.Read();
                        break;

                    default:
                        array.Add(
                            ParseByToken(next)
                        );
                        break;
                }
            }

            return array;
        }

        private object ParseValue()
        {
            TOKEN next =
                NextToken;

            return ParseByToken(next);
        }

        private object ParseByToken(
            TOKEN token)
        {
            switch (token)
            {
                case TOKEN.STRING:
                    return ParseString();

                case TOKEN.NUMBER:
                    return ParseNumber();

                case TOKEN.CURLY_OPEN:
                    return ParseObject();

                case TOKEN.SQUARED_OPEN:
                    return ParseArray();

                case TOKEN.TRUE:
                    return true;

                case TOKEN.FALSE:
                    return false;

                case TOKEN.NULL:
                    return null;
            }

            return null;
        }

        private string ParseString()
        {
            StringBuilder builder =
                new StringBuilder();

            json.Read();

            bool parsing = true;

            while (parsing)
            {
                int read =
                    json.Read();

                if (read == -1)
                    break;

                char c = (char)read;

                if (c == '"')
                {
                    parsing = false;
                    break;
                }

                if (c == '\\')
                {
                    int escaped =
                        json.Read();

                    if (escaped == -1)
                        break;

                    c = (char)escaped;

                    switch (c)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(c);
                            break;

                        case 'b':
                            builder.Append('\b');
                            break;

                        case 'f':
                            builder.Append('\f');
                            break;

                        case 'n':
                            builder.Append('\n');
                            break;

                        case 'r':
                            builder.Append('\r');
                            break;

                        case 't':
                            builder.Append('\t');
                            break;

                        case 'u':
                            char[] hex =
                                new char[4];

                            for (int i = 0;
                                 i < 4;
                                 i++)
                            {
                                hex[i] =
                                    (char)json.Read();
                            }

                            uint code;

                            if (uint.TryParse(
                                new string(hex),
                                NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture,
                                out code))
                            {
                                builder.Append(
                                    (char)code
                                );
                            }

                            break;
                    }
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private object ParseNumber()
        {
            string number =
                NextWord;

            if (number.IndexOf('.') == -1 &&
                number.IndexOf('e') == -1 &&
                number.IndexOf('E') == -1)
            {
                long integer;

                if (long.TryParse(
                    number,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out integer))
                {
                    return integer;
                }
            }

            double floating;

            if (double.TryParse(
                number,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out floating))
            {
                return floating;
            }

            return 0d;
        }

        private void EatWhitespace()
        {
            while (char.IsWhiteSpace(
                PeekChar))
            {
                json.Read();

                if (json.Peek() == -1)
                    break;
            }
        }

        private char PeekChar
        {
            get
            {
                int peek =
                    json.Peek();

                return peek == -1
                    ? '\0'
                    : Convert.ToChar(peek);
            }
        }

        private char NextChar
        {
            get
            {
                int next =
                    json.Read();

                return next == -1
                    ? '\0'
                    : Convert.ToChar(next);
            }
        }

        private string NextWord
        {
            get
            {
                StringBuilder builder =
                    new StringBuilder();

                while (!IsWordBreak(
                    PeekChar))
                {
                    builder.Append(
                        NextChar
                    );

                    if (json.Peek() == -1)
                        break;
                }

                return builder.ToString();
            }
        }

        private TOKEN NextToken
        {
            get
            {
                EatWhitespace();

                if (json.Peek() == -1)
                    return TOKEN.NONE;

                char c =
                    PeekChar;

                switch (c)
                {
                    case '{':
                        return TOKEN.CURLY_OPEN;
                    case '}':
                        return TOKEN.CURLY_CLOSE;
                    case '[':
                        return TOKEN.SQUARED_OPEN;
                    case ']':
                        return TOKEN.SQUARED_CLOSE;
                    case ',':
                        return TOKEN.COMMA;
                    case '"':
                        return TOKEN.STRING;
                    case ':':
                        return TOKEN.COLON;
                    case '-':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return TOKEN.NUMBER;
                }

                string word =
                    NextWord;

                switch (word)
                {
                    case "false":
                        return TOKEN.FALSE;
                    case "true":
                        return TOKEN.TRUE;
                    case "null":
                        return TOKEN.NULL;
                }

                return TOKEN.NONE;
            }
        }

        private static bool IsWordBreak(
            char c)
        {
            return
                char.IsWhiteSpace(c) ||
                WordBreak.IndexOf(c) != -1;
        }

        private enum TOKEN
        {
            NONE,
            CURLY_OPEN,
            CURLY_CLOSE,
            SQUARED_OPEN,
            SQUARED_CLOSE,
            COLON,
            COMMA,
            STRING,
            NUMBER,
            TRUE,
            FALSE,
            NULL
        }
    }
}
