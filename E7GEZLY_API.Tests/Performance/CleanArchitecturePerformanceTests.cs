using E7GEZLY_API.Application.Features.Location.Queries.GetGovernorates;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Tests.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;

namespace E7GEZLY_API.Tests.Performance
{
    /// <summary>
    /// Performance tests to validate that Clean Architecture implementation
    /// meets the E7GEZLY performance requirements (sub-200ms response times)
    /// </summary>
    [Trait("Category", "Performance")]
    public class CleanArchitecturePerformanceTests
    {
        private readonly Mock<ILocationRepository> _mockLocationRepository;
        private readonly Mock<ILogger<GetGovernoratesHandler>> _mockLogger;

        public CleanArchitecturePerformanceTests()
        {
            _mockLocationRepository = new Mock<ILocationRepository>();
            _mockLogger = new Mock<ILogger<GetGovernoratesHandler>>();
        }

        [Fact]
        public async Task GetGovernoratesHandler_WithSmallDataset_ShouldCompleteUnder50ms()
        {
            // Arrange
            var governorates = GenerateTestGovernorates(10); // Small dataset
            
            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(governorates);

            var handler = new GetGovernoratesHandler(_mockLocationRepository.Object, _mockLogger.Object);
            var query = new GetGovernoratesQuery();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await handler.Handle(query, CancellationToken.None);
            stopwatch.Stop();

            // Assert
            result.IsSuccess.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, 
                "Handler should complete in under 50ms for small datasets");
        }

        [Fact]
        public async Task GetGovernoratesHandler_WithLargeDataset_ShouldCompleteUnder100ms()
        {
            // Arrange
            var governorates = GenerateTestGovernorates(100); // Larger dataset (all Egypt governorates would be ~27)
            
            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(governorates);

            var handler = new GetGovernoratesHandler(_mockLocationRepository.Object, _mockLogger.Object);
            var query = new GetGovernoratesQuery();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await handler.Handle(query, CancellationToken.None);
            stopwatch.Stop();

            // Assert
            result.IsSuccess.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
                "Handler should complete in under 100ms even for large datasets");
        }

        [Fact]
        public async Task GetGovernoratesHandler_MultipleSequentialCalls_ShouldMaintainPerformance()
        {
            // Arrange
            var governorates = GenerateTestGovernorates(27); // Realistic Egypt governorate count
            
            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(governorates);

            var handler = new GetGovernoratesHandler(_mockLocationRepository.Object, _mockLogger.Object);
            var query = new GetGovernoratesQuery();

            var executionTimes = new List<long>();

            // Act - Multiple sequential calls to test performance consistency
            for (int i = 0; i < 10; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await handler.Handle(query, CancellationToken.None);
                stopwatch.Stop();

                result.IsSuccess.Should().BeTrue();
                executionTimes.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert
            var averageTime = executionTimes.Average();
            var maxTime = executionTimes.Max();

            averageTime.Should().BeLessThan(50, 
                "Average execution time should be under 50ms");
            
            maxTime.Should().BeLessThan(100, 
                "Maximum execution time should be under 100ms");

            // Verify consistent performance (no dramatic spikes)
            var standardDeviation = CalculateStandardDeviation(executionTimes);
            standardDeviation.Should().BeLessThan(averageTime * 0.5, 
                "Performance should be consistent across multiple calls");
        }

        [Fact]
        public async Task GetGovernoratesHandler_ConcurrentCalls_ShouldHandleLoad()
        {
            // Arrange
            var governorates = GenerateTestGovernorates(27);
            
            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(governorates);

            var handler = new GetGovernoratesHandler(_mockLocationRepository.Object, _mockLogger.Object);
            var query = new GetGovernoratesQuery();

            const int concurrentCalls = 20;
            var tasks = new List<Task<(bool Success, long ElapsedMs)>>();

            // Act - Concurrent calls to simulate load
            for (int i = 0; i < concurrentCalls; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    var result = await handler.Handle(query, CancellationToken.None);
                    stopwatch.Stop();
                    return (result.IsSuccess, stopwatch.ElapsedMilliseconds);
                }));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(concurrentCalls);
            results.Should().OnlyContain(r => r.Success, "All concurrent calls should succeed");

            var executionTimes = results.Select(r => r.ElapsedMs).ToList();
            var averageTime = executionTimes.Average();
            var maxTime = executionTimes.Max();

            averageTime.Should().BeLessThan(100, 
                "Average concurrent execution time should be under 100ms");
            
            maxTime.Should().BeLessThan(200, 
                "Maximum concurrent execution time should be under 200ms");
        }

        [Fact]
        public async Task MediatR_Pipeline_Overhead_ShouldBeMinimal()
        {
            // Arrange
            var governorates = GenerateTestGovernorates(1); // Minimal data to focus on pipeline overhead
            
            _mockLocationRepository
                .Setup(x => x.GetGovernoratesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(governorates);

            var handler = new GetGovernoratesHandler(_mockLocationRepository.Object, _mockLogger.Object);
            var query = new GetGovernoratesQuery();

            // Act - Measure handler execution without full MediatR pipeline
            var stopwatch = Stopwatch.StartNew();
            await handler.Handle(query, CancellationToken.None);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10, 
                "MediatR handler overhead should be minimal (under 10ms) for simple operations");
        }

        [Fact]
        public void Domain_Entity_Creation_ShouldBePerformant()
        {
            // Arrange
            const int entityCount = 1000;
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            var venues = new List<Venue>();
            
            for (int i = 0; i < entityCount; i++)
            {
                var venue = Venue.Create(
                    $"Test Venue {i}",
                    VenueType.FootballCourt,
                    "test@example.com",
                    VenueFeatures.WiFi
                );
                venues.Add(venue);
            }
            
            stopwatch.Stop();

            // Assert
            venues.Should().HaveCount(entityCount);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
                $"Creating {entityCount} domain entities should complete in under 100ms");

            var averageTimePerEntity = (double)stopwatch.ElapsedMilliseconds / entityCount;
            averageTimePerEntity.Should().BeLessThan(0.1, 
                "Each entity creation should take less than 0.1ms on average");
        }

        [Fact]
        public void ApplicationResult_Pattern_ShouldNotImpactPerformance()
        {
            // Arrange
            const int operationCount = 10000;
            var testData = Enumerable.Range(1, 100).ToList();

            // Act - Test ApplicationResult creation performance
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < operationCount; i++)
            {
                var result = E7GEZLY_API.Application.Common.Models.ApplicationResult<List<int>>.Success(testData);
                _ = result.IsSuccess;
                _ = result.Data;
            }
            
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
                $"Creating {operationCount} ApplicationResult objects should complete in under 100ms");
        }

        /// <summary>
        /// Generate test governorates for performance testing
        /// </summary>
        private static IEnumerable<Governorate> GenerateTestGovernorates(int count)
        {
            var governorates = new List<Governorate>();
            
            for (int i = 1; i <= count; i++)
            {
                governorates.Add(Governorate.CreateExisting(
                    Guid.NewGuid(),
                    i,
                    $"Governorate {i}",
                    $"محافظة {i}",
                    DateTime.UtcNow,
                    DateTime.UtcNow
                ));
            }
            
            return governorates;
        }

        /// <summary>
        /// Calculate standard deviation of execution times
        /// </summary>
        private static double CalculateStandardDeviation(IEnumerable<long> values)
        {
            var valuesArray = values.ToArray();
            var average = valuesArray.Average();
            var sumOfSquares = valuesArray.Sum(v => Math.Pow(v - average, 2));
            return Math.Sqrt(sumOfSquares / valuesArray.Length);
        }
    }
}