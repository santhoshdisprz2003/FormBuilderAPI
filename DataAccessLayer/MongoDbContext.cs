using MongoDB.Driver;
using FormBuilderAPI.Model.MongoModel;
using Microsoft.Extensions.Configuration;

namespace FormBuilderAPI.DataAccessLayer
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "MongoDB connection string is missing.");

            var mongoUrl = new MongoUrl(connectionString);
            var client = new MongoClient(mongoUrl);

            var databaseName = mongoUrl.DatabaseName ?? "FormBuilderMongoDB";
            _database = client.GetDatabase(databaseName);
        }

        // Main Forms collection
        public IMongoCollection<Form> Forms => _database.GetCollection<Form>("Forms");

    }
}
