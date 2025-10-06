using Microsoft.EntityFrameworkCore;
using Elevator.Simulation.Api.Models;

namespace Elevator.Simulation.Api.Data
{
    public class ElevatorDbContext : DbContext
    {
        public ElevatorDbContext(DbContextOptions<ElevatorDbContext> options) : base(options)
        {
        }

        public DbSet<ElevatorInfo> ElevatorInfo { get; set; }
        public DbSet<ElevatorCall> ElevatorCall { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           //Map entities to table names
           modelBuilder.Entity<ElevatorInfo>().ToTable("Elevator_tbl");
           modelBuilder.Entity<ElevatorCall>().ToTable("ElevatorRequest_tbl");

        }
    }
}