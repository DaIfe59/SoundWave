using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SoundWaveShared.Dtos;
using SoundWaveWPF.Models;

namespace SoundWaveWPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly HttpClient HttpClient = new HttpClient();
    private ObservableCollection<TrackDto> _tracks = new();
    private ObservableCollection<PlaylistDto> _playlists = new();
    private ObservableCollection<SelectedFile> _selectedFiles = new();
    private TrackDto? _selectedTrack;
    private PlaylistDto? _selectedPlaylist;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadPlaylists();
    }

    // Properties for data binding
    public ObservableCollection<TrackDto> Tracks => _tracks;
    public ObservableCollection<PlaylistDto> Playlists => _playlists;
    public ObservableCollection<SelectedFile> SelectedFiles => _selectedFiles;
    public string SearchQuery { get; set; } = string.Empty;
    public string NewPlaylistName { get; set; } = string.Empty;

    private async void BtnStatus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var uri = new Uri("http://localhost:5209/status");
            var response = await HttpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            TxtStatus.Text = $"Статус сервера: {json}";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка: {ex.Message}";
        }
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TxtStatus.Text = "Поиск треков...";
            
            var searchQuery = TxtSearch.Text.Trim();
            var uri = string.IsNullOrEmpty(searchQuery) 
                ? new Uri("http://localhost:5209/api/track")
                : new Uri($"http://localhost:5209/api/track?search={Uri.EscapeDataString(searchQuery)}");
            
            var response = await HttpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var tracks = await response.Content.ReadFromJsonAsync<List<TrackDto>>();
            
            _tracks.Clear();
            if (tracks != null)
            {
                foreach (var track in tracks)
                {
                    _tracks.Add(track);
                }
            }
            
            TxtStatus.Text = $"Найдено треков: {_tracks.Count}";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка поиска: {ex.Message}";
        }
    }

    private async void LoadPlaylists()
    {
        try
        {
            var uri = new Uri("http://localhost:5209/api/playlist");
            var response = await HttpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var playlists = await response.Content.ReadFromJsonAsync<List<PlaylistDto>>();
            
            _playlists.Clear();
            if (playlists != null)
            {
                foreach (var playlist in playlists)
                {
                    _playlists.Add(playlist);
                }
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка загрузки плейлистов: {ex.Message}";
        }
    }

    private async void BtnCreatePlaylist_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var playlistName = TxtPlaylistName.Text.Trim();
            if (string.IsNullOrEmpty(playlistName))
            {
                TxtStatus.Text = "Введите название плейлиста";
                return;
            }

            var newPlaylist = new PlaylistDto
            {
                Name = playlistName,
                Description = string.Empty
            };

            var uri = new Uri("http://localhost:5209/api/playlist");
            var response = await HttpClient.PostAsJsonAsync(uri, newPlaylist);
            response.EnsureSuccessStatusCode();
            
            TxtPlaylistName.Text = string.Empty;
            LoadPlaylists();
            TxtStatus.Text = "Плейлист создан успешно";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка создания плейлиста: {ex.Message}";
        }
    }

    private void LstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedTrack = LstTracks.SelectedItem as TrackDto;
        BtnAddToPlaylist.IsEnabled = _selectedTrack != null;
        BtnPlay.IsEnabled = _selectedTrack != null;
    }

    private void LstPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPlaylist = LstPlaylists.SelectedItem as PlaylistDto;
        BtnDeletePlaylist.IsEnabled = _selectedPlaylist != null;
        BtnViewPlaylist.IsEnabled = _selectedPlaylist != null;
    }

    private async void BtnAddToPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTrack == null || _selectedPlaylist == null)
        {
            TxtStatus.Text = "Выберите трек и плейлист";
            return;
        }

        try
        {
            var uri = new Uri($"http://localhost:5209/api/playlist/{_selectedPlaylist.Id}/tracks/{_selectedTrack.Id}");
            var response = await HttpClient.PostAsync(uri, null);
            
            if (response.IsSuccessStatusCode)
            {
                LoadPlaylists();
                TxtStatus.Text = $"Трек '{_selectedTrack.Title}' добавлен в плейлист '{_selectedPlaylist.Name}'";
            }
            else
            {
                TxtStatus.Text = $"Ошибка добавления трека: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка добавления трека: {ex.Message}";
        }
    }

    private void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTrack != null)
        {
            TxtStatus.Text = $"Воспроизведение: {_selectedTrack.Title} - {_selectedTrack.Artist}";
            // TODO: Implement actual audio playback
        }
    }

    private async void BtnDeletePlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPlaylist == null)
        {
            TxtStatus.Text = "Выберите плейлист для удаления";
            return;
        }

        try
        {
            var uri = new Uri($"http://localhost:5209/api/playlist/{_selectedPlaylist.Id}");
            var response = await HttpClient.DeleteAsync(uri);
            
            if (response.IsSuccessStatusCode)
            {
                LoadPlaylists();
                TxtStatus.Text = $"Плейлист '{_selectedPlaylist.Name}' удален";
            }
            else
            {
                TxtStatus.Text = $"Ошибка удаления плейлиста: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка удаления плейлиста: {ex.Message}";
        }
    }

    private void BtnViewPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPlaylist != null)
        {
            TxtStatus.Text = $"Плейлист '{_selectedPlaylist.Name}' содержит {_selectedPlaylist.Tracks.Count} треков";
            // TODO: Show playlist details window
        }
    }

    private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Выберите аудиофайлы",
            Filter = "Аудиофайлы|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg|MP3 файлы|*.mp3|WAV файлы|*.wav|FLAC файлы|*.flac|M4A файлы|*.m4a|AAC файлы|*.aac|OGG файлы|*.ogg|Все файлы|*.*",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            foreach (var fileName in openFileDialog.FileNames)
            {
                var fileInfo = new FileInfo(fileName);
                var selectedFile = new SelectedFile
                {
                    FileName = fileInfo.Name,
                    FilePath = fileName,
                    FileSize = FormatFileSize(fileInfo.Length),
                    Status = "Готов к загрузке"
                };

                _selectedFiles.Add(selectedFile);
            }

            BtnUploadFiles.IsEnabled = _selectedFiles.Count > 0;
            BtnClearFiles.IsEnabled = _selectedFiles.Count > 0;
            TxtStatus.Text = $"Выбрано файлов: {_selectedFiles.Count}";
        }
    }

    private async void BtnUploadFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFiles.Count == 0)
        {
            TxtStatus.Text = "Нет файлов для загрузки";
            return;
        }

        try
        {
            UploadProgress.Visibility = Visibility.Visible;
            UploadProgress.Maximum = _selectedFiles.Count;
            UploadProgress.Value = 0;

            BtnUploadFiles.IsEnabled = false;
            BtnSelectFiles.IsEnabled = false;

            var uploadedCount = 0;
            var errorCount = 0;

            foreach (var selectedFile in _selectedFiles.Where(f => !f.IsUploaded))
            {
                try
                {
                    selectedFile.Status = "Загружается...";
                    
                    using var formData = new MultipartFormDataContent();
                    using var fileStream = File.OpenRead(selectedFile.FilePath);
                    using var fileContent = new StreamContent(fileStream);
                    
                    formData.Add(fileContent, "file", selectedFile.FileName);

                    var response = await HttpClient.PostAsync("http://localhost:5209/api/upload/audio", formData);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var trackDto = await response.Content.ReadFromJsonAsync<TrackDto>();
                        if (trackDto != null)
                        {
                            selectedFile.Status = "Загружен успешно";
                            selectedFile.IsUploaded = true;
                            uploadedCount++;
                            
                            // Добавляем трек в список
                            _tracks.Add(trackDto);
                        }
                    }
                    else
                    {
                        selectedFile.Status = $"Ошибка: {response.StatusCode}";
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    selectedFile.Status = $"Ошибка: {ex.Message}";
                    errorCount++;
                }

                UploadProgress.Value++;
            }

            TxtStatus.Text = $"Загрузка завершена. Успешно: {uploadedCount}, Ошибок: {errorCount}";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            UploadProgress.Visibility = Visibility.Collapsed;
            BtnUploadFiles.IsEnabled = _selectedFiles.Any(f => !f.IsUploaded);
            BtnSelectFiles.IsEnabled = true;
        }
    }

    private void BtnClearFiles_Click(object sender, RoutedEventArgs e)
    {
        _selectedFiles.Clear();
        BtnUploadFiles.IsEnabled = false;
        BtnClearFiles.IsEnabled = false;
        TxtStatus.Text = "Список файлов очищен";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}