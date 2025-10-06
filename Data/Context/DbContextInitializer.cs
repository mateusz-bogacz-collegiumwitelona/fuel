using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Data.Context
{
    public class DbContextInitializer
    {
        public static ApplicationDbContext Create(string connectionString)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(
                    connectionString,
                    o => o.UseNetTopologySuite())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
