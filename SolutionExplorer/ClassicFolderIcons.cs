using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace VsTweaks.SolutionExplorer
{
    /// <summary>
    /// Visual Studio 2026 folder icons are outlines only. ClassicFolderIcons.imagemanifest provides the
    /// Visual Studio 2022 versions (outline plus a half-opacity fill) under this extension's own GUID, with the same
    /// IDs as the catalog images they replace. Each row's icon bindings are rebound through a converter that swaps
    /// those folder monikers for the classic ones.
    /// </summary>
    /// <remarks>
    /// The documentation says a custom manifest must not override the image catalog's own GUID, so the classic icons
    /// live under a separate GUID and are swapped in Solution Explorer rows only.
    /// </remarks>
    internal static class ClassicFolderIcons
    {
        // Must match the ClassicFolderIcons GUID in ClassicFolderIcons.imagemanifest.
        private static readonly Guid ManifestGuid = new Guid("04a16d39-f69f-48e1-8826-4e0694597a5a");

        // Every catalog image whose name contains "Folder" and whose artwork changed in Visual Studio 2026. Many are
        // command icons that never appear in the tree; they are included so the rule stays simple and checkable.
        private static readonly HashSet<int> ReplacedIds = new HashSet<int>
        {
            KnownImageIds.AddFolder,
            KnownImageIds.CompareFolders,
            KnownImageIds.ConvertBranchToFolder,
            KnownImageIds.ConvertFolderToBranch,
            KnownImageIds.DeleteFolder,
            KnownImageIds.DocumentsFolder,
            KnownImageIds.DownloadFolder,
            KnownImageIds.FilterFolderClosed,
            KnownImageIds.FilterFolderOpened,
            KnownImageIds.FolderBottomPanel,
            KnownImageIds.FolderBrowserDialogControl,
            KnownImageIds.FolderClosed,
            KnownImageIds.FolderCodeAnalysis,
            KnownImageIds.FolderError,
            KnownImageIds.FolderInformation,
            KnownImageIds.FolderOffline,
            KnownImageIds.FolderOpened,
            KnownImageIds.FolderOpenedNoColor,
            KnownImageIds.FolderOpenedWarning,
            KnownImageIds.FolderSuppressed,
            KnownImageIds.FolderTopPanel,
            KnownImageIds.FolderWarning,
            KnownImageIds.HiddenFolderClosed,
            KnownImageIds.HiddenFolderOpened,
            KnownImageIds.LinkedFolderClosed,
            KnownImageIds.LinkedFolderOpened,
            KnownImageIds.MapToFolder,
            KnownImageIds.MemberFolderClosed,
            KnownImageIds.MemberFolderOpened,
            KnownImageIds.MissingFolderClosed,
            KnownImageIds.MissingFolderOpened,
            KnownImageIds.MoveToFolder,
            KnownImageIds.NewFolder,
            KnownImageIds.NextBookmarkInFolder,
            KnownImageIds.OpenFolder,
            KnownImageIds.OpenProjectFolder,
            KnownImageIds.PackageFolderClosed,
            KnownImageIds.PackageFolderOpened,
            KnownImageIds.PreviousBookmarkInFolder,
            KnownImageIds.PropertiesFolderClosed,
            KnownImageIds.PropertiesFolderOpen,
            KnownImageIds.ReferenceFolderClosed,
            KnownImageIds.ReferenceFolderOpened,
            KnownImageIds.RemoteFolder,
            KnownImageIds.RemoteFolderOpen,
            KnownImageIds.ReplaceInFolder,
            KnownImageIds.SamplesFolder,
            KnownImageIds.SearchFolderClosed,
            KnownImageIds.SearchFolderOpened,
            KnownImageIds.ShowOrHideFolder,
            KnownImageIds.SolutionFolderSwitch,
            KnownImageIds.SpecialFolderClosed,
            KnownImageIds.SpecialFolderOpened,
            KnownImageIds.SwitchFolders,
            KnownImageIds.TeamProjectRepositoryFolder,
            KnownImageIds.WebFolderOpened,
            KnownImageIds.WebReferenceFolder,
        };

        // Visual Studio 2022 shows an open folder for an expanded node; Visual Studio 2026 keeps the closed icon.
        private static readonly Dictionary<int, int> OpenedCounterparts = new Dictionary<int, int>
        {
            [KnownImageIds.FolderClosed] = KnownImageIds.FolderOpened,
            [KnownImageIds.FilterFolderClosed] = KnownImageIds.FilterFolderOpened,
            [KnownImageIds.HiddenFolderClosed] = KnownImageIds.HiddenFolderOpened,
            [KnownImageIds.LinkedFolderClosed] = KnownImageIds.LinkedFolderOpened,
            [KnownImageIds.MemberFolderClosed] = KnownImageIds.MemberFolderOpened,
            [KnownImageIds.MissingFolderClosed] = KnownImageIds.MissingFolderOpened,
            [KnownImageIds.PackageFolderClosed] = KnownImageIds.PackageFolderOpened,
            [KnownImageIds.PropertiesFolderClosed] = KnownImageIds.PropertiesFolderOpen,
            [KnownImageIds.ReferenceFolderClosed] = KnownImageIds.ReferenceFolderOpened,
            [KnownImageIds.SearchFolderClosed] = KnownImageIds.SearchFolderOpened,
            [KnownImageIds.SpecialFolderClosed] = KnownImageIds.SpecialFolderOpened,
            [KnownImageIds.WebFolderClosed] = KnownImageIds.WebFolderOpened,
        };

        private static readonly IValueConverter ToClassicIcon = new ClassicMonikerConverter(id => id);

        private static readonly IValueConverter ToClassicExpandedIcon =
            new ClassicMonikerConverter(id => OpenedCounterparts.TryGetValue(id, out int opened) ? opened : id);

        public static void Apply(DependencyObject itemPresenter, DependencyProperty iconProperty, DependencyProperty expandedIconProperty)
        {
            Rebind(itemPresenter, iconProperty, ToClassicIcon);
            Rebind(itemPresenter, expandedIconProperty, ToClassicExpandedIcon);
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

        /// <summary>
        /// Maps the catalog ID with mapId first (the expanded icon uses it to turn closed folders into open ones),
        /// then swaps the result for the classic image in this extension's manifest.
        /// </summary>
        private sealed class ClassicMonikerConverter : IValueConverter
        {
            private readonly Func<int, int> _mapId;

            public ClassicMonikerConverter(Func<int, int> mapId)
            {
                _mapId = mapId;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is ImageMoniker moniker) || moniker.Guid != KnownImageIds.ImageCatalogGuid)
                {
                    return value;
                }

                int id = _mapId(moniker.Id);
                return ReplacedIds.Contains(id) ? new ImageMoniker { Guid = ManifestGuid, Id = id } : value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
                throw new NotSupportedException();
        }
    }
}
