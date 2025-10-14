using System;
using System.Threading.Tasks;
using FormBuilderAPI.Helper;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FormBuilderAPI.DataAccessLayer
{
    public static class DataBaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sqlContext = scope.ServiceProvider.GetRequiredService<SQLDbContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DataBaseSeeder");

            logger.LogInformation("Starting database seeding...");

            // ✅ Seed SQL Admin User only
            if (!await sqlContext.Users.AnyAsync(u => u.Role == "Admin"))
            {
                var hasher = new PasswordHasher();

                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = hasher.HashPassword("Admin@123"),
                    Role = "Admin"
                };

                await sqlContext.Users.AddAsync(adminUser);
                await sqlContext.SaveChangesAsync();

                logger.LogInformation(" Seeded default admin user.");
            }
            else
            {
                logger.LogInformation(" Admin user already exists. Skipping seeding.");
            }

            logger.LogInformation("Database seeding completed successfully.");
        }
    }
}
