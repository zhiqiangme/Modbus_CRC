using System.Collections.ObjectModel;
using Project.Core;
using Project.Models;

namespace Project.ViewModels;

public sealed class ItemsViewModel : ObservableObject
{
    private readonly ISampleDataService _sampleDataService;
    private readonly List<SampleTask> _allItems = [];

    private string _searchText = string.Empty;
    private string _emptyStateMessage = "Sample items will appear here after loading.";

    public ItemsViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ObservableCollection<SampleTask> FilteredItems { get; } = new();

    public AsyncRelayCommand RefreshCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string EmptyStateMessage
    {
        get => _emptyStateMessage;
        set => SetProperty(ref _emptyStateMessage, value);
    }

    public async Task LoadAsync()
    {
        var items = await _sampleDataService.GetTasksAsync();

        _allItems.Clear();
        _allItems.AddRange(items);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = SearchText.Trim();
        var matches = string.IsNullOrWhiteSpace(keyword)
            ? _allItems
            : _allItems
                .Where(item =>
                    item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    item.Owner.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    item.Status.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

        FilteredItems.Clear();
        foreach (var item in matches)
        {
            FilteredItems.Add(item);
        }

        EmptyStateMessage = string.IsNullOrWhiteSpace(keyword)
            ? "Sample items will appear here after loading."
            : $"No items match \"{keyword}\".";
    }
}
