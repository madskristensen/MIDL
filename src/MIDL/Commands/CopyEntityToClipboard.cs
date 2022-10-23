using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MIDLParser;
using static Community.VisualStudio.Toolkit.Windows;
using OutputWindowPane = Community.VisualStudio.Toolkit.OutputWindowPane;

namespace MIDL
{
    [Command(PackageIds.CopyEntityToClipboard)]
    internal class CopyEntityToClipboard : BaseCommand<CopyEntityToClipboard>
    {
        private static readonly Regex _rxIdentifier = new(@"(\w|\d)+");

        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                // Get active text
                await VS.StatusBar.ShowMessageAsync("Getting identifier...");
                DocumentView? docView = await VS.Documents.GetActiveDocumentViewAsync();
                if (docView == null)
                {
                    throw new InvalidOperationException("Failed to get active document view");
                }
                IWpfTextView textView = docView.TextView;
                ITextSelection selection = textView.Selection;
                int position = selection.Start.Position.Position;
                ITextSnapshotLine lineSnapshot = selection.Start.Position.GetContainingLine();
                string line = lineSnapshot.GetText();
                // TODO: Is there anyway to get the parsed items directly? The syntax highlighter
                // should have already ran a parsing pass.
                MIDLParser.Document parser = MIDLParser.Document.FromLines(line);
                parser.Parse();
                // Get the identifier.
                // TODO: This is a crude approximation. Add identifier to the parser
                // Right now it would produce incorrect result for event where the generic parameter is seen as an identifier
                string id = line;
                foreach (ParseItem item in parser.Items)
                {
                    id = id.Replace(item.Text, "");
                }
                id = id.Trim();
                Match idMatch = _rxIdentifier.Match(id);
                if (!idMatch.Success)
                {
                    throw new InvalidOperationException("Failed to find identifier");
                }
                id = idMatch.Value;
                // Generate header
                await VS.StatusBar.ShowMessageAsync("Generating file...");
                PhysicalFile idlFile = await VS.Solutions.GetActiveItemAsync() as PhysicalFile;
                // TODO: Make it language agnostic
                ProcessResult result = await idlFile.TransformToHeaderAsync();
                if (!result.Success)
                {
                    throw new InvalidOperationException("Failed to generate header file");
                }
                string[] headerFileLines = File.ReadAllLines(result.HeaderFile);
                List<string> output = new();
                foreach (string headerLine in headerFileLines)
                {
                    // TODO: Make it language agnostic
                    // TODO: Seem we need a C++ parser to accurately grab the relevant lines...?
                    if (headerLine.Contains(id) && !headerLine.Contains("struct") && !headerLine.Contains("#") && !headerLine.Contains("namespace"))
                    {
                        output.Add(headerLine);
                    }
                }
                Clipboard.SetText(string.Join(Environment.NewLine, output));
                await VS.StatusBar.ShowMessageAsync("Copied");
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync("Failed to copy to clipboard. See output window for details");
                await ex.LogAsync();
            }
        }
    }
}
