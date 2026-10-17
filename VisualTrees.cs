using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VsTweaks
{
    internal static class VisualTrees
    {
        /// <summary>
        /// Every element currently in a visual tree of this thread's windows, for features that need to catch up
        /// on UI that was created before the package loaded.
        /// </summary>
        public static IEnumerable<DependencyObject> LoadedElements() =>
            PresentationSource.CurrentSources
                .OfType<PresentationSource>()
                .Select(source => source.RootVisual)
                .Where(root => root != null)
                .SelectMany(Descendants);

        public static IEnumerable<DependencyObject> Descendants(DependencyObject root)
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

        public static DependencyObject FindAncestor(DependencyObject element, System.Type ancestorType)
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
