using DiscountSystem.Protos;

namespace DiscountSystem.Domain.Interfaces
{
    public interface IDiscountCodeService
    {
        Task<bool> GenerateDiscountCodesAsync(int count, int length, CancellationToken cancellationToken = default);
        Task<EnumUseDiscountCode> UseDiscountCodeAsync(string code, CancellationToken cancellationToken = default);

    }
}
