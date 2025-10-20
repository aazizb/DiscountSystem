using DiscountSystem.Domain.Interfaces;
using DiscountSystem.Domain.Models;

using Microsoft.EntityFrameworkCore;

namespace DiscountSystem.Infrastructure.Repositories
{
    public class DiscountCodeRepository : IDiscountCodeRepository
    {
        private readonly DiscountCodeDbContext _context;

        public DiscountCodeRepository(DiscountCodeDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeDiscountCodeAsync(IEnumerable<DiscountCode> codes)
        {
            _context.DiscountCodes.AddRange(codes);
            await _context.SaveChangesAsync();
        }

        public Task<bool> DiscountCodeExistsAsync(string code)
        {
            return _context.DiscountCodes.AnyAsync(dc => dc.Code == code);
        }

        public async Task<IEnumerable<string>> FilterExistingDiscountCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {

            return await _context.DiscountCodes
                            .Where(c => codes.Contains(c.Code))
                            .Select(c => c.Code)
                            .ToListAsync(cancellationToken);
        }

        public async Task<DiscountCode?> GetDiscountCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.DiscountCodes
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Code == code,
                cancellationToken);
        }

        public async Task<bool> TryUseDiscountCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            // If transient concurrency is expected (timing overlaps), you can retry once
            for (int attempt = 0; attempt < 2; attempt++)
            {

                var entry = await _context.DiscountCodes
                    .SingleOrDefaultAsync(c => c.Code == code);

                if (entry is null || entry.IsUsed)
                    return false;

                // Clear tracked entity to avoid EF state conflicts
                DetachLocal(o => o.Id == entry.Id);

                entry.IsUsed = true;
                entry.UsedAt = DateTime.UtcNow;
                try
                {
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another thread updated it first — retry once
                }

            }
            return false;
        }

        public async Task<int> UpdateDiscountCodeCountAsync(string code, CancellationToken cancellationToken = default)
        {

            // Atomic concurrency-safe update, ensures only one concurrent caller can mark the code as used.
            var count = await _context.DiscountCodes
                .Where(o => o.Code == code && !o.IsUsed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.IsUsed, true)
                    .SetProperty(c => c.UsedAt, DateTime.UtcNow),
                    cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return count;
        }

        /// <summary>
        /// Detach a local tracked entity that matches a condition
        /// </summary>
        private void DetachLocal(Func<DiscountCode, bool> predicate)
        {
            var localEntity = _context.Set<DiscountCode>()
                                      .Local
                                      .FirstOrDefault(predicate);
            if (localEntity != null)
                _context.Entry(localEntity).State = EntityState.Detached;
        }
    }

}
