using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace VsTweaks.FolderIcons
{
    /// <summary>
    /// Classic folder icons in Visual Studio's own tree control (Solution Explorer and every other window built on
    /// VirtualizingTreeView). Its rows draw icons themselves from two moniker properties instead of using CrispImage,
    /// so each row's bindings for those properties are rebound through the classic converters.
    /// </summary>
    /// <remarks>
    /// Rows are virtualized and created continually. SizeChanged always fires when a row completes its first layout,
    /// unlike Loaded, whose class handler only fires for elements that already have an instance handler.
    /// </remarks>
    internal sealed class TreeRowFolderIcons
    {
        private static readonly DependencyProperty IsAppliedProperty =
            DependencyProperty.RegisterAttached("IsApplied", typeof(bool), typeof(TreeRowFolderIcons));

        private readonly ShellTreeInternals _internals;

        public TreeRowFolderIcons(ShellTreeInternals internals)
        {
            _internals = internals;
        }

        public void Start()
        {
            EventManager.RegisterClassHandler(
                _internals.ItemPresenterType,
                FrameworkElement.SizeChangedEvent,
                new SizeChangedEventHandler((sender, _) => Apply((DependencyObject)sender)));

            // Rows already on screen before the package loaded may not lay out again for a long time.
            foreach (DependencyObject itemPresenter in VisualTrees.LoadedElements().Where(_internals.ItemPresenterType.IsInstanceOfType).ToList())
            {
                Apply(itemPresenter);
            }
        }

        private void Apply(DependencyObject itemPresenter)
        {
            if ((bool)itemPresenter.GetValue(IsAppliedProperty))
            {
                return;
            }

            itemPresenter.SetValue(IsAppliedProperty, true);
            Rebind(itemPresenter, _internals.IconMonikerProperty, ClassicFolderIcons.ToClassic);
            Rebind(itemPresenter, _internals.ExpandedIconMonikerProperty, ClassicFolderIcons.ToClassicExpanded);
        }

        /// <summary>
        /// Copies the row template's existing binding (path and source) and only adds a converter. A binding that
        /// does not have the expected shape is left as it is.
        /// </summary>
        private static void Rebind(DependencyObject itemPresenter, DependencyProperty iconProperty, IValueConverter converter)
        {
            if (!(BindingOperations.GetBinding(itemPresenter, iconProperty) is Binding original) || original.Converter != null)
            {
                return;
            }

            var classic = new Binding { Path = original.Path, Mode = original.Mode, Converter = converter };
            if (original.RelativeSource != null)
            {
                classic.RelativeSource = original.RelativeSource;
            }
            else if (original.ElementName != null)
            {
                classic.ElementName = original.ElementName;
            }
            else if (original.Source != null)
            {
                classic.Source = original.Source;
            }

            BindingOperations.SetBinding(itemPresenter, iconProperty, classic);
        }
    }
}
