using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace VsTweaks.GitWindows
{
    /// <summary>
    /// Lists in the Git windows (Git Changes, Git Repository and the rest of Team Explorer) always use compact rows:
    /// 20 px, the row height of Visual Studio 2022, instead of 26 px when compact spacing is off.
    /// </summary>
    /// <remarks>
    /// These windows are built on Microsoft.TeamFoundation.Controls, whose list styles bind the row height and the
    /// size of the hover buttons in each row to TeamFoundationControlSizes.Instance. Its UseCompactSpacing switch
    /// selects 24 px rows with 24 px buttons (26 px with the row border) or 20 px rows with 18 px buttons. The Git
    /// package writes the "compact spacing" setting (environment.visualExperience.useCompactSpacing) to that switch
    /// at startup and whenever the setting changes; this feature turns the switch back on each time it changes.
    /// The assembly loads with the Git package, usually after this package, so it is picked up when it loads.
    /// </remarks>
    internal static class CompactListRows
    {
        private const string ControlsAssemblyName = "Microsoft.TeamFoundation.Controls";
        private const string SizesTypeName = "Microsoft.TeamFoundation.Controls.WPF.TeamFoundationControlSizes";

        public static void Start(JoinableTaskFactory joinableTaskFactory)
        {
            AppDomain.CurrentDomain.AssemblyLoad += (_, e) =>
            {
                if (IsControlsAssembly(e.LoadedAssembly))
                {
                    ApplyOnMainThread(joinableTaskFactory, e.LoadedAssembly);
                }
            };

            Assembly loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(IsControlsAssembly);
            if (loaded != null)
            {
                ApplyOnMainThread(joinableTaskFactory, loaded);
            }
        }

        private static bool IsControlsAssembly(Assembly assembly) => assembly.GetName().Name == ControlsAssemblyName;

        // AssemblyLoad is raised on whichever thread loads the assembly; the list bindings and the setting watcher
        // live on the UI thread.
        private static void ApplyOnMainThread(JoinableTaskFactory joinableTaskFactory, Assembly controls) =>
            joinableTaskFactory.RunAsync(async () =>
            {
                await joinableTaskFactory.SwitchToMainThreadAsync();
                Apply(controls);
            }).FileAndForget("VsTweaks/GitWindows/CompactListRows");

        private static void Apply(Assembly controls)
        {
            Type sizesType = controls.GetType(SizesTypeName);
            object sizes = sizesType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            PropertyInfo useCompactSpacing = sizesType?.GetProperty("UseCompactSpacing", BindingFlags.Public | BindingFlags.Instance);
            if (!(sizes is INotifyPropertyChanged notifier) || useCompactSpacing?.PropertyType != typeof(bool) || !useCompactSpacing.CanWrite)
            {
                ActivityLog.LogWarning(nameof(VsTweaks), $"{SizesTypeName}.Instance.UseCompactSpacing was not found in {controls.FullName}.");
                return;
            }

            // Runs inside the Git package's own change notification, so a failure is logged rather than thrown there.
            void KeepCompact()
            {
                try
                {
                    if (!(bool)useCompactSpacing.GetValue(sizes))
                    {
                        useCompactSpacing.SetValue(sizes, true);
                    }
                }
                catch (TargetInvocationException e)
                {
                    ActivityLog.LogWarning(nameof(VsTweaks), $"Turning on compact Git list rows failed: {e.InnerException}");
                }
            }

            notifier.PropertyChanged += (_, __) => KeepCompact();
            KeepCompact();
        }
    }
}
