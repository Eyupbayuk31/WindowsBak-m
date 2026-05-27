using BakimApp.Models;

namespace BakimApp.Services;

public interface ICleaningService
{
    Task<CleaningCategory> ScanCategoryAsync(CleaningCategoryType categoryType, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<CleaningResult> CleanCategoryAsync(CleaningCategoryType categoryType, IProgress<(int percent, string message)>? progress = null, CancellationToken cancellationToken = default);
    List<CleaningCategory> GetAllCategories();
}
