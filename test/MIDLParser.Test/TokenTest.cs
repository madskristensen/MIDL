using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MIDLParser.Test
{
    [TestClass]
    public class TokenTest
    {
        private const string _rawFile = @"// Photo.idl
/*Comment*/
/*Longer runtimeclass
namespace
Comment*/

#include ""NamespaceRedirect.h""

namespace PhotoEditor
{
    delegate void RecognitionHandler(Boolean arg); // delegate type, for an event.
    
    [default_interface(""http://foo.com"")]
    [webhosthidden]
    runtimeclass Photo : Windows.UI.Xaml.Data.INotifyPropertyChanged // interface.
    {
        Photo(); // constructors.
        Photo(Windows.Storage.StorageFile imageFile);

        string ImageName{ get; }; // read-only property.
        Single SepiaIntensity; // read-write property.

        Windows.Foundation.IAsyncAction StartRecognitionAsync(); // (asynchronous) method.

        event RecognitionHandler ImageRecognized; // event.
    }
}";

        private static readonly string _canonicalFile = GetCanonicalFile();

        private static string GetCanonicalFile()
        {
            return _rawFile.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        }

        [TestMethod]
        public void ParserTest()
        {
            var lines = _canonicalFile.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var parser = Document.FromLines(lines);

            Assert.AreEqual(ItemType.Comment, parser.Items.First().Type);
            Assert.AreEqual(ItemType.Comment, parser.Items[1].Type);
            Assert.AreEqual(ItemType.Comment, parser.Items[2].Type);
            Assert.AreEqual(ItemType.PreprocessorDirective, parser.Items[3].Type);
            Assert.AreEqual(ItemType.String, parser.Items[4].Type);
            Assert.AreEqual(ItemType.Keyword, parser.Items[5].Type);
            Assert.AreEqual(26, parser.Items.Count);
        }

        [TestMethod]
        public void ValidationTest()
        {
            var lines = _canonicalFile.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var parser = Document.FromLines(lines);

            Assert.AreEqual(1, parser.Items.ElementAt(17).Errors.Count);
        }
    }
}
