using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class HistoryService : IHistoryService
{
    private readonly string _historyFilePath;
    private readonly ILoggerService? _logger;
    private readonly List<HistoryItem> _items = new();
    private readonly object _lock = new();

    public event EventHandler? HistoryChanged;

    public HistoryService(ILoggerService? logger = null, string? historyFilePath = null)
    {
        _logger = logger;
        _historyFilePath = historyFilePath ?? AppConstants.HistoryFilePath;
        LoadHistory();
    }

    public IReadOnlyList<HistoryItem> GetHistory()
    {
        lock (_lock)
        {
            return _items.OrderByDescending(x => x.DownloadDate).ToList().AsReadOnly();
        }
    }

    public void AddItem(HistoryItem item)
    {
        lock (_lock)
        {
            _items.Insert(0, item);
            SaveHistory();
        }
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveItem(Guid id)
    {
        lock (_lock)
        {
            var existing = _items.FirstOrDefault(x => x.Id == id);
            if (existing != null)
            {
                _items.Remove(existing);
                SaveHistory();
            }
        }
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _items.Clear();
            SaveHistory();
        }
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadHistory()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    var list = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (string.IsNullOrWhiteSpace(item.Platform))
                            {
                                item.Platform = "YouTube";
                            }
                        }

                        _items.Clear();
                        _items.AddRange(list);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao carregar arquivo de histórico.", ex);
            }
        }
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var tempFile = _historyFilePath + $".{Guid.NewGuid():N}.tmp";
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, _historyFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao salvar arquivo de histórico.", ex);
        }
    }
}
