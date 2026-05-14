using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace OctafxIndia.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var conn = config.GetConnectionString("ConnectionStrings:DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
                       ?? "Host=127.0.0.1;Port=5432;Database=octafxdb;Username=dbuser;Password=Str0ngDBPass!";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(conn);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}