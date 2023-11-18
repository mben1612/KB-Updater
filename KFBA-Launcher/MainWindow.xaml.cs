using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Text.Json;
using System.Windows.Controls;
using System.Collections.Generic;

namespace KB_Launcher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        readytoUpdate,
        readytoDownload

    }
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public string Gamedatafilelink = "https://www.dropbox.com/s/cychbkb3yf0qvr4/GameData.txt?dl=1";
        private string rootPath;
        private string gamedatafile;
        private LauncherStatus _status;
        private Game actualgame;
        private GameData gamez;
        private GameData gamefiles = new GameData();
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        Prog.Text = "Ready";
                        Prog.Background = Brushes.Green;
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        Prog.Background = Brushes.Red;
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        Prog.Background = null;
                        break;
                    case LauncherStatus.readytoDownload:
                        PlayButton.Content = "Install";
                        Prog.Text = "";
                        Prog.Background =null;
                        break;
                    case LauncherStatus.readytoUpdate:
                        PlayButton.Content = "Update";
                        break;
                    default:
                        break;
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            rootPath = Directory.GetCurrentDirectory();
            gamedatafile = Path.Combine(rootPath, "Gamedata.txt");
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
           CheckForGames();
        }
        private void CheckForGames()
        {
            try
            {
                WebClient webClient = new WebClient();
                string online = webClient.DownloadString(Gamedatafilelink);
                GameData games = JsonSerializer.Deserialize<GameData>(online);
                gamez = games;
                //Console.WriteLine(games.games[0].name);
               

               
                if (File.Exists(gamedatafile))
                {
                    string text = File.ReadAllText(gamedatafile);
                    gamefiles = JsonSerializer.Deserialize<GameData>(text);
                }
                ChangeGame(games.games[0]);

                for (int i = 0; i < games.games.Length; i++)
                {
                    if(GameGrid.Children.Count < 12)
                    {
                        Game game = games.games[i];
                        Button button = new Button();
                        int x = i/GameGrid.ColumnDefinitions.Count;
                        int y = i - x* GameGrid.RowDefinitions.Count;
                        button.Content = game.name;
                        Grid.SetRow(button, x);
                        Grid.SetColumn(button, y);
                        button.Click += ButtonClick;
                        button.DataContext = game;
                        GameGrid.Children.Add(button);
                        if(gamefiles?.games != null)
                        {
                            Version onlineVersion = new Version(game.version);
                            for (int j = 0; j < gamefiles.games.Length; j++)
                            {
                                Game game2 = gamefiles.games[j];
                                if (game2.name == game.name)
                                {
                                    Version localVersion = new Version(game2.version);
                                    if (localVersion.IsDifferentThan(onlineVersion))
                                    {
                                        button.Background = Brushes.Orange;
                                    }
                                    else
                                    {
                                        button.Background= Brushes.Green;
                                    }
                                }
                            }
                        }
                        
                    }

                }
            }
            catch (Exception ex)
            {
                //offline status
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error checking for game updates: {ex}");
            }
        }

        private void ChangeGame(Game game)
        {
            actualgame = game;
            NameOfGame.Text = game.name;
            Version onlineVersion = new Version(game.version);
            VersionText.Text = game.version;
            if(gamefiles?.games != null)
            {
                bool found = false; 
                for (int j = 0; j < gamefiles.games.Length; j++)
                {
                    Game game2 = gamefiles.games[j];
                    if (game2.name == game.name)
                    {
                        found = true;
                        Version localVersion = new Version(game2.version);
                        if (localVersion.IsDifferentThan(onlineVersion))
                        {
                            Status = LauncherStatus.readytoUpdate;
                        }
                        else
                        {
                            Status = LauncherStatus.ready;
                        }
                    }
                }
                if (!found)
                {
                    Status = LauncherStatus.readytoDownload;
                }
            }
            else
            {
                Status = LauncherStatus.readytoDownload;
            }
        }

        private void InstallGameFiles(bool _isUpdate ,Game game)
        {
            try
            {
                Version _onlineVersion;
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;

                }
                _onlineVersion = new Version(game.version);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(showprogress);
                webClient.DownloadFileAsync(new Uri(game.Downloadlink), Path.Combine(rootPath, game.name + ".zip"),game);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }
        private void showprogress(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
            Prog.Text = e.ProgressPercentage.ToString() + "%";
        }
        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                Game game = (Game)e.UserState;
                string onlineVersion = game.version;
                string path = Path.Combine(rootPath, game.name);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }

                ZipFile.ExtractToDirectory(Path.Combine(rootPath, game.name + ".zip"), rootPath);
                File.Delete(Path.Combine(rootPath, game.name + ".zip"));

                List<Game> list = new List<Game>();
                if (gamefiles.games != null)
                {
                    list.AddRange(gamefiles.games);
                    bool found = false;
                    for(int i = 0; i < list.Count; i++)
                    {
                        if(list[i].name == game.name)
                        {
                            found = true;
                            list[i].version = onlineVersion;
                        }
                    }
                    if (!found)
                    {
                        list.Add(game);
                    }
                }
                else
                {
                    list.Add(game);
                }
                   
                
                gamefiles.games = list.ToArray();
                string filetext = JsonSerializer.Serialize(gamefiles);
                File.WriteAllText(gamedatafile, filetext);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if(Status == LauncherStatus.readytoDownload)
            {
                InstallGameFiles(false,actualgame);
            }
            else if(Status == LauncherStatus.readytoUpdate)
            {
                InstallGameFiles(true,actualgame);
            }
            else if(Status == LauncherStatus.ready)
            {
                if (File.Exists(Path.Combine(rootPath,actualgame.name,actualgame.exename +".exe")))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(Path.Combine(rootPath, actualgame.name, actualgame.exename + ".exe"));
                    startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                    Process.Start(startInfo);

                    Close();
                }
                else
                {
                    Status = LauncherStatus.failed;
                }
            }
            else if (Status == LauncherStatus.failed)
            {
                InstallGameFiles(false, actualgame);
            }
        }

        private void LeftClick(object sender, RoutedEventArgs e)
        {

        }

        private void RightClick(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var para = button.DataContext;
            Game game = para as Game;
            ChangeGame(game);
;
    
            
        }

    }

    public struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
    public class GameData
    {
        public Game[] games { get; set; }
 
    }
    public class Game
    {
        public string name { get; set; }
        public string version { get; set; }
        public string Downloadlink { get; set; }
        public string exename { get; set; }
    }
}
