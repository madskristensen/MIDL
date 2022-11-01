using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace MIDLParser
{
    public partial class Document
    {
        private static readonly Regex _rxSingleLineComment = new(@"//.+");
        private static readonly Regex _rxCommentOpen = new(@"/\*");
        private static readonly Regex _rxCommentClose = new(@"\*/");
        private static readonly Regex _rxString = new(@"\""[^\""].+\""");
        private static readonly Regex _rxAttribute = new(@"(?<=\[)\w+(?=.*\])");
        private static readonly Regex _rxType = new(@"\b(asm|__asm__|auto|bool|Boolean|_Bool|char|_Complex|double|float|PWSTR|PCWSTR|_Imaginary|int|long|short|VARIANT|BSTR|string|String|Single|Double|Int16|Int32|Int64|UInt8|UInt16|UInt32|UInt64|Char|Char16|Guid|Object)\b|(?<=(event|enum|struct|runtimeclass|interface)\s*)[\w\.]+");
        private static readonly Regex _rxKeyword = new(@"^(#include|#define)|\b(true|false|signed|typedef|union|unsigned|void|enum|struct|import|VARIANT|BSTR|break|case|overridable|ref|out|const|continue|default|do|else|for|goto|if|_Pragma|return|switch|while|set|get|event|runtimeclass|apicontract|namespace|interface|delegate|static|unsealed)\b");
        private static readonly Regex _rxText = new(@"(\w|\d)+");
        private static readonly Regex _rxOp = new(@"[~.;,+\-*/()\[\]{}<>=&$!%?:|^\\]");

        public bool IsParsing { get; private set; }

        public bool IsValid { get; private set; }

        struct PendingCommentItem
        {
            public int Start { get; set; }

            public int Line { get; set; }

            public int Column { get; set; }
        }

        private PendingCommentItem? _pendingComment = null;

        public void Parse()
        {
            IsParsing = true;
            bool isSuccess = false;
            int start = 0;

            try
            {
                List<ParseItem> tokens = new();

                for (int line = 0; line < _lines.Length; ++line)
                {
                    string lineStr = _lines[line];
                    for (int column = 0; column < lineStr.Length;)
                    {
                        // Trim white space
                        while (column < lineStr.Length && lineStr[column] == ' ')
                        {
                            column++;
                        }
                        int oldColumn = column;
                        ParseItem? current = ParseLine(start, line, ref column, lineStr);

                        if (current != null)
                        {
                            tokens.Add(current);
                        }
                        else if (oldColumn == column)
                        {
                            break;
                        }
                    }

                    start += lineStr.Length;
                }

                Items = tokens.OrderBy(i => i.Start).ToList();

                ValidateDocument();

                isSuccess = true;
            }
            finally
            {
                IsParsing = false;

                if (isSuccess)
                {
                    Parsed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private ParseItem? ParseLine(int start, int line, ref int column, string lineStr)
        {
            if (_pendingComment is PendingCommentItem commentItem)
            {
                // Comment close
                if (IsMatch(_rxCommentClose, lineStr, ref column, out Match matchCommendClose))
                {
                    _pendingComment = null;
                    string text = FindText(commentItem.Line, commentItem.Column, line, column);
                    return ToParseItem(text, commentItem.Start, ItemType.Comment);
                }
                else
                {
                    column += 1;
                    return null;
                }
            }

            // Single line comment
            if (IsMatch(_rxSingleLineComment, lineStr, ref column, out Match matchComment))
            {
                return ToParseItem(matchComment, start, ItemType.Comment)!;
            }

            // Comment open
            if (IsMatch(_rxCommentOpen, lineStr, ref column, out Match matchCommentStart))
            {
                _pendingComment = new()
                {
                    Start = start + matchCommentStart.Index,
                    Column = column - matchCommentStart.Length,
                    Line = line,
                };
                return null;
            }

            // Keywords
            if (IsMatch(_rxKeyword, lineStr, ref column, out Match matchVar))
            {
                return ToParseItem(matchVar, start, ItemType.Keyword)!;
            }

            // Types
            if (IsMatch(_rxType, lineStr, ref column, out Match matchType))
            {
                return ToParseItem(matchType, start, ItemType.Type)!;
            }

            // Attributes
            if (IsMatch(_rxAttribute, lineStr, ref column, out Match? matchAttr))
            {
                return ToParseItem(matchAttr, start, ItemType.Type)!;
            }

            // Strings
            if (IsMatch(_rxString, lineStr, ref column, out Match matchStrings))
            {
                return ToParseItem(matchStrings, start, ItemType.String)!;
            }

            // Op
            if (IsMatch(_rxOp, lineStr, ref column, out Match _))
            {
                return null;
            }

            // Text
            if (IsMatch(_rxText, lineStr, ref column, out Match _))
            {
                return null;
            }
            return null;
        }

        private string FindText(int startLine, int startColumn, int endLine, int endColumn)
        {
            bool isEndOnSameLine = endLine == startLine;
            string startLineStr = _lines[startLine];
            string text = startLineStr.Substring(startColumn, isEndOnSameLine ? endColumn - startColumn : startLineStr.Length - startColumn);
            for (int line = startLine + 1; line <= endLine; ++line)
            {
                // TODO: Detect newline characters in doc
                text += "\n";
                string lineStr = _lines[line];
                text += line == endLine ? lineStr.Substring(0, endColumn) : lineStr;
            }
            return text;
        }

        public static bool IsMatch(Regex regex, string line, ref int at, out Match match)
        {
            match = regex.Match(line, at);
            bool result = match.Success && match.Index == at;
            if (result)
            {
                at += match.Length;
            }
            return result;
        }

        private ParseItem ToParseItem(string line, int start, ItemType type)
        {
            ParseItem? item = new(start, line, this, type);
            return item;
        }

        private ParseItem? ToParseItem(Match match, int start, ItemType type)
        {
            if (string.IsNullOrEmpty(match.Value))
            {
                return null;
            }

            return ToParseItem(match.Value, start + match.Index, type);
        }

        private void ValidateDocument()
        {
            IsValid = true;
            foreach (ParseItem item in Items.Where(i => i.Type == ItemType.Type))
            {
                if (_convertTypes.ContainsKey(item.Text))
                {
                    item.Errors.Add(Errors.PL001.WithFormat(item.Text, _convertTypes[item.Text]));
                    IsValid = false;
                }
            }
        }

        public static readonly Dictionary<string, string> _convertTypes = new()
        {
            {"int", "Int32"},
            {"short", "Int16"},
            {"long", "Int32"},
            {"PWSTR", "String"},
            {"PCWSTR", "String"},
            {"double", "Double"},
            {"float", "Single"},
            {"string", "String" },
        };

        private class Errors
        {
            public static Error PL001 { get; } = new("IDL001", "Use {1} instead of {0}.", ErrorCategory.Error);
            public static Error PL002 { get; } = new("PL002", "\"{0}\" is not a valid absolute URI", ErrorCategory.Warning);
        }


        public event EventHandler? Parsed;
    }
}
