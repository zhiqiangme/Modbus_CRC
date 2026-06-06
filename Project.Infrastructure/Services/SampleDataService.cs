using Project.Core;
using Project.Models;

namespace Project.Infrastructure;

public sealed class SampleDataService : ISampleDataService
{
    public Task<IReadOnlyList<SampleTask>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SampleTask> tasks =
        [
            new()
            {
                Title = "Set up modules",
                Description = "Keep project boundaries clear so UI depends on ViewModels and implementations stay in Infrastructure.",
                Owner = "Architecture",
                Status = "Ready"
            },
            new()
            {
                Title = "Replace sample data",
                Description = "Swap this service for your real API, database, or device integration layer.",
                Owner = "Data",
                Status = "Planned"
            },
            new()
            {
                Title = "Extend navigation",
                Description = "Add more sections and page ViewModels without changing the shell window structure.",
                Owner = "UI",
                Status = "Idea"
            },
            new()
            {
                Title = "Wire preferences",
                Description = "Persist user options or workspace state through the preferences service.",
                Owner = "Settings",
                Status = "Ready"
            }
        ];

        return Task.FromResult(tasks);
    }
}
