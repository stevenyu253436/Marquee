using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Marquee
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            //AllocConsole(); // 调用 AllocConsole 方法以打开控制台窗口
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            LoadRoomNumbers();
        }

        private void LoadRoomNumbers()
        {
            foreach (var room in _viewModel.RoomDictionary.Keys)
            {
                RoomNumbersComboBox.Items.Add(room);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("TextBox1 Width: {0}", TextBox1.ActualWidth);
        }

        private void UpdateTextBox1()
        {
            string part1 = Marquee1ComboBox.SelectedItem?.ToString() ?? "";
            string middle = TextBox3.Text.Trim();
            string part3 = Marquee2ComboBox.SelectedItem?.ToString() ?? "";

            TextBox1.Text = part1 + (string.IsNullOrEmpty(middle) ? "" : " " + middle + " ") + part3;
        }

        private void MarqueeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTextBox1();  // 更新 TextBox1 使用來自 ComboBox 和 TextBox3 的值
        }

        private void TextBox3_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTextBox1();  // 這同樣更新 TextBox1 當 TextBox3 的內容改變時
        }

        private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            double maxWidth = 480; // TextBox1的实际宽度

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // 创建FormattedText以计算文本宽度
            FormattedText formattedText = new FormattedText(
                textBox.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                textBox.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                TextFormattingMode.Display,
                pixelsPerDip);  // 加入PixelsPerDip参数

            // 检查文本渲染宽度是否超出TextBox的宽度
            if (formattedText.Width > maxWidth)
            {
                // 找到能够在TextBox1中完全显示的最大文本长度
                int maxFittingLength = 0;
                for (int i = 1; i <= textBox.Text.Length; i++)
                {
                    formattedText = new FormattedText(
                        textBox.Text.Substring(0, i),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                        textBox.FontSize,
                        Brushes.Black,
                        new NumberSubstitution(),
                        TextFormattingMode.Display,
                        pixelsPerDip);  // 加入PixelsPerDip参数

                    if (formattedText.Width > maxWidth)
                    {
                        maxFittingLength = i - 1;
                        break;
                    }
                }

                // 分割文本
                if (maxFittingLength > 0)
                {
                    TextBox2.Text = textBox.Text.Substring(maxFittingLength);
                    textBox.Text = textBox.Text.Substring(0, maxFittingLength);
                }
            }
        }

        private async void SendAnnouncement_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Triggered Send Announcement"); // 确认事件触发

            string marquee1Content = Marquee1ComboBox.SelectedItem?.ToString() ?? "";
            string marquee2Content = Marquee2ComboBox.SelectedItem?.ToString() ?? "";
            //string marquee1Content = _viewModel.SelectedMarquee1Item ?? "";
            //string marquee2Content = _viewModel.SelectedMarquee2Item ?? "";
            string announcement = $"{marquee1Content} {TextBox3.Text.Trim()} {marquee2Content}";

            if (string.IsNullOrWhiteSpace(announcement))
            {
                System.Windows.MessageBox.Show("公告内容不能為空！");
                return;
            }

            List<string> hostNames = new List<string>();

            // 獲取選中的房間號碼
            string selectedRoom = RoomNumbersComboBox.SelectedItem?.ToString();

            //if (!string.IsNullOrEmpty(selectedRoom) && roomDictionary.ContainsKey(selectedRoom))
            if (!string.IsNullOrEmpty(selectedRoom) && _viewModel.RoomDictionary.ContainsKey(selectedRoom))
            {
                if (selectedRoom == "全部")
                {
                    // 如果選擇的是 "全部"，則發送給所有主機
                    //hostNames = roomDictionary.Values.ToList();
                    hostNames = _viewModel.RoomDictionary.Values.ToList();
                }
                else
                {
                    // 否則，只發送給選中的主機
                    //hostNames.Add(roomDictionary[selectedRoom]);
                    hostNames.Add(_viewModel.RoomDictionary[selectedRoom]);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("請選擇一個有效的房間號碼！");
                return;
            }

            await Task.Run(async () =>
            {
                foreach (var hostName in hostNames)
                {
                    await SendAnnouncementAsync(hostName, announcement); // 在后台线程执行异步操作
                }
            }).ConfigureAwait(false); // 确保不捕获上下文
        }

        private const int MaxSendAttempts = 3; // Maximum number of send attempts
        private const int DelayBetweenSendAttempts = 1000; // Delay between send attempts in milliseconds

        private async Task SendAnnouncementAsync(string hostname, string message)
        {
            int attemptCount = 0;
            bool messageSent = false;
            Exception lastException = null;

            while (attemptCount < MaxSendAttempts && !messageSent)
            {
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        await client.ConnectAsync(hostname, 1000).ConfigureAwait(false);
                        using (NetworkStream stream = client.GetStream())
                        {
                            byte[] data = Encoding.UTF8.GetBytes(message + "\r\n");
                            await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                            // Ensure MessageBox is shown on the UI thread
                            await Application.Current.Dispatcher.InvokeAsync(() => System.Windows.MessageBox.Show("Message sent successfully."));
                            messageSent = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Error on send attempt {attemptCount + 1}: {ex.Message}");
                    lastException = ex;
                }

                attemptCount++;
                if (!messageSent && attemptCount < MaxSendAttempts)
                {
                    await Task.Delay(DelayBetweenSendAttempts).ConfigureAwait(false);
                }
            }

            if (!messageSent)
            {
                LogToFile($"Failed to send the announcement after multiple attempts. Last error: {lastException?.Message}");
            }
        }

        private void LogToFile(string message)
        {
            string logFilePath = "log.txt"; // 设定日志文件的路径
            string logMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - {message}\r\n";

            // 使用 File.AppendAllText 异步写入日志信息，它会自动创建文件（如果文件不存在的话）
            File.AppendAllText(logFilePath, logMessage);
        }

        private void ToggleVisibility(object sender, RoutedEventArgs e)
        {
            if (SettingsGroupBox.Visibility == Visibility.Collapsed)
            {
                SettingsGroupBox.Visibility = Visibility.Visible;
                this.Height = 660;  // 調整窗口高度以容納GroupBox
            }
            else
            {
                SettingsGroupBox.Visibility = Visibility.Collapsed;
                this.Height = 420;  // 調整窗口高度回到較小尺寸

                // 清空 ComboBox 的選擇
                EditMarquee1ComboBox.SelectedItem = null;
                EditMarquee2ComboBox.SelectedItem = null;

                // 清空 TextBox 的文字
                TextBoxForMarquee1.Text = "";
                TextBoxForMarquee2.Text = "";
            }
        }
        private void EditMarquee1ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 確認是否有選中的項目
            if (EditMarquee1ComboBox.SelectedItem != null)
            {
                TextBoxForMarquee1.Text = EditMarquee1ComboBox.SelectedItem.ToString();
            }
        }
        private void DeleteMarquee1Button_Click(object sender, RoutedEventArgs e)
        {
            if (EditMarquee1ComboBox.SelectedItem != null)
            {
                var itemToRemove = EditMarquee1ComboBox.SelectedItem.ToString();

                // 直接操作绑定的数据源
                ((ObservableCollection<string>)EditMarquee1ComboBox.ItemsSource).Remove(itemToRemove);

                // 清空相应的 TextBox 内容
                TextBoxForMarquee1.Clear();

                // 从文件中移除
                RemoveItemFromMarqueeFile(itemToRemove, @"txt\marquee1_items.txt");
            }
        }

        // 處理新增按鈕點擊事件
        private void AddMarquee1Button_Click(object sender, RoutedEventArgs e)
        {
            var newItem = TextBoxForMarquee1.Text;

            if (!string.IsNullOrEmpty(newItem))
            {
                var items = (ObservableCollection<string>)EditMarquee1ComboBox.ItemsSource;

                if (!items.Contains(newItem))
                {
                    items.Add(newItem);

                    // 添加到文件
                    AddItemToMarqueeFile(newItem, @"txt\marquee1_items.txt");
                }
            }
        }

        private void EditMarquee2ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = (MainViewModel)DataContext;
            if (EditMarquee2ComboBox.SelectedItem != null && viewModel.SelectedMarquee2Item != EditMarquee2ComboBox.SelectedItem.ToString())
            {
                viewModel.SelectedMarquee2Item = EditMarquee2ComboBox.SelectedItem.ToString();
            }
        }

        private void DeleteMarquee2Button_Click(object sender, RoutedEventArgs e)
        {
            if (EditMarquee2ComboBox.SelectedItem != null)
            {
                var itemToRemove = EditMarquee2ComboBox.SelectedItem.ToString();

                // 直接操作绑定的数据源
                ((ObservableCollection<string>)EditMarquee2ComboBox.ItemsSource).Remove(itemToRemove);

                // 清空相应的 TextBox 内容
                TextBoxForMarquee2.Clear();

                // 从文件中移除
                RemoveItemFromMarqueeFile(itemToRemove, @"txt\marquee2_items.txt");
            }
        }

        private void TextBoxForMarquee2_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModel = (MainViewModel)DataContext;
            var textBox = sender as TextBox;
            //if (textBox != null && textBox.Text != viewModel.TextBoxForMarquee2)
            if (textBox != null)
            {
                Console.WriteLine(textBox.Text);
                // 更新 ViewModel 中的 TextBoxForMarquee2 属性
                if (viewModel.TextBoxForMarquee2 != textBox.Text)
                {
                    viewModel.TextBoxForMarquee2 = textBox.Text;
                    Console.WriteLine("TextBoxForMarquee2 Updated: " + textBox.Text);
                    viewModel.OnPropertyChanged(nameof(viewModel.TextBoxForMarquee2)); // 手动触发 PropertyChanged
                }

                // 更新 ComboBox 的 SelectedItem
                if (viewModel.SelectedMarquee2Item != textBox.Text)
                {
                    viewModel.SelectedMarquee2Item = textBox.Text;
                    Console.WriteLine("SelectedMarquee2Item Updated: " + textBox.Text);
                    viewModel.OnPropertyChanged(nameof(viewModel.SelectedMarquee2Item)); // 手动触发 PropertyChanged
                }
            }
            else
            {
                Console.WriteLine("TextBox is null"); // 确认 sender 被正确转换
            }
        }

        private void RemoveItemFromMarqueeFile(string item, string filePath)
        {
            var lines = File.ReadAllLines(filePath).ToList();
            bool itemFound = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(item))
                {
                    lines.RemoveAt(i); // 删除整行
                    itemFound = true;
                    break; // 假设每个项目只出现一次，找到即删除
                }
            }

            if (itemFound)
            {
                File.WriteAllLines(filePath, lines);
            }
        }

        // 處理新增按鈕點擊事件
        private void AddMarquee2Button_Click(object sender, RoutedEventArgs e)
        {
            var newItem = TextBoxForMarquee2.Text;

            if (!string.IsNullOrEmpty(newItem))
            {
                var items = (ObservableCollection<string>)EditMarquee2ComboBox.ItemsSource;

                if (!items.Contains(newItem))
                {
                    items.Add(newItem);

                    // 添加到文件
                    AddItemToMarqueeFile(newItem, @"txt\marquee2_items.txt");
                }
            }
        }

        private void AddItemToMarqueeFile(string item, string filePath)
        {
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(item);
            }
        }
    }
}
