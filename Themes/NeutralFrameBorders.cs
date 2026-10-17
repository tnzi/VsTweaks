using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace VsTweaks.Themes
{
    /// <summary>
    /// Visual Studio 2026 outlines the active document or tool window, and its tab, with the theme's accent color
    /// (ShellColors.AccentFillDefault). Primary buttons, check boxes and focus lines use that same color, so a theme
    /// cannot change the outline alone. Inside window frames only, this feature redirects the accent to the theme's
    /// environment border color (ShellInternalColors.EnvironmentBorder), and restores the real accent for the content
    /// each frame hosts. Built-in themes use one color for both keys and look unchanged; VsTweaks Light sets the
    /// environment border to a neutral gray.
    /// </summary>
    /// <remarks>
    /// The frame templates read the accent through DynamicResource, which resolves from the element upwards, so a
    /// resource entry on the frame control replaces it for the frame's chrome (borders, header, tab strip), and a
    /// second entry on the content presenter puts the real accent back for everything below it. Both entries are
    /// brushes bound to the main window's theme colors, so they follow theme switches.
    /// </remarks>
    internal sealed class NeutralFrameBorders
    {
        private const string ViewManagerAssemblyName = "Microsoft.VisualStudio.Shell.ViewManager";
        private const string StylesAssemblyName = "Microsoft.VisualStudio.Shell.Styles";
        private const string FrameControlsNamespace = "Microsoft.VisualStudio.PlatformUI.Shell.Controls.";

        // Frame controls whose templates draw an accent outline when active, with the template part that hosts the
        // frame's content (null when the control hosts no content, like the auto-hide tab strip).
        private static readonly IReadOnlyDictionary<string, string> ContentPartByFrameType = new Dictionary<string, string>
        {
            ["ViewFrame"] = "Presenter",
            ["TabGroupControl"] = "PART_SelectedContentHost",
            ["DocumentGroupControl"] = "PART_SelectedContentHost",
            ["AutoHideChannelControl"] = null,
        };

        private static readonly DependencyProperty IsAppliedProperty =
            DependencyProperty.RegisterAttached("IsApplied", typeof(bool), typeof(NeutralFrameBorders));

        private static readonly DependencyProperty ThemeAccentProperty =
            DependencyProperty.RegisterAttached("ThemeAccent", typeof(Brush), typeof(NeutralFrameBorders));

        private static readonly DependencyProperty ThemeFrameBorderProperty =
            DependencyProperty.RegisterAttached("ThemeFrameBorder", typeof(Brush), typeof(NeutralFrameBorders));

        private readonly object _accentKey;
        private readonly IReadOnlyDictionary<Type, string> _contentPartByFrameType;
        private readonly Brush _frameBorder;
        private readonly Brush _accent;

        private NeutralFrameBorders(object accentKey, object frameBorderKey, FrameworkElement themeSource, IReadOnlyDictionary<Type, string> contentPartByFrameType)
        {
            _accentKey = accentKey;
            _contentPartByFrameType = contentPartByFrameType;

            themeSource.SetResourceReference(ThemeAccentProperty, accentKey);
            themeSource.SetResourceReference(ThemeFrameBorderProperty, frameBorderKey);
            _accent = FollowColor(themeSource, ThemeAccentProperty);
            _frameBorder = FollowColor(themeSource, ThemeFrameBorderProperty);
        }

        public static bool TryStart(out string error)
        {
            Assembly viewManager = FindLoadedAssembly(ViewManagerAssemblyName);
            Assembly styles = FindLoadedAssembly(StylesAssemblyName);
            if (viewManager == null || styles == null)
            {
                error = $"{ViewManagerAssemblyName} or {StylesAssemblyName} is not loaded.";
                return false;
            }

            object accentKey = FindStaticProperty(styles, "Microsoft.VisualStudio.Shell.Styles.ShellColors", "AccentFillDefaultBrushKey");
            object frameBorderKey = FindStaticProperty(styles, "Microsoft.VisualStudio.Shell.Styles.ShellInternalColors", "EnvironmentBorderBrushKey");
            var frameTypes = ContentPartByFrameType
                .Select(frame => (Type: viewManager.GetType(FrameControlsNamespace + frame.Key), ContentPartName: frame.Value))
                .ToList();
            if (accentKey == null || frameBorderKey == null || frameTypes.Any(frame => frame.Type == null))
            {
                error = "A theme color key or window frame control type was not found.";
                return false;
            }

            var contentPartByFrameType = frameTypes.ToDictionary(frame => frame.Type, frame => frame.ContentPartName);

            if (Application.Current?.MainWindow == null)
            {
                error = "The Visual Studio main window is not available.";
                return false;
            }

            new NeutralFrameBorders(accentKey, frameBorderKey, Application.Current.MainWindow, contentPartByFrameType).Start();
            error = null;
            return true;
        }

        private void Start()
        {
            // SizeChanged always fires when a frame completes its first layout; see SolutionExplorerRestyler for
            // why Loaded cannot be used.
            foreach (Type frameType in _contentPartByFrameType.Keys)
            {
                EventManager.RegisterClassHandler(
                    frameType,
                    FrameworkElement.SizeChangedEvent,
                    new SizeChangedEventHandler((sender, _) => Apply((Control)sender)));
            }

            foreach (Control frame in FindLoadedFrames())
            {
                Apply(frame);
            }
        }

        private void Apply(Control frame)
        {
            if ((bool)frame.GetValue(IsAppliedProperty) || !_contentPartByFrameType.TryGetValue(frame.GetType(), out string contentPartName))
            {
                return;
            }

            // Without a content presenter to restore the accent on, the hosted content would lose it too, so such a
            // frame is left untouched.
            FrameworkElement content = null;
            if (contentPartName != null && (content = frame.Template?.FindName(contentPartName, frame) as FrameworkElement) == null)
            {
                return;
            }

            frame.SetValue(IsAppliedProperty, true);
            frame.Resources[_accentKey] = _frameBorder;
            if (content != null)
            {
                content.Resources[_accentKey] = _accent;
            }
        }

        private IEnumerable<Control> FindLoadedFrames() =>
            VisualTrees.LoadedElements()
                .OfType<Control>()
                .Where(element => _contentPartByFrameType.ContainsKey(element.GetType()))
                .ToList();

        private static Brush FollowColor(FrameworkElement source, DependencyProperty brushProperty)
        {
            var brush = new SolidColorBrush();
            BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding
            {
                Source = source,
                Path = new PropertyPath("(0).Color", brushProperty),
            });
            return brush;
        }

        private static Assembly FindLoadedAssembly(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == name);

        private static object FindStaticProperty(Assembly assembly, string typeName, string propertyName) =>
            assembly.GetType(typeName)?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
    }
}
