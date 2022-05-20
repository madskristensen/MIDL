using System.Diagnostics;
using System.IO;

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
            string ideDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string teamDir = Path.Combine(ideDir, "CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer");

            PhysicalFile idlFile = await VS.Solutions.GetActiveItemAsync() as PhysicalFile;
            string projectFile = Path.ChangeExtension(idlFile.FullPath, ".h");
            string generatedFile = GenerateNewHeaderFile(idlFile.FullPath);

            // First time header is generated
            if (!File.Exists(projectFile))
            {
                await CreateHeaderFileAsync(projectFile, generatedFile);
                await VS.Documents.OpenViaProjectAsync(projectFile);
                await VS.StatusBar.ShowMessageAsync("Header file created from IDL");
            }

            // Subsequent generations
            else
            {
                await VS.StatusBar.ShowMessageAsync("Preparing header file merge...");
                MergeHeaderFiles(teamDir, projectFile, generatedFile);
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

        private string GenerateNewHeaderFile(string idlFile)
        {
            // TODO: generate file based on Alexander's .cvproj file
            return idlFile;
        }
    }
}
