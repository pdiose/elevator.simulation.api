using Microsoft.EntityFrameworkCore;
using Elevator.Simulation.Api.Models;

namespace Elevator.Simulation.Api.Data
{
    public class ElevatorDbContext : DbContext
    {
        public ElevatorDbContext(DbContextOptions<ElevatorDbContext> options) : base(options)
        {
        }

        public DbSet<ElevatorInfo> ElevatorInfos { get; set; }
        public DbSet<ElevatorCall> ElevatorCalls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ElevatorInfo>().ToTable("Elevator_tbl");
            modelBuilder.Entity<ElevatorCall>().ToTable("ElevatorRequest_tbl");

            // Configure the one-to-many relationship
            modelBuilder.Entity<ElevatorCall>()
                .HasOne(e => e.Elevator)
                .WithMany(i => i.ElevatorCalls)
                .HasForeignKey(e => e.AssignedElevator)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}