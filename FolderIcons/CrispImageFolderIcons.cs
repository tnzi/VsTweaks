using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Microsoft.VisualStudio.Imaging;

namespace VsTweaks.FolderIcons
{
    /// <summary>
    /// Classic folder icons everywhere Visual Studio displays a moniker with CrispImage, its general-purpose icon
    /// control (Git Changes, Git Repository, most tool windows and dialogs).
    /// </summary>
    /// <remarks>
    /// CrispImage renders its internal InternalMoniker property, which its constructor binds to the public Moniker
    /// property through a converter. Replacing that one binding with the same binding plus the classic swap in front
    /// of the converter covers every way Moniker can be set: a local value, a binding, a style or a template.
    /// CrispImage raises the static Created event at the end of its constructor, after that binding exists.
    /// </remarks>
    internal static class CrispImageFolderIcons
    {
        private static DependencyProperty _internalMonikerProperty;

        public static bool TryStart(out string error)
        {
            _internalMonikerProperty = typeof(CrispImage)
                .GetField("InternalMonikerProperty", BindingFlags.NonPublic | BindingFlags.Static)?
                .GetValue(null) as DependencyProperty;
            if (_internalMonikerProperty == null)
            {
                error = "CrispImage.InternalMonikerProperty was not found.";
                return false;
            }

            CrispImage.Created += (sender, _) => Apply((CrispImage)sender);

            // Images created before the package loaded never raise Created for it.
            foreach (CrispImage image in VisualTrees.LoadedElements().OfType<CrispImage>().ToList())
            {
                Apply(image);
            }

            error = null;
            return true;
        }

        private static void Apply(CrispImage image)
        {
            if (!(BindingOperations.GetBinding(image, _internalMonikerProperty) is Binding original) || original.Converter is ClassicThen)
            {
                return;
            }

            BindingOperations.SetBinding(image, _internalMonikerProperty, new Binding
            {
                Source = image,
                Path = original.Path,
                Converter = new ClassicThen(original.Converter),
            });
        }

        /// <summary>
        /// Swaps a folder moniker for its classic version, then runs CrispImage's own converter, which turns the
        /// public ImageMoniker into the image library's internal moniker type. The swap must come first: it only
        /// recognizes the public type.
        /// </summary>
        private sealed class ClassicThen : IValueConverter
        {
            private readonly IValueConverter _next;

            public ClassicThen(IValueConverter next)
            {
                _next = next;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                object classic = ClassicFolderIcons.ToClassic.Convert(value, targetType, parameter, culture);
                return _next == null ? classic : _next.Convert(classic, targetType, parameter, culture);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
                throw new NotSupportedException();
        }
    }
}
