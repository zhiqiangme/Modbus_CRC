using System.Collections.ObjectModel;
using Project.Core;
using Project.Models;

namespace Project.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly ISampleDataService _sampleDataService;

    private string _heading = "Reusable WPF MVVM Template";
    private string _summary = "Use this solution as the starting point for desktop apps that need layered projects, DI, and clear page-level ViewModels.";
    private int _totalItems;
    private DateTime _lastUpdatedAt;

    public DashboardViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ObservableCollection<SampleTask> RecentItems { get; } = new();

    public AsyncRelayCommand RefreshCommand { get; }

    public string Heading
    {
        get => _heading;
        set => SetProperty(ref _heading, value);
    }

    public string Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set => SetProperty(ref _totalItems, value);
    }

    public DateTime LastUpdatedAt
    {
        get => _lastUpdatedAt;
        set => SetProperty(ref _lastUpdatedAt, value);
    }

    public async Task LoadAsync()
    {
        var tasks = await _sampleDataService.GetTasksAsync();

        RecentItems.Clear();
        foreach (var task in tasks.Take(3))
        {
            RecentItems.Add(task);
        }

        TotalItems = tasks.Count;
        LastUpdatedAt = DateTime.Now;
    }
}
