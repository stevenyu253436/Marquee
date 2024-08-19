using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

public class MainViewModel : INotifyPropertyChanged
{
    private string _selectedMarquee1Item;
    private string _selectedMarquee2Item;
    private string _textBoxForMarquee2;

    public ObservableCollection<string> Marquee1Items { get; set; }
    public ObservableCollection<string> Marquee2Items { get; set; }
    public Dictionary<string, string> RoomDictionary { get; private set; }

    public string SelectedMarquee1Item
    {
        get => _selectedMarquee1Item;
        set
        {
            if (_selectedMarquee1Item != value)
            {
                _selectedMarquee1Item = value;
                OnPropertyChanged();
                UpdateTextBox1();
            }
        }
    }

    public string SelectedMarquee2Item
    {
        get => _selectedMarquee2Item;
        set
        {
            if (_selectedMarquee2Item != value)
            {
                _selectedMarquee2Item = value;
                TextBoxForMarquee2 = value; // 更新 TextBoxForMarquee2
                OnPropertyChanged();
                UpdateTextBox1();
            }
        }
    }

    public string TextBoxForMarquee2
    {
        get => _textBoxForMarquee2;
        set
        {
            if (_textBoxForMarquee2 != value)
            {
                _textBoxForMarquee2 = value;
                OnPropertyChanged();
                UpdateTextBox1();
            }
        }
    }

    private string _textBox1;
    public string TextBox1
    {
        get => _textBox1;
        set
        {
            if (_textBox1 != value)
            {
                _textBox1 = value;
                OnPropertyChanged();
            }
        }
    }

    private string _textBox2;
    public string TextBox2
    {
        get => _textBox2;
        set
        {
            if (_textBox2 != value)
            {
                _textBox2 = value;
                OnPropertyChanged();
            }
        }
    }

    public MainViewModel()
    {
        Marquee1Items = new ObservableCollection<string>(File.ReadAllLines(@"txt\marquee1_items.txt"));
        Marquee2Items = new ObservableCollection<string>(File.ReadAllLines(@"txt\marquee2_items.txt"));
        RoomDictionary = new Dictionary<string, string>();
        LoadRoomNumbers();
    }

    private void LoadRoomNumbers()
    {
        try
        {
            string filePath = @"txt\room.txt";
            string[] lines = File.ReadAllLines(filePath);

            // 添加 "全部" 選項
            RoomDictionary.Add("全部", "全部");

            foreach (string line in lines)
            {
                string[] parts = line.Split(';');
                if (parts.Length > 1)
                {
                    string[] roomNumbers = parts[1].Split(',');
                    foreach (string room in roomNumbers)
                    {
                        // 保留完整的主機名
                        string shortRoomNumber = room.Substring(room.Length - 3);
                        RoomDictionary.Add(shortRoomNumber, room);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading file: " + ex.Message);
        }
    }

    private void UpdateTextBox1()
    {
        TextBox1 = $"{SelectedMarquee1Item ?? ""} {(string.IsNullOrEmpty(TextBoxForMarquee2) ? "" : " " + TextBoxForMarquee2 + " ")} {SelectedMarquee2Item ?? ""}";

        // 更新 TextBox2 (第二行)
        UpdateTextBox2();
    }

    private void UpdateTextBox2()
    {
        // 假設第二行的文字依賴於第一行
        // 你可以根據你的具體需求來設計這段邏輯
        TextBox2 = $"{SelectedMarquee2Item ?? ""}";
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
