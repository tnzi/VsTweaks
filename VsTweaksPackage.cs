using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using VsTweaks.FolderIcons;
using VsTweaks.GitWindows;
using VsTweaks.SolutionExplorer;
using VsTweaks.Themes;
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

            // Solution Explorer's layout and the folder icons in tree rows both work on Visual Studio's tree control.
            if (ShellTreeInternals.TryResolve(out ShellTreeInternals shellTree, out string shellTreeError))
            {
                new SolutionExplorerRestyler(shellTree).Start();
                new TreeRowFolderIcons(shellTree).Start();
            }
            else
            {
                LogFeatureOff(shellTreeError);
            }

            StartOrLog(CrispImageFolderIcons.TryStart);
            StartOrLog(NeutralFrameBorders.TryStart);
            CompactListRows.Start(JoinableTaskFactory);
        }

        private delegate bool TryStartFeature(out string error);

        private static void StartOrLog(TryStartFeature tryStart)
        {
            if (!tryStart(out string error))
            {
                LogFeatureOff(error);
            }
        }

        // A feature whose Visual Studio internals changed simply stays off; the IDE itself is unaffected.
        private static void LogFeatureOff(string reason) => ActivityLog.LogWarning(nameof(VsTweaks), reason);
    }
}
