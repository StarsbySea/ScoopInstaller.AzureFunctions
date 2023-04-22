using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ScoopSearch.Indexer.Configuration;
using ScoopSearch.Indexer.Data;
using ScoopSearch.Indexer.Function;
using ScoopSearch.Indexer.GitHub;
using ScoopSearch.Indexer.Indexer;
using ScoopSearch.Indexer.Tests.Helpers;
using Xunit.Abstractions;

namespace ScoopSearch.Indexer.Tests.Function;

public class DispatchBucketsCrawlerTests : IClassFixture<HostFixture>
{
    private readonly XUnitLogger<BucketCrawler> _logger;
    private readonly Mock<IIndexer> _indexerMock;
    private readonly DispatchBucketsCrawler _sut;

    public DispatchBucketsCrawlerTests(HostFixture hostFixture, ITestOutputHelper testOutputHelper)
    {
        hostFixture.Configure(testOutputHelper);

        _logger = new XUnitLogger<BucketCrawler>(testOutputHelper);
        _indexerMock = new Mock<IIndexer>();
        _sut = new DispatchBucketsCrawler(
            hostFixture.Host.Services.GetRequiredService<IGitHubClient>(),
            _indexerMock.Object,
            hostFixture.Host.Services.GetRequiredService<IOptions<BucketsOptions>>());
    }

    [Fact]
    public async void Run_ManifestMetadataUpdatedFromBucket_Succeeds()
    {
        // Arrange
        var timerInfo = new TimerInfo(new ConstantSchedule(TimeSpan.Zero), new ScheduleStatus());
        var asyncCollectorMock = new Mock<IAsyncCollector<QueueItem>>();
        var cancellationToken = new CancellationToken();
        var expectedOfficialBucketsCount = 10;
        var expectedAtLeastBucketsCount = 1400;

        // Act
        await _sut.Run(timerInfo, asyncCollectorMock.Object, _logger, cancellationToken);

        // Assert
        _indexerMock.Verify(_ => _.GetBucketsAsync(cancellationToken));
        _indexerMock.VerifyNoOtherCalls();;
        asyncCollectorMock.Verify(_ => _.AddAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()), Times.AtLeast(expectedAtLeastBucketsCount));
        asyncCollectorMock.VerifyNoOtherCalls();

        _logger.Should().Log(LogLevel.Information, "Retrieving buckets from sources");
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, $"Found {expectedOfficialBucketsCount} official buckets.+"));
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, @"Found \d{4} buckets on GitHub\."));
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, @"Found \d+ buckets to ignore \(settings\.json\)\."));
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, @"Found \d+ buckets to ignore from external list.+"));
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, @"Found \d+ buckets to add \(settings\.json\)\."));
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, @"Found \d+ buckets to add from external list.+"));
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, @"\d+ buckets to remove from the index\."));
        _logger.Should().Log(LogLevel.Information, _ => Regex.IsMatch(_, @"Adding \d{4} buckets for indexing\."));
        _logger.Should().Log(LogLevel.Debug, _ => _.StartsWith("Adding bucket"), Times.AtLeast(expectedAtLeastBucketsCount));
        _logger.Should().Log(LogLevel.Debug, _ => _.StartsWith("Adding bucket 'https://github.com/ScoopInstaller/Main'"));
        _logger.Should().Log(LogLevel.Debug, _ => _.StartsWith("Adding bucket 'https://github.com/ScoopInstaller/Extras'"));
    }
}
