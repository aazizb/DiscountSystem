using DiscountSystem.Domain.Interfaces;
using DiscountSystem.Protos;

using Grpc.Core;

namespace DiscountSystem.Server.Services
{
    public class DiscountCodeServices : DiscountService.DiscountServiceBase
    {
        private readonly IDiscountCodeService _service;
        private readonly ILogger<DiscountCodeServices> _logger;
        private static readonly int MaxNumCodes = DiscountCodeConstants.MaxNumCodes;
        private static readonly int[] AllowedCodeLengths = DiscountCodeConstants.AllowedCodeLengths;
        public DiscountCodeServices(IDiscountCodeService service, ILogger<DiscountCodeServices> logger)
        {
            _service = service;
            _logger = logger;
        }
        public override async Task<GenerateDiscountCodeResponse> GenerateDiscountCodes(GenerateDiscountCodeRequest request, ServerCallContext context)
        {
            var validation = ValidateRequest(request);

            if (!validation.Result)
                return validation;

            if (request.Count > DiscountCodeConstants.MaxNumCodes)
            {
                _logger.LogWarning($"Requested count {request.Count} exceeds maximum. Limiting to {MaxNumCodes}.");
                request.Count = MaxNumCodes;
            }

            var result = await _service.GenerateDiscountCodesAsync(request.Count, request.Length, context.CancellationToken);

            return new GenerateDiscountCodeResponse { Result = result };
        }
        public override async Task<UseDiscountCodeResponse> UseDiscountCode(UseDiscountCodeRequest request, ServerCallContext context)
        {
            var result = await _service.UseDiscountCodeAsync(request.Code);
            return new UseDiscountCodeResponse { Result = result };
        }
        private static GenerateDiscountCodeResponse ValidateRequest(GenerateDiscountCodeRequest request)
        {
            if (request.Count <= 0 || request.Length <= 0)
                return new GenerateDiscountCodeResponse { Result = false, Message = "Number of codes and code length must be greater than zero." };

            if (!AllowedCodeLengths.Contains(request.Length))
                return new GenerateDiscountCodeResponse { Result = false, Message = $"Code length must be one of: {string.Join(", ", AllowedCodeLengths)}" };

            return new GenerateDiscountCodeResponse { Result = true };
        }
        public static class DiscountCodeConstants
        {
            public static readonly int MaxNumCodes = 2000;
            public const int MinCodeLength = 7;
            public const int MaxCodeLength = 8;
            public static readonly int[] AllowedCodeLengths = { MinCodeLength, MaxCodeLength };
        }
    }

}
