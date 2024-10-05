using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace RetroBezel
{
    public class RetroBezel : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private RetroBezelSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("6d1b536f-c1e9-42f5-91d1-4fec74687d3c");

        private static readonly string OverlayFilename = "playnite-custom-overlay.cfg";

        public RetroBezel(IPlayniteAPI api) : base(api)
        {
            settings = new RetroBezelSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }
        private string ExistingFile(string path) => !string.IsNullOrEmpty(path) && File.Exists(path) ? path : default;

        private string FromBezelDirRandom(string gamePath)
        {
            string dir = Path.Combine(gamePath, "bezels");
            if (!Directory.Exists(dir))
                return default;

            Random rand = new Random();
            List<string> files = Directory.GetFiles(dir, "*.png").ToList();

            return files.Count > 0 ? files[rand.Next(files.Count)] : default;
        }

        private string IgnoreArticles(string name)
        {
            string cutted = Regex.Replace(name, @"^((?:The|An|A)(?:[^A-Z-a-z0-9]+))(.*$)", "$2", RegexOptions.IgnoreCase);
            cutted = Regex.Replace(cutted, @"(.*)((?:[^A-Z-a-z0-9]+)(?:The|An|A))$", "$1", RegexOptions.IgnoreCase);
            return cutted;
        }
        private string DeConventGameName(string name)
        {
            int len = name.IndexOfAny(new char[] { '(', '[' });
            string cutted = (len > 0 ? name.Substring(0, len) : name).ToLower().Trim();
            cutted = IgnoreArticles(cutted);
            return Regex.Replace(cutted, @"[^A-Za-z0-9]+", "", RegexOptions.IgnoreCase).ToLower();
        }

        private int LevenshteinDistance(string source, string target)
        {
            // degenerate cases
            if (source == target) return 0;
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            // create two work vectors of integer distances
            int[] v0 = new int[target.Length + 1];
            int[] v1 = new int[target.Length + 1];

            // initialize v0 (the previous row of distances)
            // this row is A[0][i]: edit distance for an empty s
            // the distance is just the number of characters to delete from t
            for (int i = 0; i < v0.Length; i++)
                v0[i] = i;

            for (int i = 0; i < source.Length; i++)
            {
                // calculate v1 (current row distances) from the previous row v0

                // first element of v1 is A[i+1][0]
                //   edit distance is delete (i+1) chars from s to match empty t
                v1[0] = i + 1;

                // use formula to fill in the rest of the row
                for (int j = 0; j < target.Length; j++)
                {
                    var cost = (source[i] == target[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }

                // copy v1 (current row) to v0 (previous row) for next iteration
                for (int j = 0; j < v0.Length; j++)
                    v0[j] = v1[j];
            }

            return v1[target.Length];
        }

        private double Similarity(string a, string b)
        {
            string first = DeConventGameName(a.ToLower());
            string second = DeConventGameName(b.ToLower());

            if ((first == null) || (first == null)) return 0.0;
            if ((first.Length == 0) || (first.Length == 0)) return 0.0;
            if (first == second) return 1.0;

            int stepsToSame = LevenshteinDistance(first, second);
            return 1.0 - ((double)stepsToSame / (double)Math.Max(first.Length, second.Length));
        }

        private string MostSimilar(string gameName, string source, List<string> paths)
        {
            return paths.Select(s => new Tuple<string, double>(s, Math.Max(Similarity(gameName, Path.GetFileNameWithoutExtension(s)), Similarity(source, Path.GetFileNameWithoutExtension(s)))))
                .Where(t => t.Item2 >= settings.Settings.SimilarityEdge)
                .OrderByDescending(d => d.Item2)
                .FirstOrDefault()?.Item1;
        }

        private string DetectBezelProject(Game game, string emulatorDir )
        {
            List<string> paths = new List<string>() { settings.Settings.BezelProjectPath };
            paths.AddMissing(@"{EmulatorDir}\overlay");                               // Default TheBezelProject location
            paths.AddMissing(@"{EmulatorDir}\..\..\decorations\thebezelproject");     // RetroBat decorations location

            return paths.Select( p => PlayniteApi.ExpandGameVariables(game, p, emulatorDir) )
                        .FirstOrDefault(p => Directory.Exists(p));
        }

        private string GetDirectoryName(string path)
        {
            return Path.GetFileName(path);
        }
        private string FromBezelProject(Game game, string emulatorDir, string romFile)
        {
            string bezelProjectPath = DetectBezelProject(game, emulatorDir);

            List<string> directories = Directory.GetFiles(bezelProjectPath, "*.png", SearchOption.AllDirectories)
                .Select(f => Path.GetDirectoryName(f))
                .Distinct()
                .Where(d=> romFile.ToLower().Contains($"\\{GetDirectoryName(d).ToLower()}\\"))
                .ToList();

            if (directories.Count == 0)
            {
                directories.Add(bezelProjectPath);
            }

            List<string> files = new List<string>();
            foreach( string d in directories )
            {
                files.AddMissing(Directory.GetFiles(d, "*.png", SearchOption.AllDirectories).ToList());
            }

            string romName = Path.GetFileNameWithoutExtension(romFile);
            string bezel = files.FirstOrDefault(f => 0 == string.Compare(Path.GetFileNameWithoutExtension(f), romName, StringComparison.OrdinalIgnoreCase))
                        ?? files.FirstOrDefault(f => 0 == string.Compare(Path.GetFileNameWithoutExtension(f), DeConventGameName(game.Name), StringComparison.OrdinalIgnoreCase))
                        ?? MostSimilar(game.Name, romFile, files);

            return bezel;
        }

        private string FromSystem(Game game, string romFile)
        {
            string platformsPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "ExtraMetadata", "Platform");

            var platform = game.Platforms.Select(p => Path.Combine(platformsPath, p.Name)).ToList();
            List<string> bezels = platform
                .Select(p => Path.Combine(p, "Bezel.png"))
                .Where(p => File.Exists(p))
                .ToList();

            foreach( var path in
                 platform
                    .Select(p=>Path.Combine(p, "Bezels"))
                    .Where(p=>Directory.Exists(p)) )
            {
                bezels.AddMissing(Directory.GetFiles(path, "*.png"));
            }

            if (bezels.Count > 0)
            {
                return bezels[new Random().Next(bezels.Count)];
            }

            return default;
        }

        private void UpdateConfig(string retroarchCfg, string overlayPath, bool enable)
        {
            Dictionary<string, string> options = new Dictionary<string, string>()
             {
                {"input_overlay",             overlayPath               },
                {"input_overlay_enable",      enable ? "true" : "false" },
                {"input_overlay_behind_menu", "false"                   },
                {"input_overlay_center_x",    "0.500000"                },
                {"input_overlay_center_y",    "0.500000"                },
            };

            List<string> keys = options.Keys.ToList();
            List<string> lines = new List<string>();

            using (StreamReader configFile = new StreamReader(retroarchCfg))
            {
                string line;
                do
                {
                    line = configFile.ReadLine();
                    foreach (string k in keys)
                    {
                        if (Regex.IsMatch(line, $"\\s*{k}\\s*=\\s*.*"))
                        {
                            line = $"{k} = \"{options[k]}\"";
                            keys.Remove(k);
                            break;
                        }
                    }
                    lines.Add(line);
                }
                while (line != null);

                foreach (string k in keys)
                {
                    lines.Add($"{k} = \"{options[k]}\"");
                }
            }
            using (StreamWriter configFile = new StreamWriter(retroarchCfg))
            {
                foreach (string line in lines)
                {
                    configFile.WriteLine(line);
                }
            }
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
            Game game = args.Game;
            GameAction action = args.SourceAction;

            Guid emulatorId = game.GameActions.FirstOrDefault(a => a.IsPlayAction && a.Type == GameActionType.Emulator)?.EmulatorId ?? default;
            Emulator emulator = PlayniteApi.Database.Emulators.Get(emulatorId);

            if (0 != string.Compare(emulator?.BuiltInConfigId ?? "", "retroarch", StringComparison.OrdinalIgnoreCase))
                return;

            string romFile = PlayniteApi.ExpandGameVariables(game, args.SelectedRomFile, emulator.InstallDir ?? "").Replace("\\\\", "\\");

            string retroarchCfg = Path.Combine(emulator.InstallDir, "retroarch.cfg");

            if (!File.Exists(retroarchCfg))
                return;


            string gamePath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "ExtraMetadata", "Games", game.Id.ToString());
            string customOverlayCfg = Path.Combine(gamePath, OverlayFilename);

            string gameBezel = ExistingFile(Path.Combine(gamePath, "bezel.png"))
                            ?? FromBezelDirRandom(gamePath)
                            ?? FromBezelProject(game, emulator.InstallDir, romFile)
                            ?? FromSystem(game, romFile);

            UpdateConfig(retroarchCfg, customOverlayCfg, gameBezel != null);
            if (gameBezel == null)
            {
                return;
            }

            using (StreamWriter overlayFile = new StreamWriter(customOverlayCfg))
            {
                overlayFile.WriteLine("overlays = 1");
                overlayFile.WriteLine($"overlay0_overlay = \"{gameBezel}\"");
                overlayFile.WriteLine("overlay0_full_screen = true");
                overlayFile.WriteLine("overlay0_descs = 0");
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new RetroBezelSettingsView();
        }
    }
}