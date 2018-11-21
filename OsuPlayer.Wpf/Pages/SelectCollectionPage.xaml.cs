﻿using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.ViewModels;
using Milkitic.OsuPlayer.Windows;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Collection = Milkitic.OsuPlayer.Data.Collection;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// AddCollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class SelectCollectionPage : Page
    {
        public SelectCollectionPageViewModel ViewModel { get; set; }

        private readonly MainWindow _mainWindow;
        private readonly BeatmapEntry _entry;

        public SelectCollectionPage(MainWindow mainWindow, BeatmapEntry entry)
        {
            InitializeComponent();
            ViewModel = (SelectCollectionPageViewModel)DataContext;
            _entry = entry;
            ViewModel.Entry = entry;
            _mainWindow = mainWindow;
            RefreshList();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            _mainWindow.FramePop.Navigate(null);
            GC.SuppressFinalize(this);
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new AddCollectionPage(_mainWindow, this));
        }

        public void RefreshList()
        {
            ViewModel.Collections = new ObservableCollection<CollectionViewModel>(
                CollectionViewModel.CopyFrom(DbOperator.GetCollections().OrderByDescending(k => k.CreateTime)));
        }

        public static void AddToCollection(Collection col, BeatmapEntry entry)
        {
            if (string.IsNullOrEmpty(col.ImagePath))
            {
                OsuFile osuFile =
                    new OsuFile(Path.Combine(Domain.OsuSongPath, entry.FolderName, entry.BeatmapFileName));
                if (osuFile.Events.BackgroundInfo != null)
                {
                    var imgPath = Path.Combine(Domain.OsuSongPath, entry.FolderName, osuFile.Events.BackgroundInfo.Filename);
                    if (File.Exists(imgPath))
                    {
                        col.ImagePath = imgPath;
                        DbOperator.UpdateCollection(col);
                    }
                }
            }
            DbOperator.AddMapToCollection(entry, col);
        }
    }
}
