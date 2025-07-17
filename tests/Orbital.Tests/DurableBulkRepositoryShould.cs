using Microsoft.Azure.Cosmos;
using Moq;
using Orbital.Durability;
using Orbital.Interfaces;
using Orbital.Models;
using Polly;

namespace Orbital.Tests;

public abstract class DurableBulkRepositoryShould
{
    private readonly Mock<IBulkRepository<TestDocument, ContainerAccessorStub>> _innerRepository;
    private readonly Mock<IDurabilityPolicyProvider> _durabilityPolicyProvider;

    private readonly TestDocument _testDocument = new("user");
    private readonly PartitionKey _partitionKey;

    protected DurableBulkRepositoryShould()
    {
        _innerRepository = new Mock<IBulkRepository<TestDocument, ContainerAccessorStub>>();
        _durabilityPolicyProvider = new Mock<IDurabilityPolicyProvider>();
        _partitionKey = new PartitionKey(_testDocument.Id);

        _durabilityPolicyProvider
            .Setup(mock =>
                       mock.GetPolicy(It.IsAny<string>()))
            .Returns(ResiliencePipeline.Empty)
            .Verifiable();
    }

    private DurableBulkRepository<TestDocument, ContainerAccessorStub> CreateSut() => new(
        _innerRepository.Object,
        _durabilityPolicyProvider.Object
    );

    public class ForwardCallsToInnerBulkRepository : DurableBulkRepositoryShould
    {
        [Fact]
        public async Task ReadPartitionAsync()
        {
            // arrange
            List<string> ids = [_testDocument.Id];

            _innerRepository
                .Setup(
                    mock => mock.ReadPartitionAsync(
                        ids,
                        _partitionKey,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync([_testDocument])
                .Verifiable();

            var sut = CreateSut();

            // act
            var result = await sut.ReadPartitionAsync(ids, _partitionKey);

            // assert
            Assert.Contains(result, testDocument => ids.Contains(testDocument.Id));

            _innerRepository.Verify();
            _durabilityPolicyProvider.Verify();
        }

        [Fact]
        public async Task BulKCreateAsync()
        {
            // arrange
            const double expectedResult = 10.0;
            List<TestDocument> testDocuments = [_testDocument];

            _innerRepository
                .Setup(
                    mock => mock.BulkCreateAsync(
                        testDocuments,
                        _partitionKey,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new BulkOperationResult<TestDocument>
                    {
                        Succeeded = [_testDocument],
                        TotalRequestUnits = expectedResult
                    }
                )
                .Verifiable();

            var sut = CreateSut();

            // act
            var result = await sut.BulkCreateAsync(testDocuments, _partitionKey);

            // assert
            Assert.Contains(result.Succeeded, testDocuments.Contains);
            Assert.Equal(expected: expectedResult, actual: result.TotalRequestUnits);

            _innerRepository.Verify();
            _durabilityPolicyProvider.Verify();
        }

        [Fact]
        public async Task BulkUpsertAsync()
        {
            // arrange
            const double expectedResult = 10.0;
            List<TestDocument> testDocuments = [_testDocument];

            _innerRepository
                .Setup(
                    mock => mock.BulkUpsertAsync(
                        testDocuments,
                        _partitionKey,
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new BulkOperationResult<TestDocument>
                    {
                        Succeeded = [_testDocument],
                        TotalRequestUnits = expectedResult
                    }
                )
                .Verifiable();

            var sut = CreateSut();

            // act
            var result = await sut.BulkUpsertAsync(testDocuments, _partitionKey);

            // assert
            Assert.Contains(result.Succeeded, testDocuments.Contains);
            Assert.Equal(expected: expectedResult, actual: result.TotalRequestUnits);

            _innerRepository.Verify();
            _durabilityPolicyProvider.Verify();
        }

        [Fact]
        public async Task BulkDeleteAsync()
        {
            // arrange
            const double expectedResult = 10.0;
            List<string> ids = [_testDocument.Id];

            _innerRepository
                .Setup(
                    mock => mock.BulkDeleteAsync(
                        ids,
                        _partitionKey,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new BulkOperationResult<TestDocument>
                    {
                        TotalRequestUnits = expectedResult
                    }
                )
                .Verifiable();

            var sut = CreateSut();

            // act
            var result = await sut.BulkDeleteAsync(
                ids,
                _partitionKey
            );

            // assert
            Assert.True(result.IsSuccess);

            _innerRepository.Verify();
            _durabilityPolicyProvider.Verify();
        }
    }
}