using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.Helper;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Model.MongoModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;


namespace FormBuilderAPI.DataAccessLayer
{
    public static class DataBaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sqlContext = scope.ServiceProvider.GetRequiredService<SQLDbContext>();
            var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DataBaseSeeder");

            logger.LogInformation("Starting database seeding...");

            // ✅ Seed SQL Users
            if (!await sqlContext.Users.AnyAsync())
            {
                var hasher = new PasswordHasher();

                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = hasher.HashPassword("Admin@123"),
                    Role = "Admin"
                };

                var learnerUser = new User
                {
                    Username = "learner",
                    PasswordHash = hasher.HashPassword("Learner@123"),
                    Role = "Learner"
                };

                await sqlContext.Users.AddRangeAsync(adminUser, learnerUser);
                await sqlContext.SaveChangesAsync();

                logger.LogInformation("✅ Seeded default users: admin, learner");
            }

            // ✅ Seed Mongo Forms
            var forms = mongoContext.Forms;
            var hasForms = await forms.Find(_ => true).AnyAsync();

            if (!hasForms)
            {
                var sampleForm = new Form
                {
                    Title = "Sample Registration Form",
                    Description = "Demo form created during seeding.",
                    Sections = new List<FormSection>
                    {
                        new FormSection
                        {
                            Title = "Basic Information",
                            Fields = new List<FormField>
                            {
                                new FormField { Label = "Full Name", Type = "text", Required = true },
                                new FormField { Label = "Email", Type = "email", Required = true }
                            }
                        }
                    }
                };

                await forms.InsertOneAsync(sampleForm);
                logger.LogInformation("✅ Inserted sample form in MongoDB.");
            }

            logger.LogInformation("🎯 Database seeding completed successfully.");
        }
    }
}
