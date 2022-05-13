using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MIDL
{
    public class FixTypeAction : ISuggestedAction
    {
        private readonly SnapshotSpan _span;
        private readonly string _oldType;
        private readonly string _newType;

        public FixTypeAction(SnapshotSpan span, string oldType, string newType)
        {
            _span = span;
            _oldType = oldType;
            _newType = newType;
        }
        public bool HasActionSets => false;
        public string DisplayText => $"Replace {_oldType} with {_newType}";
        public ImageMoniker IconMoniker => KnownMonikers.QuickReplace;
        public string IconAutomationText => $"Replace {_oldType} with {_newType}";
        public string InputGestureText => "";
        public bool HasPreview => false;
        public void Dispose()
        { }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<SuggestedActionSet> list = new SuggestedActionSet[0];
            return Task.FromResult(list);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _span.Snapshot.TextBuffer.Replace(_span, _newType);
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
