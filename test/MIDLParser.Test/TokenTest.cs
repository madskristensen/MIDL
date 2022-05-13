using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MIDLParser.Test
{
    [TestClass]
    public class TokenTest
    {
        private const string _file = @"// Photo.idl
#include ""NamespaceRedirect.h""

namespace PhotoEditor
{
    delegate void RecognitionHandler(Boolean arg); // delegate type, for an event.
    
    [default_interface]
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

        [TestMethod]
        public void ParserTest()
        {
            var lines = _file.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var parser = Document.FromLines(lines);

            Assert.AreEqual(ItemType.Comment, parser.Items.First().Type);
            Assert.AreEqual(ItemType.String, parser.Items.ElementAt(2).Type);
            Assert.AreEqual(21, parser.Items.Count);
        }

        [TestMethod]
        public void ValidationTest()
        {
            var lines = _file.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var parser = Document.FromLines(lines);

            Assert.AreEqual(1, parser.Items.ElementAt(13).Errors.Count);
        }
    }
}
