using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using MIDLParser;

namespace MIDL
{
    public class SuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly IClassifier _aggregator;

        public SuggestedActionsSource(IClassifier aggregator)
        {
            _aggregator = aggregator;
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged
        {
            add { }
            remove { }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            IList<ClassificationSpan> spans = _aggregator.GetClassificationSpans(range);
            List<SuggestedActionSet> actionSets = new();

            foreach (ClassificationSpan span in spans.Where(s => s.ClassificationType.IsOfType(PredefinedClassificationTypeNames.SymbolDefinition)))
            {
                string text = span.Span.GetText();

                if (Document._convertTypes.ContainsKey(text))
                {
                    FixTypeAction ignoreAction = new(span.Span, text, Document._convertTypes[text]);
                    SuggestedActionSet ignoreSet = new(PredefinedSuggestedActionCategoryNames.CodeFix, new[] { ignoreAction }, "Title", SuggestedActionSetPriority.High, span.Span);
                    actionSets.Add(ignoreSet);
                }
            }

            return actionSets;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            IList<ClassificationSpan> spans = _aggregator.GetClassificationSpans(range);

            foreach (ClassificationSpan span in spans.Where(s => s.ClassificationType.IsOfType(PredefinedClassificationTypeNames.SymbolDefinition)))
            {
                string text = span.Span.GetText();

                if (Document._convertTypes.ContainsKey(text))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
