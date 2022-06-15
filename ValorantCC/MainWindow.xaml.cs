using EZ_Updater;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ValorantCC.src;
using ValorantCC.SubWindow;

namespace ValorantCC
{
    public partial class MainWindow : MetroWindow
    {
        public Processor DataProcessor = new Processor();
        BrushConverter bc = new BrushConverter();
        public AuthResponse AuthResponse;
        public CrosshairProfile SelectedProfile;
        List<Color> SelectedColors;
        public API ValCCAPI;
        public int SelectedIndex;
        public bool LoggedIn;
        public bool PressedForceLogin = false;
        private string _sharecode;
        private FetchResponse? sharedProfileResp;
        public MainWindow()
        {
            // Create logging dir
            if (!Directory.Exists(Path.GetDirectoryName(Utilities.Utils.LoggingFile))) Directory.CreateDirectory(Path.GetDirectoryName(Utilities.Utils.LoggingFile));
            // Replace old logs
            if (File.Exists(Utilities.Utils.LoggingFile)) File.Move(Utilities.Utils.LoggingFile, Path.GetDirectoryName(Utilities.Utils.LoggingFile) + "\\" + Path.GetFileNameWithoutExtension(Utilities.Utils.LoggingFile) + "-old" + Path.GetExtension(Utilities.Utils.LoggingFile), true);
            Version ProgramFileVersion = new Version(FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName).ProductVersion);

            InitializeComponent();
            Utilities.Utils.Log($"앱 시작됨 | v{ProgramFileVersion}. 이전 로그 파일을 교체했습니다.");
            Title = $"ValorantCC v{ProgramFileVersion} 한글패치: Machos#7828";
            Random nClicks = new Random();
            for (int i = 0; i < nClicks.Next(0, Resources.MergedDictionaries[0].Count - 1); i++)
                next_Click(null, null);
        }
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!LoggedIn)
            {
                Utilities.Utils.MessageText("로그인하지 않았습니다!", Brushes.Red);
                return;
            }
            Utilities.Utils.MessageText("저장중...", Brushes.Yellow);
            SelectedColors = new List<Color> { (Color)primary_color.SelectedColor, (Color)prim_outline_color.SelectedColor, (Color)ads_color.SelectedColor, (Color)ads_outline_color.SelectedColor, (Color)sniper_dot_color.SelectedColor };

            if (await DataProcessor.SaveNewColor(SelectedColors, profiles.SelectedIndex, profiles.Text))
            {
                profiles.Items.Refresh();
                profiles.SelectedIndex = DataProcessor.CurrentProfile;
                Utilities.Utils.MessageText("저장되었습니다! 발로란트를 다시 시작하십시오.", Brushes.Lime);
                return;
            }
            Utilities.Utils.MessageText("세션이 만료되었습니다! ValorantCC/Valorant를 다시 시작하세요.", Brushes.Red);

            return;
        }

        private void profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (profiles.SelectedIndex == -1) return;

            SelectedIndex = profiles.SelectedIndex;
            SelectedProfile = DataProcessor.ProfileFromIndex(SelectedIndex);

            primary_color.SelectedColor = Color.FromRgb(SelectedProfile.Primary.Color.R, SelectedProfile.Primary.Color.G, SelectedProfile.Primary.Color.B);
            prim_outline_color.SelectedColor = Color.FromRgb(SelectedProfile.Primary.OutlineColor.R, SelectedProfile.Primary.OutlineColor.G, SelectedProfile.Primary.OutlineColor.B);
            if (SelectedProfile.aDS == null) SelectedProfile.aDS = SelectedProfile.Primary;
            if (SelectedProfile.bUsePrimaryCrosshairForADS) SelectedProfile.aDS.Color = SelectedProfile.Primary.Color;
            ads_color.SelectedColor = Color.FromRgb(SelectedProfile.aDS.Color.R, SelectedProfile.aDS.Color.G, SelectedProfile.aDS.Color.B);
            ads_outline_color.SelectedColor = Color.FromRgb(SelectedProfile.aDS.OutlineColor.R, SelectedProfile.aDS.OutlineColor.G, SelectedProfile.aDS.OutlineColor.B);
            sniper_dot_color.SelectedColor = Color.FromRgb(SelectedProfile.Sniper.CenterDotColor.R, SelectedProfile.Sniper.CenterDotColor.G, SelectedProfile.Sniper.CenterDotColor.B);
            /*if (!sharedProfileResp.HasValue)
            {
                if (ValCCAPI != null) sharedProfileResp = await ValCCAPI.ObtainSelfSaved();
            }
            else
            {
                var respData = sharedProfileResp.Value;
                if (respData.success && respData.data != null)
                {
                    var data = respData.data.FirstOrDefault();
                    _sharecode = data.shareCode;
                    CrosshairProfile fromDb = JsonConvert.DeserializeObject<CrosshairProfile>(data.settings);
                    if (SelectedProfile.ProfileName == fromDb.ProfileName)
                    {
                        chkbxShareable.IsChecked = data.shareable;
                        btnCopyShareCode.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        chkbxShareable.IsChecked = false;
                        btnCopyShareCode.Visibility = Visibility.Collapsed;
                    }
                }
            }*/
            Crosshair_load();
        }
        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private async void btnReload_Click(object sender, RoutedEventArgs e)
        {
            if (!LoggedIn)
            {
                Utilities.Utils.MessageText("로그인하지 않았습니다!", Brushes.Red);
                return;
            }
            Utilities.Utils.Log("새로고침 클릭 > 프로세서 재구성.");
            if (!(await DataProcessor.Construct()))
                Utilities.Utils.MessageText("세션이 만료되었습니다! ValorantCC/Valorant를 다시 시작하세요.", Brushes.Red);

            profiles.ItemsSource = DataProcessor.ProfileNames;
            profiles.Items.Refresh();
            profiles_SelectionChanged(null, null);
            profiles.SelectedIndex = DataProcessor.CurrentProfile;
        }

        private void profiles_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!LoggedIn) return;
            DataProcessor.ProfileNames[SelectedIndex] = profiles.Text;
            profiles.Items.Refresh();
            profiles.SelectedIndex = SelectedIndex;
        }

        public void Crosshair_load()
        {
            //Primary
            Crosshair_Parser.dot_redraw(primeDOT, primeDOTOT, SelectedProfile.Primary);
            Crosshair_Parser.rectangle_redraw(primeX1, primeX1OT, Crosshair_Parser.Position.East, SelectedProfile.Primary);
            Crosshair_Parser.rectangle_redraw(primeX2, primeX2OT, Crosshair_Parser.Position.West, SelectedProfile.Primary);
            Crosshair_Parser.rectangle_redraw(primeY1, primeY1OT, Crosshair_Parser.Position.North, SelectedProfile.Primary);
            Crosshair_Parser.rectangle_redraw(primeY2, primeY2OT, Crosshair_Parser.Position.South, SelectedProfile.Primary);

            Crosshair_Parser.rectangle_redraw(primeOLX1, primeOLX1OT, Crosshair_Parser.Position.East, SelectedProfile.Primary);
            Crosshair_Parser.rectangle_redraw(primeOLX2, primeOLX2OT, Crosshair_Parser.Position.West, SelectedProfile.Primary);
            Crosshair_Parser.rectangle_redraw(primeOLY1, primeOLY1OT, Crosshair_Parser.Position.North, SelectedProfile.Primary);
            Crosshair_Parser.rectangle_redraw(primeOLY2, primeOLY2OT, Crosshair_Parser.Position.South, SelectedProfile.Primary);

            //ADS
            var cross = SelectedProfile.Primary;
            if (!SelectedProfile.bUsePrimaryCrosshairForADS)
                cross = SelectedProfile.aDS;

            Crosshair_Parser.dot_redraw(adsDOT, adsDOTOT, cross);
            Crosshair_Parser.rectangle_redraw(adsX1, adsX1OT, Crosshair_Parser.Position.East, cross);
            Crosshair_Parser.rectangle_redraw(adsX2, adsX2OT, Crosshair_Parser.Position.West, cross);
            Crosshair_Parser.rectangle_redraw(adsY1, adsY1OT, Crosshair_Parser.Position.North, cross);
            Crosshair_Parser.rectangle_redraw(adsY2, adsY2OT, Crosshair_Parser.Position.South, cross);

            Crosshair_Parser.rectangle_redraw(adsOLX1, adsOLX1OT, Crosshair_Parser.Position.East, cross);
            Crosshair_Parser.rectangle_redraw(adsOLX2, adsOLX2OT, Crosshair_Parser.Position.West, cross);
            Crosshair_Parser.rectangle_redraw(adsOLY1, adsOLY1OT, Crosshair_Parser.Position.North, cross);
            Crosshair_Parser.rectangle_redraw(adsOLY2, adsOLY2OT, Crosshair_Parser.Position.South, cross);

            //Sniper
            Crosshair_Parser.dot_redraw(sniperdot, SelectedProfile.Sniper);
        }

        private void primary_color_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            primeDOT.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
            primeX1.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
            primeX2.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
            primeY1.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
            primeY2.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());

            primeOLX1.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
            primeOLX2.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
            primeOLY1.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
            primeOLY2.Fill = (Brush)bc.ConvertFrom(primary_color.SelectedColor.ToString());
        }

        private void prim_outline_color_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            primeDOTOT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
            primeX1OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
            primeX2OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
            primeY1OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
            primeY2OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());

            primeOLX1OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
            primeOLX2OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
            primeOLY1OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
            primeOLY2OT.Stroke = (Brush)bc.ConvertFrom(prim_outline_color.SelectedColor.ToString());
        }

        private void ads_color_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            adsDOT.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
            adsX1.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
            adsX2.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
            adsY1.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
            adsY2.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());

            adsOLX1.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
            adsOLX2.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
            adsOLY1.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
            adsOLY2.Fill = (Brush)bc.ConvertFrom(ads_color.SelectedColor.ToString());
        }

        private void ads_outline_color_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            adsDOTOT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
            adsX1OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
            adsX2OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
            adsY1OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
            adsY2OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());

            adsOLX1OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
            adsOLX2OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
            adsOLY1OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
            adsOLY2OT.Stroke = (Brush)bc.ConvertFrom(ads_outline_color.SelectedColor.ToString());
        }

        private void sniper_dot_color_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            sniperdot.Fill = (Brush)bc.ConvertFrom(sniper_dot_color.SelectedColor.ToString());
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TabButtonEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void TabButtonLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush(Colors.White);
        }

        private void ClipboardButtonEnter(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name == "btnCopyLogs") CopyButtonLabel.FontSize += 1;
            else communitylist_label.FontSize += 1;
        }
        private void ClipboardButtonLeave(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name == "btnCopyLogs") CopyButtonLabel.FontSize -= 1;
            else communitylist_label.FontSize -= 1;
        }

        private void btnOpenLogs_Click(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo() { FileName = Path.GetDirectoryName(Utilities.Utils.LoggingFile), UseShellExecute = true };
            p.Start();
            Utilities.Utils.MessageText("로그 폴더가 열렸습니다! 존재하는 경우 보고서에 OLD 파일을 포함하십시오.", Brushes.Lime);
        }

        private void next_Click(object sender, RoutedEventArgs e)
        {
            CrosshairBG.Source = Get_CrosshairBG();
        }

        private void previos_Click(object sender, RoutedEventArgs e)
        {
            CrosshairBG.Source = Get_CrosshairBG(false);
        }

        private ImageSource Get_CrosshairBG(bool next = true)
        {
            int number = int.Parse(System.IO.Path.GetFileNameWithoutExtension(CrosshairBG.Source.ToString()).Replace("CrosshairBG", "")) + (next ? 1 : -1);

            if (next && number > Resources.MergedDictionaries[0].Count - 1)
                number = 0;
            else if (number < 0)
                number = Resources.MergedDictionaries[0].Count - 1;

            return (ImageSource)FindResource("crosshairBG" + number);
        }

        private void btnCommunityProfiles_Click(object sender, RoutedEventArgs e)
        {
            if (!LoggedIn)
            {
                Utilities.Utils.MessageText("로그인하지 않았습니다!", Brushes.Red);
                return;
            }
            try
            {
                Window publicProfiles = Application.Current.Windows.Cast<Window>().Single(window => window.GetType().ToString() == "ValorantCC.ProfilesWindow");
                publicProfiles.Activate();
            }
            catch (Exception)
            {
                ProfilesWindow publicProfiles = new ProfilesWindow(SelectedProfile, ValCCAPI);
                publicProfiles.Owner = this;
                publicProfiles.Show();
            }
        }

        private void chkbxShareable_Click(object sender, RoutedEventArgs e)
        {
            if (!LoggedIn)
            {
                Utilities.Utils.MessageText("로그인하지 않았습니다!", Brushes.Red);
                ((CheckBox)sender).IsChecked = !((CheckBox)sender).IsChecked;
                return;
            }
        }

        private async void btnShare_Click(object sender, RoutedEventArgs e)
        {
            if (!LoggedIn)
            {
                Utilities.Utils.MessageText("로그인하지 않았습니다!", Brushes.Red);
                return;
            }
            Utilities.Utils.MessageText("프로필이 저장되고 있습니다...", Brushes.Yellow);

            ValCCAPI.Shareable = (bool)chkbxShareable.IsChecked;
            ValCCAPI.profile = SelectedProfile;
            SetCallResponse response = await ValCCAPI.Set();
            if (!response.success)
            {
                Utilities.Utils.Log($"십자선을 공유할 수 없음: {response.message}");
                Utilities.Utils.MessageText("십자선을 공유할 수 없음: 내부 오류/충돌", Brushes.Red);
                return;
            }
            String sharecode = response.message;
            Clipboard.SetText(sharecode);
            MessageWindow.Show($"공유 코드는 \"{sharecode}\"이며 복사되었습니다.\n이 프로필을 커뮤니티 전체에서 액세스하려면\'공유 가능' 확인란이 선택되어 있는지 확인하십시오.", "프로필이 공유되었습니다!");

            Utilities.Utils.MessageText("프로필이 저장되었습니다. 이제 저장하기 전에 \"공유 가능\" 확인란이 선택되어 있으면 탐색할 수 있습니다.", Brushes.Lime);
        }

        private async void spinner_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundAuth auth = new BackgroundAuth(DataProcessor);

            try
            {
                if (Directory.Exists(Path.GetTempPath() + $"EZ_Updater0"))
                {
                    if (Directory.Exists(Path.GetTempPath() + $"EZ_Updater0{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}"))
                        Directory.Delete(Path.GetTempPath() + $"EZ_Updater0{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}", true);
                    Directory.Move(Path.GetTempPath() + $"EZ_Updater0", Path.GetTempPath() + $"EZ_Updater0{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}");
                }
                Updater.CustomLogger = Utilities.Utils.Log;
                Updater.LogInterfix = "  | ";
                if (await Updater.CheckUpdateAsync("개발자: weedeej\n\n한글패치: Machos#7828", "ValorantCC"))
                {
                    if (Updater.CannotWriteOnDir)
                    {
                        Utilities.Utils.Log("사용자는 현재 valcc 디렉토리에 파일을 생성할 권한이 없습니다. 이사를 고려하십시오.");
                        MessageWindow.Show("사용 가능한 업데이트가 있지만 이 폴더에 쓸 수 있는 권한이 없습니다.\n사용자가 만든 폴더로 앱을 이동하거나 앱을 관리자로 실행하는 것을 고려해 보세요.");
                        this.Close();
                    }
                    bool UpdateMessage = MessageWindow.Show($"사용 가능한 업데이트가 있습니다. 업데이트하시겠습니까? {Updater.ReleaseVersion}?", Updater.ReleaseName, true);
                    if (!UpdateMessage)
                    {
                        if (Updater.ReleaseName.Contains("[!]"))
                        {
                            MessageWindow.Show("이 업데이트에는 중요한 패치가 포함되어 있으므로 건너뛸 수 없습니다.");
                            var update = new UpdateWindow();
                            update.Owner = this;
                            update.ShowDialog();
                        }
                        else Utilities.Utils.Log($"사용자가 릴리스를 건너뛰었습니다.: {Updater.ReleaseName}");
                    }
                    else
                    {
                        var update = new UpdateWindow();
                        update.Owner = this;
                        update.ShowDialog();
                    }
                }
            } catch (Exception ex)
            {
                Utilities.Utils.Log($"{ex.Message}: {ex.StackTrace}");
            }
            auth.LoopCheck();
        }

        private void btnCopyShareCode_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_sharecode);
            Utilities.Utils.MessageText("공유 코드가 복사되었습니다!", Brushes.Lime);
        }

        private void _zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //
        }

        private void ForceLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            PressedForceLogin = true;
            ForceLoginBtn.Content = "강제 로그인 . . .";
            ForceLoginBtn.Background = (Brush)bc.ConvertFrom("#FF44464F");
        }
    }
}
