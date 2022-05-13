using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MIDL
{
    [ComVisible(true)]
    [Guid(PackageGuids.MidlEditorFactoryString)]
    public class LanguageFactory : LanguageBase
    {
        public const string LanguageName = "IDL";
        public const string FileExtension = ".idl";

        public LanguageFactory(object site) : base(site)
        { }

        public void RegisterLanguageService(Package package)
        {
            ((IServiceContainer)package).AddService(GetType(), this, true);
        }

        public override string Name => LanguageName;

        public override string[] FileExtensions => new[] { FileExtension };

        public override void SetDefaultPreferences(LanguagePreferences preferences)
        {
            preferences.EnableCodeSense = false;
            preferences.EnableMatchBraces = true;
            preferences.EnableMatchBracesAtCaret = true;
            preferences.EnableShowMatchingBrace = true;
            preferences.EnableCommenting = true;
            preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
            preferences.LineNumbers = true;
            preferences.MaxErrorMessages = 100;
            preferences.AutoOutlining = false;
            preferences.MaxRegionTime = 2000;
            preferences.InsertTabs = false;
            preferences.IndentSize = 4;
            preferences.IndentStyle = IndentingStyle.Smart;
            preferences.ShowNavigationBar = false;

            preferences.WordWrap = false;
            preferences.WordWrapGlyphs = true;

            preferences.AutoListMembers = true;
            preferences.EnableQuickInfo = true;
            preferences.ParameterInformation = true;
        }
    }
}
