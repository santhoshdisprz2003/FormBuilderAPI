using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.MongoModel;
using FormBuilderAPI.Repository;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FormBuilderAPITests.RepositoryTest
{
    public class FormRepositoryTest : IDisposable
    {
        private readonly Mock<IMongoCollection<Form>> _mockFormsCollection;
        private readonly Mock<MongoDbContext> _mockMongoContext;
        private readonly SQLDbContext _sqlContext;
        private readonly FormRepository _repository;

        public FormRepositoryTest()
        {
            _mockFormsCollection = new Mock<IMongoCollection<Form>>();
            _mockMongoContext = new Mock<MongoDbContext>();
            _sqlContext = CreateSqlContext();

            // Setup the virtual property
            _mockMongoContext.Setup(x => x.Forms).Returns(_mockFormsCollection.Object);
            _repository = new FormRepository(_mockMongoContext.Object, _sqlContext);
        }

        private SQLDbContext CreateSqlContext()
        {
            var options = new DbContextOptionsBuilder<SQLDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new SQLDbContext(options);
        }

        public void Dispose()
        {
            _sqlContext?.Dispose();
        }

        #region GetAllFormsAsync Tests

        [Fact]
        public async Task GetAllFormsAsync_AdminRole_ReturnsAllForms()
        {
            // Arrange
            var forms = new List<Form>
            {
                CreateTestForm("1", FormStatus.Draft),
                CreateTestForm("2", FormStatus.Published)
            };

            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(forms);
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            _mockFormsCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            // Act
            var result = await _repository.GetAllFormsAsync("Admin", 1, 10);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Forms.Count());
        }

        [Fact]
        public async Task GetAllFormsAsync_NonAdminRole_ReturnsOnlyPublishedForms()
        {
            // Arrange
            var forms = new List<Form>
            {
                CreateTestForm("1", FormStatus.Published)
            };

            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(forms);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            _mockFormsCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.GetAllFormsAsync("User", 1, 10);

            // Assert
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Forms);
            Assert.All(result.Forms, f => Assert.Equal(FormStatus.Published, f.Status));
        }

        [Fact]
        public async Task GetAllFormsAsync_WithSearch_ReturnsFilteredForms()
        {
            // Arrange
            var forms = new List<Form>
            {
                CreateTestForm("1", FormStatus.Published, "Test Form")
            };

            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(forms);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            _mockFormsCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.GetAllFormsAsync("Admin", 1, 10, "Test");

            // Assert
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Forms);
        }

        [Fact]
        public async Task GetAllFormsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var forms = new List<Form>
            {
                CreateTestForm("3", FormStatus.Published)
            };

            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(forms);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            _mockFormsCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            // Act
            var result = await _repository.GetAllFormsAsync("Admin", 2, 2);

            // Assert
            Assert.Equal(5, result.TotalCount);
        }

        #endregion

        #region GetFormByIdAsync Tests

        [Fact]
        public async Task GetFormByIdAsync_WithRole_AdminRole_ReturnsForm()
        {
            // Arrange
            var form = CreateTestForm("1", FormStatus.Draft);
            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(new List<Form> { form });
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetFormByIdAsync("1", "Admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.Id);
        }

        [Fact]
        public async Task GetFormByIdAsync_WithRole_NonAdmin_DraftForm_ReturnsNull()
        {
            // Arrange
            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(new List<Form>());
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetFormByIdAsync("1", "User");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFormByIdAsync_WithRole_NonAdmin_PublishedForm_ReturnsForm()
        {
            // Arrange
            var form = CreateTestForm("1", FormStatus.Published);
            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(new List<Form> { form });
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetFormByIdAsync("1", "User");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(FormStatus.Published, result.Status);
        }

        [Fact]
        public async Task GetFormByIdAsync_WithoutRole_ReturnsForm()
        {
            // Arrange
            var form = CreateTestForm("1", FormStatus.Draft);
            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(new List<Form> { form });
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetFormByIdAsync("1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.Id);
        }

        #endregion

        #region InsertFormAsync Tests

        [Fact]
        public async Task InsertFormAsync_ValidForm_ReturnsFormId()
        {
            // Arrange
            var form = CreateTestForm("1", FormStatus.Draft);
            _mockFormsCollection.Setup(x => x.InsertOneAsync(
                It.IsAny<Form>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _repository.InsertFormAsync(form);

            // Assert
            Assert.Equal("1", result);
            _mockFormsCollection.Verify(x => x.InsertOneAsync(
                It.IsAny<Form>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UpdateFormConfigAsync Tests

        [Fact]
        public async Task UpdateFormConfigAsync_ValidData_ReturnsTrue()
        {
            // Arrange
            var updateResult = new UpdateResult.Acknowledged(1, 1, null);
            _mockFormsCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _repository.UpdateFormConfigAsync("1", "New Title", "New Description");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFormConfigAsync_NoMatchingForm_ReturnsFalse()
        {
            // Arrange
            var updateResult = new UpdateResult.Acknowledged(0, 0, null);
            _mockFormsCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _repository.UpdateFormConfigAsync("999", "New Title", "New Description");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region UpdateFormLayoutAsync Tests

        [Fact]
        public async Task UpdateFormLayoutAsync_ValidLayout_ReturnsTrue()
        {
            // Arrange
            var layout = new FormLayout
            {
                HeaderCard = new FormHeaderCard { Title = "Header", Description = "Description" },
                Fields = new List<FormField>()
            };

            var updateResult = new UpdateResult.Acknowledged(1, 1, null);
            _mockFormsCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _repository.UpdateFormLayoutAsync("1", layout);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFormLayoutAsync_NoMatchingForm_ReturnsFalse()
        {
            // Arrange
            var layout = new FormLayout();
            var updateResult = new UpdateResult.Acknowledged(0, 0, null);
            _mockFormsCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _repository.UpdateFormLayoutAsync("999", layout);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region UpdateFormStatusAsync Tests

        [Fact]
        public async Task UpdateFormStatusAsync_ValidData_ReturnsTrue()
        {
            // Arrange
            var publishedAt = DateTime.UtcNow;
            var updateResult = new UpdateResult.Acknowledged(1, 1, null);
            _mockFormsCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _repository.UpdateFormStatusAsync("1", FormStatus.Published, publishedAt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFormStatusAsync_NoMatchingForm_ReturnsFalse()
        {
            // Arrange
            var publishedAt = DateTime.UtcNow;
            var updateResult = new UpdateResult.Acknowledged(0, 0, null);
            _mockFormsCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _repository.UpdateFormStatusAsync("999", FormStatus.Published, publishedAt);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region DeleteFormAsync Tests

        [Fact]
        public async Task DeleteFormAsync_ExistingForm_ReturnsTrue()
        {
            // Arrange
            var deleteResult = new DeleteResult.Acknowledged(1);
            _mockFormsCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _repository.DeleteFormAsync("1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFormAsync_NonExistingForm_ReturnsFalse()
        {
            // Arrange
            var deleteResult = new DeleteResult.Acknowledged(0);
            _mockFormsCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _repository.DeleteFormAsync("999");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Helper Methods

        private Form CreateTestForm(string id, FormStatus status, string title = "Test Form")
        {
            return new Form
            {
                Id = id,
                Config = new FormConfig
                {
                    Title = title,
                    Description = "Test Description"
                },
                Layout = new FormLayout
                {
                    HeaderCard = new FormHeaderCard
                    {
                        Title = "Header",
                        Description = "Header Description"
                    },
                    Fields = new List<FormField>
                    {
                        new FormField
                        {
                            Label = "Test Field",
                            Type = "text",
                            Required = true,
                            Order = 1
                        }
                    }
                },
                Status = status,
                CreatedBy = "test-user",
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
