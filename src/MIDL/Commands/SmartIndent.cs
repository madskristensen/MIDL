using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace MIDL.Commands
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(SmartIndent))]
    [ContentType(LanguageFactory.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class SmartIndent : ICommandHandler<ReturnKeyCommandArgs>
    {
        private static readonly Regex _rxLead = new(@"^\s+", RegexOptions.Compiled);

        public string DisplayName => nameof(SmartIndent);

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            ITextView view = args.TextView;
            int position = view.Caret.Position.BufferPosition.Position;

            bool shouldIndent = ShouldIndent(view, position);

            if (shouldIndent)
            {
                return Indent(view, position);
            }

            return false;
        }

        private static bool ShouldIndent(ITextView view, int position)
        {
            if (!view.Selection.IsEmpty || position == 0 || position == view.TextBuffer.CurrentSnapshot.Length)
            {
                return false;
            }

            string prevChar = view.TextBuffer.CurrentSnapshot.GetText(position - 1, 1);

            if (prevChar != "{")
            {
                return false;
            }

            string nextChar = view.TextBuffer.CurrentSnapshot.GetText(position, 1);

            if (nextChar != "}")
            {
                return false;
            }

            return true;
        }

        private static bool Indent(ITextView view, int position)
        {
            ITextSnapshotLine line = view.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);
            Match leadingWhitespace = _rxLead.Match(line.GetText());
            int indentation = view.Options.IsConvertTabsToSpacesEnabled() ? view.Options.GetIndentSize() : view.Options.GetTabSize();
            string newLineChar = view.Options.GetNewLineCharacter();

            if (leadingWhitespace.Success)
            {
                using (ITextEdit edit = view.TextBuffer.CreateEdit())
                {
                    string firstEdit = newLineChar + leadingWhitespace.Value;
                    edit.Insert(position - 1, firstEdit);

                    string secondEdit = newLineChar + leadingWhitespace.Value + "".PadLeft(indentation);
                    edit.Insert(position, secondEdit);

                    string thirdEdit = newLineChar + leadingWhitespace.Value;
                    edit.Insert(position, thirdEdit);

                    edit.Apply();

                    view.Caret.MoveTo(new SnapshotPoint(view.TextBuffer.CurrentSnapshot, position + firstEdit.Length + secondEdit.Length));
                }

                return true;
            }

            return false;
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}
