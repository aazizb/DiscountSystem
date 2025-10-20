using Microsoft.Extensions.Logging;

using Moq;

using static global::DiscountSystem.Server.Services.DiscountCodeServices;

namespace DiscountSystem.Test
{

    // ====================================================================
    //              MOCK/ASSUMED DEFINITIONS FOR TESTING 
    // ====================================================================

    // 1. Mock gRPC Base Class and Context
    public abstract class DiscountServiceBase
    {
        // These methods are abstract in the base and implemented in DiscountCodeServices
        public abstract Task<GenerateDiscountCodeResponse> GenerateDiscountCodes(GenerateDiscountCodeRequest request, ServerCallContext context);
        public abstract Task<UseDiscountCodeResponse> UseDiscountCode(UseDiscountCodeRequest request, ServerCallContext context);
    }

    public class ServerCallContext
    {
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
    }

    // 2. Mock DTOs and enum
    public class GenerateDiscountCodeRequest
    {
        public int Count { get; set; }
        public int Length { get; set; }
    }

    public class GenerateDiscountCodeResponse
    {
        public bool Result { get; set; }
        public string? Message { get; set; }
    }

    public class UseDiscountCodeRequest
    {
        public string? Code { get; set; }
    }

    public class UseDiscountCodeResponse
    {
        public EnumUseDiscountCode Result { get; set; }
    }

    public enum EnumUseDiscountCode
    {
        Success = 0,
        Missing = 1,
        Used = 2,
        Invalid = 3
    }

    // 3. Mock service interface
    public interface IDiscountCodeService
    {
        Task<bool> GenerateDiscountCodesAsync(int count, int length, CancellationToken cancellationToken);
        Task<EnumUseDiscountCode> UseDiscountCodeAsync(string code);
    }

    // ====================================================================
    //                          CLASS UNDER TEST
    // ====================================================================

    public class DiscountCodeServices : DiscountServiceBase
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
            var result = await _service.UseDiscountCodeAsync(request.Code!);
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

    // ====================================================================
    //                          XUNIT UNIT TESTS
    // ====================================================================

    public class DiscountCodeServicesTests
    {
        private readonly Mock<IDiscountCodeService> _mockService;
        private readonly Mock<ILogger<DiscountCodeServices>> _mockLogger;
        private readonly DiscountCodeServices _sut; 
        private readonly ServerCallContext _context;

        public DiscountCodeServicesTests()
        {
            // Setup mocks
            _mockService = new Mock<IDiscountCodeService>();
            _mockLogger = new Mock<ILogger<DiscountCodeServices>>();
            _context = new ServerCallContext { CancellationToken = CancellationToken.None };

            // Initialize the class under test with the mocks
            _sut = new DiscountCodeServices(_mockService.Object, _mockLogger.Object);
        }

        // --- GenerateDiscountCodes Tests ---

        [Fact]
        public async Task GenerateDiscountCodes_ValidRequest_ReturnsSuccessAndCallsService()
        {
            // Arrange
            var request = new GenerateDiscountCodeRequest { Count = 5, Length = 8 };

            // Mock the dependency to return true (successful generation)
            _mockService.Setup(s => s.GenerateDiscountCodesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var response = await _sut.GenerateDiscountCodes(request, _context);

            // Assert
            Assert.True(response.Result);
            Assert.Null(response.Message);

            // Verify the underlying service method was called with the correct parameters
            _mockService.Verify(s => s.GenerateDiscountCodesAsync(
                5,
                8,
                _context.CancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task GenerateDiscountCodes_CountExceedsMax_CallsServiceWithMaxCount()
        {
            // Arrange
            var request = new GenerateDiscountCodeRequest { Count = DiscountCodeConstants.MaxNumCodes + 10, Length = 8 };

            // Mock the dependency to return true
            _mockService.Setup(s => s.GenerateDiscountCodesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var response = await _sut.GenerateDiscountCodes(request, _context);

            // Assert
            Assert.True(response.Result);

            // Verify the service was called with the capped MaxNumCodes
            _mockService.Verify(s => s.GenerateDiscountCodesAsync(
                DiscountCodeConstants.MaxNumCodes,
                8,
                _context.CancellationToken),
                Times.Once);
        }

        [Theory]
        [InlineData(0, 8)]      // Count <= 0
        [InlineData(5, 0)]      // Length <= 0
        [InlineData(-1, 8)]     // Count < 0
        [InlineData(5, -1)]     // Length < 0
        public async Task GenerateDiscountCodes_InvalidCountOrLength_ReturnsValidationError(int count, int length)
        {
            // Arrange
            var request = new GenerateDiscountCodeRequest { Count = count, Length = length };

            // Act
            var response = await _sut.GenerateDiscountCodes(request, _context);

            // Assert
            Assert.False(response.Result);
            Assert.Contains("must be greater than zero", response.Message);

            // Ensure service was never called due to validation failure
            _mockService.Verify(s => s.GenerateDiscountCodesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GenerateDiscountCodes_InvalidLength_ReturnsValidationError()
        {
            // Arrange
            var request = new GenerateDiscountCodeRequest { Count = 5, Length = 9 };

            // Act
            var response = await _sut.GenerateDiscountCodes(request, _context);

            // Assert
            Assert.False(response.Result);
            Assert.Contains("must be one of", response.Message);

            // Ensure service was never called
            _mockService.Verify(s => s.GenerateDiscountCodesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // --- UseDiscountCode Tests ---

        [Fact]
        public async Task UseDiscountCode_CodeIsSuccessful_ReturnsSuccessResult()
        {
            // Arrange
            var request = new UseDiscountCodeRequest { Code = "VALIDCODE" };

            // Mock the dependency to return success
            _mockService.Setup(s => s.UseDiscountCodeAsync(request.Code))
                .ReturnsAsync(EnumUseDiscountCode.Success);

            // Act
            var response = await _sut.UseDiscountCode(request, _context);

            // Assert
            Assert.Equal(EnumUseDiscountCode.Success, response.Result);

            // Verify the underlying service method was called
            _mockService.Verify(s => s.UseDiscountCodeAsync(request.Code), Times.Once);
        }

        [Theory]
        [InlineData("MISSING", EnumUseDiscountCode.Missing)]
        [InlineData("USED", EnumUseDiscountCode.Used)]
        [InlineData("INVALID", EnumUseDiscountCode.Invalid)]
        [InlineData("SUCCESS", EnumUseDiscountCode.Success)]
        public async Task UseDiscountCode_CodeFails_ReturnsFailureResult(string code, EnumUseDiscountCode expectedResult)
        {
            // Arrange
            var request = new UseDiscountCodeRequest { Code = code };

            // Mock the dependency to return various failure states
            _mockService.Setup(s => s.UseDiscountCodeAsync(request.Code))
                .ReturnsAsync(expectedResult);

            // Act
            var response = await _sut.UseDiscountCode(request, _context);

            // Assert
            Assert.Equal(expectedResult, response.Result);

            // Verify the underlying service method was called
            _mockService.Verify(s => s.UseDiscountCodeAsync(request.Code), Times.Once);
        }
    }

}
