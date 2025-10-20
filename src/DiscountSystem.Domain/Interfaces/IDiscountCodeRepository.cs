using DiscountSystem.Domain.Models;

namespace DiscountSystem.Domain.Interfaces
{
    public interface IDiscountCodeRepository
    {
        Task AddRangeDiscountCodeAsync(IEnumerable<DiscountCode> codes);
        Task<bool> TryUseDiscountCodeAsync(string code);
        Task<bool> DiscountCodeExistsAsync(string code);
        Task<DiscountCode?> GetDiscountCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<int> UpdateDiscountCodeCountAsync(string code, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> FilterExistingDiscountCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);

    }
}
