using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using VsTweaks.SolutionExplorer;
using Task = System.Threading.Tasks.Task;

namespace VsTweaks
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    // The image manifest refers to icons in this assembly through /VsTweaks;component/... URIs. Visual Studio builds
    // its image cache at startup, before this package loads, so it must be able to resolve this assembly by name;
    // otherwise every image in the manifest fails to load.
    [ProvideBindingPath]
    public sealed class VsTweaksPackage : AsyncPackage
    {
        public const string PackageGuidString = "38927d76-3bc6-43ca-94a4-cc12b3a112ee";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            StartClassicSolutionExplorer();
        }

        private static void StartClassicSolutionExplorer()
        {
            if (!ShellTreeInternals.TryResolve(out ShellTreeInternals internals, out string error))
            {
                // If Visual Studio changes its internals, the feature simply stays off; the IDE itself is unaffected.
                ActivityLog.LogWarning(nameof(VsTweaks), error);
                return;
            }

            new SolutionExplorerRestyler(internals).Start();
        }
    }
}
