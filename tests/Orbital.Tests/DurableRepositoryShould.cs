using Microsoft.Azure.Cosmos;
using Moq;
using Orbital.Durability;
using Orbital.Interfaces;
using Polly;

namespace Orbital.Tests;

public abstract class DurableRepositoryShould
{
    private readonly Mock<IRepository<TestDocument, ContainerAccessorStub>> _innerRepository;
    private readonly Mock<IDurabilityPolicyProvider> _durabilityPolicyProvider;

    private readonly TestDocument _testDocument = new("user");
    private readonly PartitionKey _partitionKey;

    protected DurableRepositoryShould()
    {
        _innerRepository = new Mock<IRepository<TestDocument, ContainerAccessorStub>>();
        _durabilityPolicyProvider = new Mock<IDurabilityPolicyProvider>();
        _partitionKey = new PartitionKey(_testDocument.Id);

        _durabilityPolicyProvider
            .Setup(mock =>
                       mock.GetPolicy(It.IsAny<string>()))
            .Returns(ResiliencePipeline.Empty)
            .Verifiable();
    }

    private DurableRepository<TestDocument, ContainerAccessorStub> CreateSut() => new(
        _innerRepository.Object,
        _durabilityPolicyProvider.Object
    );

    public class ForwardCallsToInnerRepository : DurableRepositoryShould
    {
        [Fact]
        public async Task GetAsync()
        {
            // arrange
            _innerRepository
                .Setup(
                    mock => mock.GetAsync(
                        _testDocument.Id,
                        _partitionKey,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(_testDocument)
                .Verifiable(Times.Once);

            var sut = CreateSut();

            // act
            var result = await sut.GetAsync(_testDocument.Id, _partitionKey);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result, _testDocument);

            _durabilityPolicyProvider.Verify();
            _innerRepository.Verify();
        }

        [Fact]
        public async Task CreateAsync()
        {
            // arrange
            _innerRepository
                .Setup(
                    mock => mock.CreateAsync(
                        _testDocument,
                        _partitionKey,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(_testDocument)
                .Verifiable();

            var sut = CreateSut();

            // act
            var result = await sut.CreateAsync(_testDocument, _partitionKey);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result, _testDocument);

            _durabilityPolicyProvider.Verify();
            _innerRepository.Verify();
        }

        [Fact]
        public async Task UpsertAsync()
        {
            // arrange
            _innerRepository
                .Setup(
                    mock => mock.UpsertAsync(
                        _testDocument,
                        _partitionKey,
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(_testDocument)
                .Verifiable();

            var sut = CreateSut();

            // act
            var result = await sut.UpsertAsync(
                _testDocument,
                _partitionKey,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            );

            // assert
            Assert.NotNull(result);
            Assert.Equal(result, _testDocument);

            _innerRepository.Verify();
            _durabilityPolicyProvider.Verify();
        }

        [Fact]
        public async Task DeleteAsync()
        {
            // arrange
            _innerRepository
                .Setup(
                    mock => mock.DeleteAsync(
                        _testDocument.Id,
                        _partitionKey,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(true)
                .Verifiable();

            var sut = CreateSut();

            // act
            var result = await sut.DeleteAsync(_testDocument.Id, _partitionKey);

            // assert
            Assert.True(result);

            _innerRepository.Verify();
            _durabilityPolicyProvider.Verify();
        }
    }
}