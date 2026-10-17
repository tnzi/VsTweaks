using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VsTweaks.SolutionExplorer
{
    /// <summary>
    /// Finds Solution Explorer's tree and each of its rows, and applies the classic layout and classic folder icons.
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
            // them once. Switching a tree to the classic layout rebuilds all of its rows, and the class handler then
            // takes over the icons of the new rows.
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

            if (!(FindAncestor(itemPresenter, _internals.TreeViewType) is FrameworkElement treeView) || !_internals.IsHierarchyTreeView(treeView))
            {
                return;
            }

            ClassicTreeLayout.Apply(treeView, _internals.UseAlternateStyleProperty);
            ClassicFolderIcons.Apply(itemPresenter, _internals.IconMonikerProperty, _internals.ExpandedIconMonikerProperty);
        }

        private IEnumerable<FrameworkElement> FindLoadedHierarchyTreeViews() =>
            PresentationSource.CurrentSources
                .OfType<PresentationSource>()
                .Select(source => source.RootVisual)
                .Where(root => root != null)
                .SelectMany(Descendants)
                .Where(element => _internals.TreeViewType.IsInstanceOfType(element) && _internals.IsHierarchyTreeView(element))
                .OfType<FrameworkElement>()
                .ToList();

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            var pending = new Stack<DependencyObject>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                DependencyObject current = pending.Pop();
                yield return current;

                int childCount = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < childCount; i++)
                {
                    pending.Push(VisualTreeHelper.GetChild(current, i));
                }
            }
        }

        private static DependencyObject FindAncestor(DependencyObject element, System.Type ancestorType)
        {
            for (DependencyObject current = VisualTreeHelper.GetParent(element); current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (ancestorType.IsInstanceOfType(current))
                {
                    return current;
                }
            }

            return null;
        }
    }
}
