﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace MSBuildWorkspaceTester.Services
{
    internal class WorkspaceService : BaseService
    {
        public MSBuildWorkspace Workspace { get; }

        public WorkspaceService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            /*
            var properties = new Dictionary<string, string>
            {
                // This property ensures that XAML files will be compiled in the current AppDomain
                // rather than a separate one. Any tasks isolated in AppDomains or tasks that create
                // AppDomains will likely not work due to https://github.com/Microsoft/MSBuildLocator/issues/16.
                //{ "AlwaysCompileMarkupFilesInSeparateDomain", bool.FalseString },
                { "DesignTimeBuild", bool.TrueString },
                //{ "BuildProjectReferences", bool.TrueString },
                //{ "SkipCompilerExecution", bool.FalseString },
                //{ "ProvideCommandLineArgs ", bool.TrueString }
            };
            */
            
            // Set global properties
            var globalProperties = new Dictionary<string, string>
            {
                //{ MSBuildProperties.SolutionDir, solutionDir },
                //{ MSBuildProperties.MSBuildExtensionsPath, _dotnetSdkPaths.ExtensionsPath },
                //{ MSBuildProperties.MSBuildSDKsPath, _dotnetSdkPaths.SdksPath },
                //{ MSBuildProperties.RoslynTargetsPath, _dotnetSdkPaths.RoslynTargetsPath },
                { MSBuildProperties.DesignTimeBuild, bool.TrueString },
                //{ MSBuildProperties.BuildProjectReferences, "false" },
                //{ MSBuildProperties.SkipCompilerExecution, "true" },
                //{ MSBuildProperties.ProvideCommandLineArgs, "true" },
                // Workaround for a problem with resource files, see https://github.com/dotnet/sdk/issues/346#issuecomment-257654120
                { MSBuildProperties.GenerateResourceMSBuildArchitecture, "CurrentArchitecture" }
            };

            Workspace = MSBuildWorkspace.Create(globalProperties);
            Workspace.WorkspaceFailed += WorkspaceFailed;

            //Workspace.LoadMetadataForReferencedProjects = true;
        }

        private void WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                Logger.LogError(e.Diagnostic.Message);
            }
            else
            {
                Logger.LogWarning(e.Diagnostic.Message);
            }
        }

        public async Task OpenSolutionAsync(string solutionFilePath)
        {
            LogHeader();

            var watch = Stopwatch.StartNew();
            var solution = await Workspace.OpenSolutionAsync(solutionFilePath, new LoaderProgress(Logger));

            watch.Stop();
            Logger.LogInformation($"\r\nSolution opened: {watch.Elapsed:m\\:ss\\.fffffff}");
        }

        public async Task OpenProjectAsync(string projectFilePath)
        {
            LogHeader();

            var watch = Stopwatch.StartNew();
            var project = await Workspace.OpenProjectAsync(projectFilePath, new LoaderProgress(Logger));

            watch.Stop();
            Logger.LogInformation($"\r\nProject opened: {watch.Elapsed:m\\:ss\\.fffffff}");
        }

        private void LogHeader()
        {
            Logger.LogInformation("Operation       Elapsed Time    Project");
        }

        private class LoaderProgress : IProgress<ProjectLoadProgress>
        {
            private readonly ILogger _logger;

            public LoaderProgress(ILogger logger)
            {
                _logger = logger;
            }

            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);

                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                _logger.LogInformation($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}
