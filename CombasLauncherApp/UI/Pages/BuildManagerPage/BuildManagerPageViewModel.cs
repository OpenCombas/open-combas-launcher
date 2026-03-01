using CombasLauncherApp.Models;
using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CombasLauncherApp.Services.Interfaces;
using Application = System.Windows.Application;

namespace CombasLauncherApp.UI.Pages.BuildManagerPage
{
    public class BuildCollection(string name, string path, ObservableCollection<BuildEntry> builds)
    {
        public string Name { get; set; } = name;

        public string Path { get; set; } = path;

        public ObservableCollection<BuildEntry> Builds { get; } = builds;
    }

    public partial class BuildManagerPageViewModel : ObservableObject
    {
        private readonly IMessageBoxService _messageBoxService = ServiceProvider.GetService<IMessageBoxService>();


        [ObservableProperty] 
        private ObservableCollection<BuildCollection> _buildCollections = [];

        [ObservableProperty]
        private BuildEntry? _selectedCollectionBuild;


        [ObservableProperty]
        private ObservableCollection<BuildEntry> _currentLoadedBuilds = [];

        [ObservableProperty]
        private BuildEntry? _selectedLoadedBuild;

        public BuildManagerPageViewModel()
        {
            LoadBuildCollections();
            LoadActiveBuilds();
        }

        [RelayCommand]
        private void CreateCollection(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return;
            }

            // Create new collection folder in the build collections dir then reload builds

            var buildCollectionDirPath = AppService.HoundBuildCollectionDir;

            if (!Directory.Exists(buildCollectionDirPath))
            {
                Directory.CreateDirectory(buildCollectionDirPath);
            }

            Directory.CreateDirectory(Path.Combine(buildCollectionDirPath, collectionName));

            LoadBuildCollections();
        }

        [RelayCommand]
        private void DeleteCollection(BuildCollection buildCollection)
        {
            var message = Application.Current.TryFindResource("LOC_Delete_Build_Collection_Warning") as string ?? "Missing Resource";
            var caption = Application.Current.TryFindResource("LOC_Warning") as string ?? "Missing Resource";
            var result = _messageBoxService.Show(message, caption, MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Move all builds in the collection to the archive folder

            var houndBuildService = ServiceProvider.GetService<IHoundBuildService>();
            var builds = houndBuildService.GetBuildEntries(buildCollection.Path);
            
            foreach (var build in builds)
            {
                ArchiveBuild(build);
            }

            // Delete the collection folder then reload builds

            Directory.Delete(buildCollection.Path, true);

            LoadBuildCollections();
        }


        private void LoadBuildCollections()
        {
            BuildCollections.Clear();
            var buildCollectionDirPath = AppService.HoundBuildCollectionDir;

            if (!Directory.Exists(buildCollectionDirPath))
            {
                Directory.CreateDirectory(buildCollectionDirPath);
            }

            //Get the build collections from the directory.

            var buildCollectionPaths = Directory.GetDirectories(buildCollectionDirPath);

            foreach (var buildCollectionPath in buildCollectionPaths)
            {
                var collectionName = Path.GetFileName(buildCollectionPath);
                var houndBuildService = ServiceProvider.GetService<IHoundBuildService>();
                var entries = houndBuildService.GetBuildEntries(buildCollectionPath);

                BuildCollections.Add(new BuildCollection(collectionName, buildCollectionPath, entries));
            }
        }

        private void LoadActiveBuilds()
        {
            CurrentLoadedBuilds.Clear();
            var houndBuildService = ServiceProvider.GetService<IHoundBuildService>();
            var entries = houndBuildService.GetBuildEntries(AppService.HoundCurrentBuildsDir);

            foreach (var build in entries)
            {
                CurrentLoadedBuilds.Add(build);
            }
        }

        private void ArchiveBuild(BuildEntry build)
        {
            var buildArchiveDirPath = AppService.HoundArchivedBuildsDir;
            if (!Directory.Exists(buildArchiveDirPath))
            {
                Directory.CreateDirectory(buildArchiveDirPath);
            }

            var buildFileName = Path.GetFileName(build.DirectoryPath);

            var destPath = Path.Combine(buildArchiveDirPath, buildFileName);
            
            Directory.Move(build.DirectoryPath, destPath);

        }
    }
}
