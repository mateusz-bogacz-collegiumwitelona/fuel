using Microsoft.EntityFrameworkCore.Design;

namespace Data.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            string connectionString = "User ID=user;Password=pass;Host=postgis;Port=5432;Database=database;Pooling=true;";
            return DbContextInitializer.Create(connectionString);
        }
    }
}
