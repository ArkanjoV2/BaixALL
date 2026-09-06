using System;
using System.Collections.Generic;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface IHistoryService
{
    IReadOnlyList<HistoryItem> GetHistory();
    void AddItem(HistoryItem item);
    void RemoveItem(Guid id);
    void ClearHistory();
    event EventHandler? HistoryChanged;
}
