using System.Diagnostics;
using System.IO;
using Microsoft.Build.Execution;
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
                ProcessResult result = idlFile.TransformToHeader();

                if (!result.Success)
                {
                    await VS.StatusBar.ShowProgressAsync("Error generating header file", 2, 2);

                    OutputWindowPane output = await VS.Windows.GetOutputWindowPaneAsync(VSOutputWindowPane.General);
                    await output.WriteLineAsync(result.Output);
                    await output.ActivateAsync();

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

                    MergeHeaderFiles(teamDir, headerFile, result.HeaderFile);
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
            finally
            {
                await Task.Delay(2000);
                await VS.StatusBar.ShowProgressAsync("Ready", 1, 1);
                await VS.StatusBar.ClearAsync();
            }
        }

        private async Task CreateHeaderFileAsync(string fileName, string generatedFile)
        {
            File.Copy(generatedFile, fileName, false);
            Project project = await VS.Solutions.GetActiveProjectAsync();
            await project.AddExistingFilesAsync(fileName);
        }

        private static void MergeHeaderFiles(string teamDir, string projectFile, string generatedFile)
        {
            string args = $"\"{projectFile}\" \"{generatedFile}\" \"{projectFile}\" \"{projectFile}\" /m";

            ProcessStartInfo start = new()
            {
                FileName = "vsdiffmerge.exe",
                Arguments = args,
                WorkingDirectory = teamDir,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(start);
        }
    }
}
