using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Merge.VsPackage;
using Microsoft.VisualStudio.Shell.Interop;
using static Community.VisualStudio.Toolkit.Windows;

namespace MIDL
{
    [Command(PackageIds.MyCommand)]
    internal sealed class UpdateHeaderFile : BaseCommand<UpdateHeaderFile>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (VsShellUtilities.IsSolutionBuilding(Package))
            {
                await VS.MessageBox.ShowAsync("Header files can't be updated while a build is in progress");
                return;
            }

            await VS.StatusBar.ShowProgressAsync("Generating header file...", 1, 3);

            try
            {
                PhysicalFile idlFile = await VS.Solutions.GetActiveItemAsync() as PhysicalFile;
                ProcessResult result = await idlFile.TransformToHeaderAsync();

                if (!result.Success)
                {
                    await VS.StatusBar.ShowProgressAsync("", 2, 2);
                    await VS.StatusBar.ShowMessageAsync("Error generating header file. Make sure the project builds");

                    if (!string.IsNullOrEmpty(result.Output))
                    {
                        OutputWindowPane output = await VS.Windows.GetOutputWindowPaneAsync(VSOutputWindowPane.General);
                        await output.WriteLineAsync(result.Output);
                        await output.ActivateAsync();
                    }

                    return;
                }

                string headerFile = Path.ChangeExtension(idlFile.FullPath, ".h");

                // First time header is generated
                if (!File.Exists(headerFile))
                {
                    await CreateHeaderFileAsync(headerFile, result.HeaderFile);
                    await VS.Documents.OpenViaProjectAsync(headerFile);
                    await VS.StatusBar.ShowProgressAsync("Header file created from IDL", 2, 2);
                }

                // Subsequent generations
                else
                {
                    await VS.StatusBar.ShowProgressAsync("Preparing header file merge...", 2, 3);

                    string ideDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    string teamDir = Path.Combine(ideDir, "CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer");

                    await MergeHeaderFilesAsync(headerFile, result.HeaderFile);
                }

                await Task.Delay(2000);
                await VS.StatusBar.ShowProgressAsync("Ready", 1, 1);
                await VS.StatusBar.ClearAsync();
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync("Error generating header file. See output window for details");
                await ex.LogAsync();
            }
        }

        private async Task CreateHeaderFileAsync(string fileName, string generatedFile)
        {
            File.Copy(generatedFile, fileName, false);
            Project project = await VS.Solutions.GetActiveProjectAsync();
            await project.AddExistingFilesAsync(fileName);

        }

        private static async Task MergeHeaderFilesAsync(string projectFile, string generatedFile)
        {
            string projectFileName = Path.GetFileName(projectFile);
            string baseFile = Path.Combine(Path.GetTempPath(), projectFileName);
            if (File.Exists(baseFile))
            {
                File.Delete(baseFile);
            }
            StripDiff(baseFile, projectFile, generatedFile);
            string resultFile = Path.Combine(Path.GetTempPath(), 
                $"{Path.GetFileNameWithoutExtension(projectFile)}_result.{Path.GetExtension(projectFile)}");
            File.Copy(projectFile, resultFile, true);

            IModernMergeService mergeService = await GetMergeServiceAsync();

            mergeService.OpenAndRegisterMergeWindow(fileName: "IDL Merge",
                                                        leftFilePath: projectFile,
                                                        rightFilePath: generatedFile,
                                                        baseFilePath: baseFile,
                                                        resultFilePath: resultFile,
                                                        leftFileTag: "Local",
                                                        rightFileTag: "Generated",
                                                        baseFileTag: "Base file",
                                                        resultFileTag: "Result",
                                                        leftFileTitle: projectFileName,
                                                        rightFileTitle: "from IDL",
                                                        baseFileTitle: "Base",
                                                        resultFileTitle: projectFileName,
                                                        callbackParam: null,
                                                        onMergeComplete: (r) =>
                                                        {
                                                            if (r.MergeAccepted)
                                                            {
                                                                File.Copy(resultFile, projectFile, true);
                                                            }
                                                        });
        }

        private static async Task<IModernMergeService> GetMergeServiceAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsShell shell = await VS.Services.GetShellAsync();
            Guid mergePackageId = new("BF0F8831-2CA2-4057-B64E-FF1CED3CEFA2");
            shell.LoadPackage(ref mergePackageId, out _);
            IModernMergeService mergeService = await VS.GetServiceAsync<SModernMergeService, IModernMergeService>();

            return mergeService;
        }

        private static void StripDiff(string resultFileName, string file1, string file2)
        {
            string[] lines1 = File.ReadAllLines(file1);
            string[] lines2 = File.ReadAllLines(file2);

            File.WriteAllLines(resultFileName, lines2.Intersect(lines1));
        }
    }
}