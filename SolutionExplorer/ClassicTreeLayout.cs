using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.VisualStudio.PlatformUI;

namespace VsTweaks.SolutionExplorer
{
    /// <summary>
    /// Visual Studio 2026 turns on the "alternate style" switch (UseAlternateStyle) on Solution Explorer's tree:
    /// 12 px of indentation per level, rows at least 20 to 24 px tall, and chevron expanders. With the switch off,
    /// the tree uses the classic rendering that Visual Studio still ships: 18 px per level, 16 px icons with 2/1 px of
    /// padding above and below, and triangle expanders. That is the Visual Studio 2022 look.
    /// </summary>
    /// <remarks>
    /// Visual Studio writes the switch back to true (as a local value) whenever it re-hosts the tree. A hold-forever
    /// animation pins it to false instead: animated values take precedence over local values, so later writes have
    /// no effect.
    /// </remarks>
    internal static class ClassicTreeLayout
    {
        private static readonly DependencyProperty IsAppliedProperty =
            DependencyProperty.RegisterAttached("IsApplied", typeof(bool), typeof(ClassicTreeLayout));

        private static readonly AnimationTimeline HoldClassicStyle = CreateHold(false);

        public static void Apply(FrameworkElement treeView, DependencyProperty useAlternateStyleProperty)
        {
            if ((bool)treeView.GetValue(IsAppliedProperty))
            {
                return;
            }

            treeView.SetValue(IsAppliedProperty, true);
            treeView.BeginAnimation(useAlternateStyleProperty, HoldClassicStyle);
            MatchRowBackgroundToTree(treeView);
        }

        /// <summary>
        /// Classic rows are filled with TreeViewColors.Background, inset by 1 px on every side. In Visual Studio 2022
        /// themes that color matched the tree's background. Visual Studio 2026 paints the tree with the tool window
        /// background instead, and where the two differ a 2 px line shows between rows. Inside this tree only,
        /// TreeViewColors.Background is redirected to a brush that follows the tree's own background color: it
        /// changes along with the theme, rows stay opaque, and icons are themed against the real background color.
        /// </summary>
        private static void MatchRowBackgroundToTree(FrameworkElement treeView)
        {
            var rowBackground = new SolidColorBrush();
            BindingOperations.SetBinding(rowBackground, SolidColorBrush.ColorProperty, new Binding("Background.Color") { Source = treeView });
            treeView.Resources[TreeViewColors.BackgroundBrushKey] = rowBackground;
        }

        private static AnimationTimeline CreateHold(bool value)
        {
            var hold = new BooleanAnimationUsingKeyFrames { FillBehavior = FillBehavior.HoldEnd };
            hold.KeyFrames.Add(new DiscreteBooleanKeyFrame(value, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            hold.Freeze();
            return hold;
        }
    }
}
