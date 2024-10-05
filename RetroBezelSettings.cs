using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetroBezel
{
    public class RetroBezelSettings : ObservableObject
    {
        private const double DefaultSimilarityEdge = 0.75;

        private string bezelProjectPath = @"{EmulatorDir}\overlay";
        public string BezelProjectPath { get => bezelProjectPath; set => SetValue(ref bezelProjectPath, value); }

        private bool useBezelProject = false;
        public bool UseBezelProject { get => useBezelProject; set => SetValue(ref useBezelProject, value); }

        private double similarityEdge = DefaultSimilarityEdge;
        public double SimilarityEdge { get => similarityEdge; set => SetValue(ref similarityEdge, value); }

        public void ResetSimilarityEdge()
        {
            SimilarityEdge = DefaultSimilarityEdge;
        }

    }

    public class RetroBezelSettingsViewModel : ObservableObject, ISettings
    {
        private readonly RetroBezel plugin;
        private RetroBezelSettings editingClone { get; set; }

        private RetroBezelSettings settings;
        public RetroBezelSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand<object> BrowseForBezelProject
        {
            get => new RelayCommand<object>((a) =>
            {
                var filePath = plugin.PlayniteApi.Dialogs.SelectFolder();
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    Settings.BezelProjectPath = filePath;
                }
            });
        }

        public RelayCommand<object> ResetSimilarityEdge
        {
            get => new RelayCommand<object>((a) =>
            {
                Settings.ResetSimilarityEdge();
            });
        }

        public RetroBezelSettingsViewModel(RetroBezel plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<RetroBezelSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new RetroBezelSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}