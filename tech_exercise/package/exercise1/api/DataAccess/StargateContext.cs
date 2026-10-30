using Microsoft.EntityFrameworkCore;
using System.Data;

namespace StargateAPI.DataAccess
{
    public class StargateContext : DbContext
    {
        public IDbConnection Connection => Database.GetDbConnection();
        public DbSet<Person> People { get; set; }
        public DbSet<AstronautDetail> AstronautDetails { get; set; }
        public DbSet<AstronautDuty> AstronautDuties { get; set; }
        public DbSet<ProcessLog> ProcessLogs { get; set; }

        public StargateContext(DbContextOptions<StargateContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StargateContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        public async Task SeedDataAsync(CancellationToken cancellationToken = default)
        {
            if (await People.AnyAsync(cancellationToken))
            {
                return;
            }

            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            People.AddRange(
                new Person { Id = 1, Name = "John Doe" },
                new Person { Id = 2, Name = "Jane Doe" });

            AstronautDetails.Add(new AstronautDetail
            {
                Id = 1,
                PersonId = 1,
                CurrentRank = "1LT",
                CurrentDutyTitle = "Commander",
                CareerStartDate = seedDate
            });

            AstronautDuties.Add(new AstronautDuty
            {
                Id = 1,
                PersonId = 1,
                DutyStartDate = seedDate,
                DutyTitle = "Commander",
                Rank = "1LT"
            });

            await SaveChangesAsync(cancellationToken);
        }
    }
}
