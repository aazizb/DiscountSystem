using System.Security.Cryptography;

using DiscountSystem.Domain.Interfaces;

namespace DiscountSystem.Services
{
    public class DiscountCodeGenerator : IDiscountCodeGenerator
    {
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public IEnumerable<string> GenerateUniqueCodes(int count, int length)
        {
            var codes = new HashSet<string>();

            while (codes.Count < count)
            {
                var code = GenerateCode(length);
                codes.Add(code);
            }

            return codes;
        }

        private static string GenerateCode(int length)
        {
            Span<char> chars = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = Characters[RandomNumberGenerator.GetInt32(Characters.Length)];
            }
            return new string(chars);
        }
    }

}
