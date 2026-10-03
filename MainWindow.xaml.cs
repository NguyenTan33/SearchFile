using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SearchFile.Models;
using SearchFile.Services;

namespace SearchFile
{
    public partial class MainWindow : Window
    {
        private readonly FileSearchService _searchService = new();
        private readonly ObservableCollection<FileSearchResult> _results = new();
        private CancellationTokenSource? _cts;
        private Stopwatch _stopwatch = new();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                Activate();
                Focus();
                Topmost = true;
                Topmost = false;
            };

            GridResults.ItemsSource = _results;
            LoadDrives();
        }

        private void LoadDrives()
        {
            var drives = _searchService.GetAvailableDrives();
            CmbDrives.ItemsSource = drives;
            if (drives.Count > 0) CmbDrives.SelectedIndex = 0;
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await TriggerSearchAsync();
        }

        private async void CmbDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await TriggerSearchAsync();
        }

        private async void Filter_Checked(object sender, RoutedEventArgs e)
        {
            await TriggerSearchAsync();
        }

        private string GetSelectedCategory()
        {
            if (RbDocs?.IsChecked == true) return "Văn bản";
            if (RbImages?.IsChecked == true) return "Hình ảnh";
            if (RbExec?.IsChecked == true) return "Phần mềm/Script";
            if (RbCode?.IsChecked == true) return "Code/SQL";
            if (RbFolders?.IsChecked == true) return "Thư mục";
            return "Tất cả";
        }

        private async Task TriggerSearchAsync()
        {
            if (TxtSearch == null || CmbDrives == null || TxtStatus == null || OverlaySearching == null) return;

            string searchText = TxtSearch.Text.Trim();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _results.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                TxtStatus.Text = "Sẵn sàng. Gõ tên file vào ô trên để tìm kiếm.";
                OverlaySearching.Visibility = Visibility.Collapsed;
                return;
            }

            OverlaySearching.Visibility = Visibility.Visible;
            TxtStatus.Text = $"⏳ Đang tìm kiếm '{searchText}'...";
            _stopwatch.Restart();

            string drive = CmbDrives.SelectedItem?.ToString() ?? "Tất cả";
            string category = GetSelectedCategory();

            try
            {
                await _searchService.SearchFilesAsync(
                    searchText,
                    drive,
                    category,
                    batch =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            foreach (var item in batch)
                            {
                                _results.Add(item);
                            }
                            _stopwatch.Stop();
                            TxtStatus.Text = $"✅ Tìm thấy {_results.Count:N0} file ({_stopwatch.Elapsed.TotalMilliseconds:N0} ms)";
                        });
                    },
                    token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Lỗi: {ex.Message}";
            }
            finally
            {
                OverlaySearching.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            TxtSearch.Focus();
        }

        private void GridResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenFile(GridResults.SelectedItem as FileSearchResult);
        }

        private void GridResults_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(GridResults.SelectedItem as FileSearchResult);
        }

        private void MenuOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is FileSearchResult selected && !string.IsNullOrEmpty(selected.FilePath))
            {
                try
                {
                    if (File.Exists(selected.FilePath) || Directory.Exists(selected.FilePath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{selected.FilePath}\"");
                    }
                    else if (Directory.Exists(selected.DirectoryPath))
                    {
                        Process.Start("explorer.exe", $"\"{selected.DirectoryPath}\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is FileSearchResult selected)
            {
                Clipboard.SetText(selected.FilePath);
                TxtStatus.Text = $"📋 Đã copy đường dẫn: {selected.FilePath}";
            }
        }

        private void MenuCopyName_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is FileSearchResult selected)
            {
                Clipboard.SetText(selected.FileName);
                TxtStatus.Text = $"📋 Đã copy tên file: {selected.FileName}";
            }
        }

        private static void OpenFile(FileSearchResult? item)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath)) return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = item.FilePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi mở file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
            }
            else if (e.Key == Key.Escape)
            {
                if (TxtSearch.IsFocused)
                {
                    TxtSearch.Clear();
                }
            }
        }
    }
}