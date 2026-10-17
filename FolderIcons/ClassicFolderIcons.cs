using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace VsTweaks.FolderIcons
{
    /// <summary>
    /// Visual Studio 2026 folder icons are outlines only. ClassicFolderIcons.imagemanifest provides the
    /// Visual Studio 2022 versions (outline plus a half-opacity fill) under this extension's own GUID, with the same
    /// IDs as the catalog images they replace. The converters here swap a catalog folder moniker for the classic one;
    /// TreeRowFolderIcons and CrispImageFolderIcons put them between Visual Studio's icons and the screen.
    /// </summary>
    /// <remarks>
    /// The image library keeps the first definition of a moniker and drops later ones, and the documentation says a
    /// custom manifest must not reuse the image catalog's GUID, so the catalog images cannot be replaced at the
    /// source. The swap happens where icons are displayed instead.
    /// </remarks>
    internal static class ClassicFolderIcons
    {
        // Must match the ClassicFolderIcons GUID in ClassicFolderIcons.imagemanifest.
        private static readonly Guid ManifestGuid = new Guid("04a16d39-f69f-48e1-8826-4e0694597a5a");

        // Catalog images that show a folder as an item: every image whose name contains "Folder" and whose artwork
        // changed in Visual Studio 2026, minus the command icons (New Folder, Add Folder, Open Folder and so on),
        // which would otherwise mix two icon styles in menus and toolbars.
        private static readonly HashSet<int> FolderIds = new HashSet<int>
        {
            KnownImageIds.DocumentsFolder,
            KnownImageIds.DownloadFolder,
            KnownImageIds.FilterFolderClosed,
            KnownImageIds.FilterFolderOpened,
            KnownImageIds.FolderClosed,
            KnownImageIds.FolderCodeAnalysis,
            KnownImageIds.FolderError,
            KnownImageIds.FolderInformation,
            KnownImageIds.FolderOffline,
            KnownImageIds.FolderOpened,
            KnownImageIds.FolderOpenedNoColor,
            KnownImageIds.FolderOpenedWarning,
            KnownImageIds.FolderSuppressed,
            KnownImageIds.FolderWarning,
            KnownImageIds.HiddenFolderClosed,
            KnownImageIds.HiddenFolderOpened,
            KnownImageIds.LinkedFolderClosed,
            KnownImageIds.LinkedFolderOpened,
            KnownImageIds.MemberFolderClosed,
            KnownImageIds.MemberFolderOpened,
            KnownImageIds.MissingFolderClosed,
            KnownImageIds.MissingFolderOpened,
            KnownImageIds.PackageFolderClosed,
            KnownImageIds.PackageFolderOpened,
            KnownImageIds.PropertiesFolderClosed,
            KnownImageIds.PropertiesFolderOpen,
            KnownImageIds.ReferenceFolderClosed,
            KnownImageIds.ReferenceFolderOpened,
            KnownImageIds.RemoteFolder,
            KnownImageIds.RemoteFolderOpen,
            KnownImageIds.SamplesFolder,
            KnownImageIds.SearchFolderClosed,
            KnownImageIds.SearchFolderOpened,
            KnownImageIds.SpecialFolderClosed,
            KnownImageIds.SpecialFolderOpened,
            KnownImageIds.TeamProjectRepositoryFolder,
            KnownImageIds.WebFolderOpened,
            KnownImageIds.WebReferenceFolder,
        };

        // Visual Studio 2022 shows an open folder for an expanded tree node; Visual Studio 2026 keeps the closed icon.
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

        /// <summary>Swaps a catalog folder moniker for its classic version; any other value passes through.</summary>
        public static readonly IValueConverter ToClassic = new ClassicMonikerConverter(id => id);

        /// <summary>Like <see cref="ToClassic"/>, but turns closed folders into open ones first, for expanded tree nodes.</summary>
        public static readonly IValueConverter ToClassicExpanded =
            new ClassicMonikerConverter(id => OpenedCounterparts.TryGetValue(id, out int opened) ? opened : id);

        /// <summary>
        /// Maps the catalog ID with mapId first, then swaps the result for the classic image in this extension's manifest.
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
                return FolderIds.Contains(id) ? new ImageMoniker { Guid = ManifestGuid, Id = id } : value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
                throw new NotSupportedException();
        }
    }
}
