using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using DataLayerGenerator.Options;
using DataLayerGenerator.Services;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace DataLayerGenerator.Commands
{
    internal sealed class GenerateDataLayerCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("C5D6E7F8-A9B0-4C5D-8E7F-3A4B5C6D7E8F");

        private readonly AsyncPackage package;
        private readonly DataLayerGeneratorService generatorService;
        private readonly DTE2 dte;
        private readonly GeneratorOptions options;
        private IVsOutputWindowPane outputPane;

        private GenerateDataLayerCommand(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            options = OptionsProvider.GetOptions(package);
            generatorService = new DataLayerGeneratorService(options);

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        public static GenerateDataLayerCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;

            Instance = new GenerateDataLayerCommand(package, commandService, dte);

            await Instance.InitializeOutputPaneAsync();
        }

        private async Task InitializeOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (await package.GetServiceAsync(typeof(SVsOutputWindow)) is IVsOutputWindow outWindow)
            {
                var customGuid = new Guid("C9D0E1F2-3A4B-5C6D-9E8F-0A1B2C3D4E5F");
                outWindow.CreatePane(ref customGuid, Constants.OutputPaneName, 1, 1);
                outWindow.GetPane(ref customGuid, out outputPane);
            }
        }

        private void LogMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            outputPane?.OutputStringThreadSafe($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            outputPane?.Activate();
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!(sender is OleMenuCommand command)) return;

            command.Visible = false;
            command.Enabled = false;

            if (dte?.SelectedItems == null) return;

            foreach (SelectedItem item in dte.SelectedItems)
            {
                if (item.ProjectItem?.FileNames[1] != null)
                {
                    var fileName = item.ProjectItem.FileNames[1];
                    if (Path.GetExtension(fileName).Equals(Constants.CSharpExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        command.Visible = true;
                        command.Enabled = true;
                        return;
                    }
                }
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            this.package.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ExecuteAsync();
                }
                catch (Exception ex)
                {
                    await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
                    LogMessage($"Critical error: {ex.Message}");
                    LogMessage($"Stack trace: {ex.StackTrace}");
                    ShowMessage($"Error: {ex.Message}\n\nCheck the Output Window for details.");
                }
            }).FileAndForget("DataLayerGenerator/Execute");
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            LogMessage("=== Data Layer Generator v1.0.0 ===");
            LogMessage("Starting data layer generation...");
            LogMessage($"Options: Folder={options.DataLayerFolderName}, Suffix={options.DataLayerSuffix}");
            LogMessage($"  DbContext={options.DbContextName}, GenerateInterfaces={options.GenerateInterfaces}");

            if (dte?.SelectedItems == null)
            {
                ShowMessage("No files selected.");
                return;
            }

            var selectedFiles = dte.SelectedItems.Cast<SelectedItem>()
                .Where(item =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return item.ProjectItem?.FileNames[1] != null;
                })
                .Select(item =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return new
                    {
                        Path = item.ProjectItem.FileNames[1],
                        item.ProjectItem
                    };
                })
                .Where(f => Path.GetExtension(f.Path).Equals(Constants.CSharpExtension, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!selectedFiles.Any())
            {
                ShowMessage("No C# files selected.");
                return;
            }

            LogMessage($"Processing {selectedFiles.Count} file(s)...");

            int successCount = 0;
            int failCount = 0;
            int skippedCount = 0;

            foreach (var file in selectedFiles)
            {
                var filePath = file.Path;
                LogMessage($"Analyzing: {Path.GetFileName(filePath)}");

                try
                {
                    var modelInfos = await generatorService.AnalyzeModelsAsync(filePath);

                    if (!modelInfos.Any())
                    {
                        LogMessage($"  No suitable model classes found in {Path.GetFileName(filePath)}");
                        skippedCount++;
                        continue;
                    }

                    foreach (var modelInfo in modelInfos)
                    {
                        LogMessage($"  Found model: {modelInfo.ClassName}");

                        // Show dialog to configure generation
                        var dialog = new UI.GenerateDataLayerDialog(modelInfo.ClassName, options);
                        var result = dialog.ShowDialog();

                        if (result != true)
                        {
                            LogMessage($"  Skipped by user");
                            skippedCount++;
                            continue;
                        }

                        var dataLayerOptions = dialog.GetOptions();
                        var className = modelInfo.ClassName;
                        var dataClassName = $"{className}{options.DataLayerSuffix}";

                        // Determine output folder
                        var projectPath = Path.GetDirectoryName(file.ProjectItem.ContainingProject.FullName);
                        var dataLayerFolder = Path.Combine(projectPath, options.DataLayerFolderName);

                        if (!Directory.Exists(dataLayerFolder))
                        {
                            Directory.CreateDirectory(dataLayerFolder);
                            LogMessage($"  Created folder: {options.DataLayerFolderName}");
                        }

                        // Generate data layer class
                        var dataLayerCode = generatorService.GenerateDataLayerClass(className, modelInfo, dataLayerOptions);
                        var dataLayerFilePath = Path.Combine(dataLayerFolder, $"{dataClassName}{Constants.CSharpExtension}");

                        // Check if file exists
                        if (File.Exists(dataLayerFilePath))
                        {
                            var overwrite = ShowOverwriteConfirmation(Path.GetFileName(dataLayerFilePath));
                            if (overwrite != System.Windows.MessageBoxResult.Yes)
                            {
                                LogMessage($"  Skipped existing file: {dataClassName}{Constants.CSharpExtension}");
                                skippedCount++;
                                continue;
                            }
                        }

                        // Save data layer file
                        File.WriteAllText(dataLayerFilePath, dataLayerCode);
                        LogMessage($"  Generated: {dataClassName}{Constants.CSharpExtension}");

                        // Generate interface if requested
                        if (dataLayerOptions.GenerateInterface)
                        {
                            var interfaceCode = generatorService.GenerateInterface(className, modelInfo, dataLayerOptions);
                            var interfaceName = $"I{dataClassName}";

                            string interfaceFolder;
                            if (options.CreateInterfacesFolder)
                            {
                                interfaceFolder = Path.Combine(dataLayerFolder, "Interfaces");
                                if (!Directory.Exists(interfaceFolder))
                                {
                                    Directory.CreateDirectory(interfaceFolder);
                                    LogMessage($"  Created folder: {options.DataLayerFolderName}/Interfaces");
                                }
                            }
                            else
                            {
                                interfaceFolder = dataLayerFolder;
                            }

                            var interfaceFilePath = Path.Combine(interfaceFolder, $"{interfaceName}{Constants.CSharpExtension}");

                            // Check if interface file exists
                            if (File.Exists(interfaceFilePath))
                            {
                                var overwrite = ShowOverwriteConfirmation(Path.GetFileName(interfaceFilePath));
                                if (overwrite != System.Windows.MessageBoxResult.Yes)
                                {
                                    LogMessage($"  Skipped existing interface: {interfaceName}{Constants.CSharpExtension}");
                                }
                                else
                                {
                                    File.WriteAllText(interfaceFilePath, interfaceCode);
                                    LogMessage($"  Generated interface: {interfaceName}{Constants.CSharpExtension}");
                                }
                            }
                            else
                            {
                                File.WriteAllText(interfaceFilePath, interfaceCode);
                                LogMessage($"  Generated interface: {interfaceName}{Constants.CSharpExtension}");
                            }
                        }

                        // Add files to project
                        try
                        {
                            var projectItems = file.ProjectItem.ContainingProject.ProjectItems;
                            var dataFolderItem = projectItems.Cast<ProjectItem>()
                                .FirstOrDefault(pi =>
                                {
                                    ThreadHelper.ThrowIfNotOnUIThread();
                                    return pi.Name == options.DataLayerFolderName;
                                }) ?? projectItems.AddFolder(options.DataLayerFolderName);

                            // Add data layer file
                            var existingDataItem = dataFolderItem?.ProjectItems.Cast<ProjectItem>()
                                .FirstOrDefault(pi =>
                                {
                                    ThreadHelper.ThrowIfNotOnUIThread();
                                    return pi.Name == $"{dataClassName}{Constants.CSharpExtension}";
                                });

                            if (existingDataItem == null)
                            {
                                dataFolderItem?.ProjectItems.AddFromFile(dataLayerFilePath);
                                LogMessage($"  Added to project: {dataClassName}{Constants.CSharpExtension}");
                            }

                            // Add interface file if generated
                            if (dataLayerOptions.GenerateInterface && options.CreateInterfacesFolder)
                            {
                                var interfaceFolderItem = dataFolderItem?.ProjectItems.Cast<ProjectItem>()
                                    .FirstOrDefault(pi =>
                                    {
                                        ThreadHelper.ThrowIfNotOnUIThread();
                                        return pi.Name == "Interfaces";
                                    }) ?? dataFolderItem?.ProjectItems.AddFolder("Interfaces");

                                var interfaceName = $"I{dataClassName}";
                                var interfaceFilePath = Path.Combine(dataLayerFolder, "Interfaces", $"{interfaceName}{Constants.CSharpExtension}");

                                if (File.Exists(interfaceFilePath))
                                {
                                    var existingInterfaceItem = interfaceFolderItem?.ProjectItems.Cast<ProjectItem>()
                                        .FirstOrDefault(pi =>
                                        {
                                            ThreadHelper.ThrowIfNotOnUIThread();
                                            return pi.Name == $"{interfaceName}{Constants.CSharpExtension}";
                                        });

                                    if (existingInterfaceItem == null)
                                    {
                                        interfaceFolderItem?.ProjectItems.AddFromFile(interfaceFilePath);
                                        LogMessage($"  Added to project: {interfaceName}{Constants.CSharpExtension}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"  Warning: Could not add file(s) to project: {ex.Message}");
                        }

                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    LogMessage($"  Error processing {Path.GetFileName(filePath)}: {ex.Message}");
                    ShowMessage($"Error processing {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            var summary = $"Data layer generation complete!\n\n" +
                         $"Succeeded: {successCount}\n" +
                         $"Failed: {failCount}\n" +
                         $"Skipped: {skippedCount}";

            LogMessage("=== Generation Complete ===");
            LogMessage(summary.Replace("\n", " "));

            if (successCount > 0 || failCount > 0)
            {
                ShowMessage(summary);
            }
        }

        private void ShowMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                Constants.ExtensionName,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static System.Windows.MessageBoxResult ShowOverwriteConfirmation(string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return System.Windows.MessageBox.Show(
                $"File '{fileName}' already exists. Do you want to overwrite it?",
                Constants.ExtensionName,
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
        }
    }

    internal static class JoinableTaskExtensions
    {
        public static void FileAndForget(this Microsoft.VisualStudio.Threading.JoinableTask joinableTask, string context)
        {
            _ = joinableTask.Task.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        ActivityLog.LogError(context, $"Unhandled exception: {t.Exception.InnerException?.Message ?? t.Exception.Message}");
                    }
                },
                System.Threading.Tasks.TaskScheduler.Default);
        }
    }
}
