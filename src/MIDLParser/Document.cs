using System.Collections.Generic;
using System.Linq;

namespace MIDLParser
{
    public partial class Document
    {
        private string[] _lines;

        protected Document(string[] lines)
        {
            _lines = lines;
            Parse();
        }

        public List<ParseItem> Items { get; private set; } = new List<ParseItem>();

        public void UpdateLines(string[] lines)
        {
            _lines = lines;
        }

        public static Document FromLines(params string[] lines)
        {
            Document? doc = new(lines);
            return doc;
        }

        public ParseItem? FindItemFromPosition(int position)
        {
            return Items.LastOrDefault(t => t.Contains(position));
        }
    }
}
