// <#+
#if !T4
namespace UnionTypes.Toolkit.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Text;
#endif

#nullable enable

    /// <summary>
    /// A class that helps generate formatted and indented code text. 
    /// It provides methods to write text, manage indentation, and handle code blocks.
    /// </summary>
    public class CodeWriter
    {
        private StringBuilder _builder = new StringBuilder();
        private string _lineStartIndentation = "";
        private const string _indent = "    ";
        private bool _isLineStart = true;

        /// <summary>
        /// Return formatted text.
        /// </summary>
        public string WrittenText => _builder.ToString();

        /// <summary>
        /// Writes the text, starting on a new indented line if necessary.
        /// </summary>
        public void Write(string text)
        {
            if (_isLineStart)
            {
                _builder.Append(_lineStartIndentation);
                _isLineStart = false;
            }

            _builder.Append(text);
        }

        /// <summary>
        /// Writes the text followed by a newline, starting on a new indented line if necessary.
        /// </summary>
        public void WriteLine(string? text = null)
        {
            if (text != null)
                Write(text);
            _builder.AppendLine();
            _isLineStart = true;
        }

        /// <summary>
        /// Writes the text on a new indented line.
        /// </summary>
        public void WriteLineNested(string text)
        {
            WriteNested(() => WriteLine(text));
        }

        /// <summary>
        /// Indents all the text written by the action.
        /// </summary>
        public void WriteNested(Action action)
        {
            var oldLineStartIndentation = _lineStartIndentation;
            _lineStartIndentation = _lineStartIndentation + _indent;
            action();
            _lineStartIndentation = oldLineStartIndentation;
        }

        /// <summary>
        /// Writes the open string on a new line, all the text written by the action is indented starting on the line after, and writes the close string on a new line after the action.
        /// </summary>
        public void WriteNested(string open, string close, Action action)
        {
            if (!_isLineStart)
                WriteLine();
            WriteLine(open);
            WriteNested(action);
            WriteLine(close);
        }

        /// <summary>
        /// Writes the text written by the action, on new lines indented between the an open and close brace, each on separate lines.
        /// </summary>
        /// <param name="action"></param>
        public void WriteBraceNested(Action action)
        {
            WriteNested("{", "}", action);
        }

        private List<string> _blocks = default!;

        /// <summary>
        /// Guarantees that blocks written by the action are separated by a single blank line.
        /// </summary>
        public void WriteLineSeparatedBlocks(Action action)
        {
            var oldBuilder = _builder;
            var oldBlocks = _blocks;
            _builder = new StringBuilder();
            _blocks = new List<string>();

            action();

            _builder = oldBuilder;

            if (_blocks.Count > 0)
            {
                _builder.Append(string.Join(Environment.NewLine, _blocks));
            }

            _blocks = oldBlocks;
        }

        /// <summary>
        /// Identifies the text written by the action as a single block, as understood by the <see cref="WriteLineSeparatedBlocks"/> method.
        /// </summary>
        public void WriteBlock(Action action)
        {
            // any writes outside of WriteBlock is treated as a separate block
            if (_builder.Length > 0)
            {
                _blocks.Add(_builder.ToString());
                _builder.Clear();
            }

            action();

            if (_builder.Length > 0)
            {
                _blocks.Add(_builder.ToString());
                _builder.Clear();
            }
        }

        /// <summary>
        /// Writes a blank line between each action
        /// </summary>
        public void WriteLineSeparated(params Action[] actions)
        {
            WriteLineSeparatedBlocks(() =>
            {
                foreach (var action in actions)
                {
                    WriteBlock(action);
                }
            });
        }

        private bool _firstListElement = false;

        /// <summary>
        /// Elements written by the action are separated by commas.
        /// </summary>
        public void WriteCommaList(Action action)
        {
            var oldFirstListElement = _firstListElement;
            _firstListElement = true;
            action();
            _firstListElement = oldFirstListElement;
        }

        /// <summary>
        /// The text written by the action is considered an element of a comma-separated list, formatted by the outer calls to <see cref="WriteCommaList"/>.
        /// </summary>
        public void WriteCommaListElement(Action action)
        {
            if (!_firstListElement)
                Write(", ");
            action();
            _firstListElement = false;
        }

        /// <summary>
        /// Turns name into a lower-case first letter name, e.g. "MyName" becomes "myName"
        /// </summary>
        public static string LowerName(string name)
        {
            if (!char.IsLower(name[0]))
                return char.ToLower(name[0]) + name.Substring(1);
            return name;
        }

        /// <summary>
        /// Turns name into a upper-case first letter name, e.g. "myName" becomes "MyName"
        /// </summary>
        public static string UpperName(string name)
        {
            if (!char.IsUpper(name[0]))
                return char.ToUpper(name[0]) + name.Substring(1);
            return name;
        }
    }

#if !T4
}
#endif
// #>