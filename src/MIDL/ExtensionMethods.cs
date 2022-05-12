using Microsoft.VisualStudio.Text;
using MIDLParser;

namespace MIDL
{
    public static class ExtensionMethods
    {
        public static Span ToSpan(this ParseItem token) => new(token.Start, token.Length);

        public static IdlDocument GetDocument(this ITextBuffer buffer) =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new IdlDocument(buffer));
    }
}
