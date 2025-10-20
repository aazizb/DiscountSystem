using DiscountSystem.Domain.Interfaces;
using DiscountSystem.Domain.Models;
using DiscountSystem.Protos;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscountSystem.Services
{
    public class DiscountCodeService : IDiscountCodeService
    {
        private readonly IDiscountCodeRepository _repository;
        private readonly IDiscountCodeGenerator _codeGenerator;
        private readonly ILogger<DiscountCodeService> _logger;

        public DiscountCodeService(IDiscountCodeRepository repository, IDiscountCodeGenerator codeGenerator, ILogger<DiscountCodeService> logger)
        {
            _repository = repository;
            _codeGenerator = codeGenerator;
            _logger = logger;
        }
        public async Task<bool> GenerateDiscountCodesAsync(int count, int length, CancellationToken cancellationToken = default)
        {
            if (count <= 0 || length <= 0)
                return false;

            const int batchSize = 1000;
            const int maxRetries = 5;

            int remaining = count;

            while (remaining > 0)
            {
                int currentBatch = Math.Min(batchSize, remaining);
                var attempts = 0;
                bool saved = false;

                while (!saved && attempts < maxRetries)
                {
                    attempts++;

                    try
                    {
                        // Generate unique codes in memory
                        var codes = _codeGenerator.GenerateUniqueCodes(currentBatch, length).ToList();

                        // Filter out those that already exist in the database
                        var existingCodes = await _repository.FilterExistingDiscountCodesAsync(codes, cancellationToken);

                        if (existingCodes.Any())
                        {
                            // Remove duplicates and regenerate replacements
                            var duplicates = existingCodes.ToHashSet();
                            codes = codes.Where(c => !duplicates.Contains(c)).ToList();

                            var replacements = _codeGenerator.GenerateUniqueCodes(duplicates.Count, length).ToList();
                            codes.AddRange(replacements);
                        }

                        var entities = codes.Select(code => new DiscountCode
                        {
                            Code = code,
                            CreatedAt = DateTime.UtcNow,
                            IsUsed = false
                        }).ToList();

                        await _repository.AddRangeDiscountCodeAsync(entities);

                        saved = true;
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogWarning(ex, $"Collision or update error in batch attempt {attempts}");

                        // Clear tracked entities to avoid EF state conflicts

                        // Retry after regenerating next iteration
                    }
                }

                if (!saved)
                {
                    _logger.LogError($"Failed to save batch after {maxRetries} attempts");
                    return false;
                }

                remaining -= currentBatch;
            }

            return true;

        }

        public async Task<EnumUseDiscountCode> UseDiscountCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                return EnumUseDiscountCode.Invalid;

            try
            {
                // Existence and state check for clearer result codes
                var entity = await _repository.GetDiscountCodeAsync(code, cancellationToken);

                if (entity == null)
                    return EnumUseDiscountCode.Missing;

                if (entity.IsUsed)
                    return EnumUseDiscountCode.Used;

                int rowsAffected = await _repository.UpdateDiscountCodeCountAsync(code, cancellationToken);

                if (rowsAffected > 0)
                    // Success: dicount code atomically marked as used
                    return EnumUseDiscountCode.Success;

                // check again — another concurrent call may have used it
                var final = await _repository.GetDiscountCodeAsync(code, cancellationToken);

                if (final?.IsUsed == true)
                    return EnumUseDiscountCode.Used;

                // Unexpected case — code vanished or was modified
                return EnumUseDiscountCode.Invalid;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Defensive fallback — should rarely occur with EF 8+ ExecuteUpdateAsync
                return EnumUseDiscountCode.Used;
            }
            catch (OperationCanceledException)
            {
                // Cancellation support
                _logger.LogWarning($"UseDiscountCodeAsync operation cancelled for code {code}");
                return EnumUseDiscountCode.Invalid;
            }
            catch (Exception ex)
            {
                // Common errors logging
                _logger.LogError(ex, $"Error using discount code {code}");
                return EnumUseDiscountCode.Invalid;
            }
        }
    }

}
