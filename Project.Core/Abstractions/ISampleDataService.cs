using Project.Models;

namespace Project.Core;

public interface ISampleDataService
{
    Task<IReadOnlyList<SampleTask>> GetTasksAsync(CancellationToken cancellationToken = default);
}
