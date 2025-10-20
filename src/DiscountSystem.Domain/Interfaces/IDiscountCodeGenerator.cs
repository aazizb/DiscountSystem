namespace DiscountSystem.Domain.Interfaces
{
    public interface IDiscountCodeGenerator
    {
        IEnumerable<string> GenerateUniqueCodes(int count, int length);

    }
}
