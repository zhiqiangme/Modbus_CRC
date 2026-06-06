using Project.Models;

namespace Project.Core;

public interface IAppPreferencesService
{
    Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default);
}
