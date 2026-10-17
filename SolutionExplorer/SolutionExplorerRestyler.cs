using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace VsTweaks.SolutionExplorer
{
    /// <summary>
    /// Finds Solution Explorer's tree through its rows and applies the classic layout to it.
    /// </summary>
    /// <remarks>
    /// Rows are virtualized and created continually as the user scrolls and expands nodes. A WPF class handler for
    /// Loaded only fires for elements that already have an instance handler, so it cannot be relied on; SizeChanged
    /// always fires when an element completes its first layout, so its class handler is used to discover new rows.
    /// </remarks>
    internal sealed class SolutionExplorerRestyler
    {
        private static readonly DependencyProperty IsVisitedProperty =
            DependencyProperty.RegisterAttached("IsVisited", typeof(bool), typeof(SolutionExplorerRestyler));

        private readonly ShellTreeInternals _internals;

        public SolutionExplorerRestyler(ShellTreeInternals internals)
        {
            _internals = internals;
        }

        public void Start()
        {
            EventManager.RegisterClassHandler(
                _internals.ItemPresenterType,
                FrameworkElement.SizeChangedEvent,
                new SizeChangedEventHandler((sender, _) => OnItemPresenterSizeChanged((FrameworkElement)sender)));

            // Trees already on screen before the package loaded produce no new rows for the handler above, so sweep
            // them once.
            foreach (FrameworkElement treeView in FindLoadedHierarchyTreeViews())
            {
                ClassicTreeLayout.Apply(treeView, _internals.UseAlternateStyleProperty);
            }
        }

        private void OnItemPresenterSizeChanged(FrameworkElement itemPresenter)
        {
            if ((bool)itemPresenter.GetValue(IsVisitedProperty))
            {
                return;
            }

            itemPresenter.SetValue(IsVisitedProperty, true);

            if (!(VisualTrees.FindAncestor(itemPresenter, _internals.TreeViewType) is FrameworkElement treeView) || !_internals.IsHierarchyTreeView(treeView))
            {
                return;
            }

            ClassicTreeLayout.Apply(treeView, _internals.UseAlternateStyleProperty);
        }

        private IEnumerable<FrameworkElement> FindLoadedHierarchyTreeViews() =>
            VisualTrees.LoadedElements()
                .Where(element => _internals.TreeViewType.IsInstanceOfType(element) && _internals.IsHierarchyTreeView(element))
                .OfType<FrameworkElement>()
                .ToList();
    }
}
