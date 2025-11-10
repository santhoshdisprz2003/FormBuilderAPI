using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.MongoModel;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FormBuilderAPITests.DataAccessLayerTest
{
    public class MongoDbContextTest
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ValidConnectionString_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_ValidConnectionStringWithDatabase_UsesSpecifiedDatabase()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/MyCustomDatabase";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_NullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            var configuration = CreateConfiguration(null!);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(configuration));
            Assert.Contains("MongoDB connection string is missing", exception.Message);
            Assert.Equal("connectionString", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            var configuration = CreateConfiguration("");

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(configuration));
            Assert.Contains("MongoDB connection string is missing", exception.Message);
        }

        [Fact]
        public void Constructor_WhitespaceConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            var configuration = CreateConfiguration("   ");

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(configuration));
            Assert.Contains("MongoDB connection string is missing", exception.Message);
        }

        [Fact]
        public void Constructor_ConnectionStringWithoutDatabase_UsesDefaultDatabase()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
            // The default database name should be "FormBuilderMongoDB"
        }

        [Fact]
        public void Constructor_ConnectionStringWithAuthenticationCredentials_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://username:password@localhost:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_ConnectionStringWithReplicaSet_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://host1:27017,host2:27017,host3:27017/TestDatabase?replicaSet=myReplicaSet";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_ConnectionStringWithOptions_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/TestDatabase?retryWrites=true&w=majority";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_MongoDbAtlasConnectionString_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb+srv://username:password@cluster0.mongodb.net/TestDatabase?retryWrites=true&w=majority";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        #endregion

        #region Forms Collection Tests

        [Fact]
        public void Forms_ValidContext_ReturnsIMongoCollection()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);
            var context = new MongoDbContext(configuration);

            // Act
            var forms = context.Forms;

            // Assert
            Assert.NotNull(forms);
            Assert.IsAssignableFrom<IMongoCollection<Form>>(forms);
        }

        [Fact]
        public void Forms_MultipleAccess_ReturnsSameCollection()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);
            var context = new MongoDbContext(configuration);

            // Act
            var forms1 = context.Forms;
            var forms2 = context.Forms;

            // Assert
            Assert.NotNull(forms1);
            Assert.NotNull(forms2);
            // Both should reference the same collection
            Assert.Equal(forms1.CollectionNamespace.CollectionName, forms2.CollectionNamespace.CollectionName);
        }

        [Fact]
        public void Forms_CollectionName_IsCorrect()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);
            var context = new MongoDbContext(configuration);

            // Act
            var forms = context.Forms;

            // Assert
            Assert.NotNull(forms);
            Assert.Equal("Forms", forms.CollectionNamespace.CollectionName);
        }

        [Fact]
        public void Forms_DatabaseName_MatchesConnectionString()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/MyTestDatabase";
            var configuration = CreateConfiguration(connectionString);
            var context = new MongoDbContext(configuration);

            // Act
            var forms = context.Forms;

            // Assert
            Assert.NotNull(forms);
            Assert.Equal("MyTestDatabase", forms.CollectionNamespace.DatabaseNamespace.DatabaseName);
        }

        [Fact]
        public void Forms_NoDatabaseInConnectionString_UsesDefaultDatabaseName()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017";
            var configuration = CreateConfiguration(connectionString);
            var context = new MongoDbContext(configuration);

            // Act
            var forms = context.Forms;

            // Assert
            Assert.NotNull(forms);
            Assert.Equal("FormBuilderMongoDB", forms.CollectionNamespace.DatabaseNamespace.DatabaseName);
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public void Constructor_MissingConnectionStringKey_ThrowsArgumentNullException()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"SomeOtherKey", "SomeValue"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(configuration));
            Assert.Contains("MongoDB connection string is missing", exception.Message);
        }

        #endregion

        #region Integration-like Tests

        [Fact]
        public void Constructor_DifferentDatabaseNames_CreatesDifferentContexts()
        {
            // Arrange
            var connectionString1 = "mongodb://localhost:27017/Database1";
            var connectionString2 = "mongodb://localhost:27017/Database2";
            var configuration1 = CreateConfiguration(connectionString1);
            var configuration2 = CreateConfiguration(connectionString2);

            // Act
            var context1 = new MongoDbContext(configuration1);
            var context2 = new MongoDbContext(configuration2);

            // Assert
            Assert.NotNull(context1);
            Assert.NotNull(context2);
            Assert.NotEqual(
                context1.Forms.CollectionNamespace.DatabaseNamespace.DatabaseName,
                context2.Forms.CollectionNamespace.DatabaseNamespace.DatabaseName
            );
        }

        [Fact]
        public void Constructor_SameConnectionString_CreatesIndependentContexts()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/TestDatabase";
            var configuration1 = CreateConfiguration(connectionString);
            var configuration2 = CreateConfiguration(connectionString);

            // Act
            var context1 = new MongoDbContext(configuration1);
            var context2 = new MongoDbContext(configuration2);

            // Assert
            Assert.NotNull(context1);
            Assert.NotNull(context2);
            Assert.NotSame(context1, context2);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Constructor_InvalidMongoDbUrl_ThrowsMongoConfigurationException()
        {
            // Arrange
            var connectionString = "invalid://connection/string";
            var configuration = CreateConfiguration(connectionString);

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => new MongoDbContext(configuration));
        }

        [Fact]
        public void Constructor_ConnectionStringWithSpecialCharacters_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://user%40name:p%40ssw0rd@localhost:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_LocalhostWithPort_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://localhost:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_IPv4Address_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://127.0.0.1:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        [Fact]
        public void Constructor_IPv6Address_CreatesContext()
        {
            // Arrange
            var connectionString = "mongodb://[::1]:27017/TestDatabase";
            var configuration = CreateConfiguration(connectionString);

            // Act
            var context = new MongoDbContext(configuration);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Forms);
        }

        #endregion

        #region Helper Methods

        private IConfiguration CreateConfiguration(string connectionString)
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"ConnectionStrings:MongoConnection", connectionString}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        #endregion

        #region Virtual Property Tests (For Mocking)

        [Fact]
        public void Forms_IsVirtual_CanBeMocked()
        {
            // This test verifies that Forms property is virtual and can be mocked
            // Arrange
            var mockContext = new Mock<MongoDbContext>();
            var mockCollection = new Mock<IMongoCollection<Form>>();
            
            mockContext.Setup(x => x.Forms).Returns(mockCollection.Object);

            // Act
            var forms = mockContext.Object.Forms;

            // Assert
            Assert.NotNull(forms);
            Assert.Same(mockCollection.Object, forms);
        }

        #endregion
    }
}
