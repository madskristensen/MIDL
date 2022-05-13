using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MIDL
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name(nameof(SuggestedActionsProvider))]
    [ContentType("text")]
    public class SuggestedActionsProvider : ISuggestedActionsSourceProvider
    {
        [Import]
        private readonly IClassifierAggregatorService _service = null;

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            IClassifier aggregator = _service.GetClassifier(textBuffer);
            return textView.Properties.GetOrCreateSingletonProperty(() => new SuggestedActionsSource(aggregator));
        }
    }
}
