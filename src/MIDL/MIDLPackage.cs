global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using MIDL.Commands;

namespace MIDL
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.MIDLString)]

    [ProvideLanguageService(typeof(LanguageFactory), LanguageFactory.LanguageName, 0, ShowSmartIndent = true, DefaultToInsertSpaces = true)]
    [ProvideLanguageExtension(typeof(LanguageFactory), LanguageFactory.FileExtension)]
    [ProvideLanguageEditorOptionPage(typeof(OptionsProvider.GeneralOptions), LanguageFactory.LanguageName, null, "Advanced", null, new[] { "idl", "midl", "webidl" })]

    [ProvideEditorFactory(typeof(LanguageFactory), 341, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorExtension(typeof(LanguageFactory), LanguageFactory.FileExtension, 65535, NameResourceID = 341)]
    [ProvideEditorLogicalView(typeof(LanguageFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideFileIcon(LanguageFactory.FileExtension, "KnownMonikers.InterfaceFile")]

    [ProvideAutoLoad(PackageGuids.IdlFileSelectedString, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(PackageGuids.IdlFileSelectedString,
    name: "IDL file selected",
    expression: "idl & building",
    termNames: new[] { "idl", "building" },
    termValues: new[] { "HierSingleSelectionName:.idl$", VSConstants.UICONTEXT.NotBuildingAndNotDebugging_string })]
    public sealed class MIDLPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            LanguageFactory language = new(this);
            RegisterEditorFactory(language);
            language.RegisterLanguageService(this);

            await this.RegisterCommandsAsync();
        }
    }
}