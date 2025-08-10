using E7GEZLY_API.Application.Features.Location.Queries.GetGovernorates;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace E7GEZLY_API.Tests.Unit.Application
{
    /// <summary>
    /// Unit tests for GetGovernoratesHandler following Clean Architecture patterns
    /// </summary>
    public class GetGovernoratesHandlerTests
    {
        private readonly Mock<ILocationRepository> _mockLocationRepository;
        private readonly Mock<ILogger<GetGovernoratesHandler>> _mockLogger;
        private readonly GetGovernoratesHandler _handler;

        public GetGovernoratesHandlerTests()
        {
            _mockLocationRepository = new Mock<ILocationRepository>();
            _mockLogger = new Mock<ILogger<GetGovernoratesHandler>>();
            _handler = new GetGovernoratesHandler(_mockLocationRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WhenGovernoratesExist_ShouldReturnSuccessResult()
        {
            // Arrange
            var governorates = new List<Governorate>
            {
                Governorate.CreateExisting(Guid.NewGuid(), 1, "Cairo", "القاهرة", DateTime.UtcNow, DateTime.UtcNow),
                Governorate.CreateExisting(Guid.NewGuid(), 2, "Alexandria", "الإسكندرية", DateTime.UtcNow, DateTime.UtcNow)
            };

            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(governorates);

            var query = new GetGovernoratesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            var governoratesList = result.Data.ToList();
            governoratesList[0].NameEn.Should().Be("Cairo");
            governoratesList[0].NameAr.Should().Be("القاهرة");
            governoratesList[1].NameEn.Should().Be("Alexandria");
            governoratesList[1].NameAr.Should().Be("الإسكندرية");

            // Verify repository was called
            _mockLocationRepository.Verify(
                x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenNoGovernoratesExist_ShouldReturnSuccessWithEmptyList()
        {
            // Arrange
            var emptyGovernorates = new List<Governorate>();

            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyGovernorates);

            var query = new GetGovernoratesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailureResult()
        {
            // Arrange
            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var query = new GetGovernoratesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to fetch governorates");

            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error occurred while fetching governorates")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenCancellationRequested_ShouldCancelOperation()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var query = new GetGovernoratesQuery();

            // Act
            var result = await _handler.Handle(query, cts.Token);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to fetch governorates");
        }

        [Fact]
        public async Task Handle_ShouldLogInformationMessages()
        {
            // Arrange
            var governorates = new List<Governorate>
            {
                Governorate.CreateExisting(Guid.NewGuid(), 1, "Cairo", "القاهرة", DateTime.UtcNow, DateTime.UtcNow)
            };

            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(governorates);

            var query = new GetGovernoratesQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            // Verify start logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching all governorates")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            // Verify completion logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully fetched") && v.ToString().Contains("governorates")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}