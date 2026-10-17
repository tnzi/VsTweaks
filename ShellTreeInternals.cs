using System;
using System.Reflection;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace VsTweaks
{
    /// <summary>
    /// Solution Explorer's tree is an internal Visual Studio control with no public extensibility point.
    /// This class looks up the internal types and dependency properties the feature relies on by name, so that a
    /// future Visual Studio that renames them disables the feature instead of failing to load the package.
    /// </summary>
    internal sealed class ShellTreeInternals
    {
        private const string TreeViewTypeName = "Microsoft.Internal.VisualStudio.PlatformUI.VirtualizingTreeView";
        private const string ItemPresenterTypeName = "Microsoft.Internal.VisualStudio.PlatformUI.TreeItemPresenter";

        // Solution Explorer's tree derives from this type. Other hierarchy windows built on HierarchyNavigatorPane use
        // it too and get the classic style as well; VirtualizingTreeViews of any other type are left alone.
        private const string HierarchyTreeViewTypeName = "Microsoft.VisualStudio.PlatformUI.HierarchyPivotTreeView";

        private ShellTreeInternals(
            Type treeViewType,
            Type itemPresenterType,
            DependencyProperty useAlternateStyleProperty,
            DependencyProperty iconMonikerProperty,
            DependencyProperty expandedIconMonikerProperty)
        {
            TreeViewType = treeViewType;
            ItemPresenterType = itemPresenterType;
            UseAlternateStyleProperty = useAlternateStyleProperty;
            IconMonikerProperty = iconMonikerProperty;
            ExpandedIconMonikerProperty = expandedIconMonikerProperty;
        }

        public Type TreeViewType { get; }

        public Type ItemPresenterType { get; }

        public DependencyProperty UseAlternateStyleProperty { get; }

        public DependencyProperty IconMonikerProperty { get; }

        public DependencyProperty ExpandedIconMonikerProperty { get; }

        public static bool TryResolve(out ShellTreeInternals internals, out string error)
        {
            internals = null;
            Assembly shell = typeof(ToolWindowPane).Assembly;

            Type treeViewType = shell.GetType(TreeViewTypeName);
            Type itemPresenterType = shell.GetType(ItemPresenterTypeName);
            if (treeViewType == null || itemPresenterType == null)
            {
                error = $"{TreeViewTypeName} or {ItemPresenterTypeName} was not found in {shell.FullName}.";
                return false;
            }

            DependencyProperty useAlternateStyle = FindDependencyProperty(treeViewType, "UseAlternateStyleProperty");
            DependencyProperty iconMoniker = FindDependencyProperty(itemPresenterType, "IconMonikerProperty");
            DependencyProperty expandedIconMoniker = FindDependencyProperty(itemPresenterType, "ExpandedIconMonikerProperty");
            if (useAlternateStyle == null || iconMoniker == null || expandedIconMoniker == null)
            {
                error = $"{TreeViewTypeName} or {ItemPresenterTypeName} is missing an expected dependency property.";
                return false;
            }

            internals = new ShellTreeInternals(treeViewType, itemPresenterType, useAlternateStyle, iconMoniker, expandedIconMoniker);
            error = null;
            return true;
        }

        public bool IsHierarchyTreeView(object treeView)
        {
            for (Type type = treeView.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName == HierarchyTreeViewTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        private static DependencyProperty FindDependencyProperty(Type ownerType, string fieldName) =>
            ownerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as DependencyProperty;
    }
}
