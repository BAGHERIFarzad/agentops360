using AgentOps360.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentOps360.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<AgentTask> AgentTasks => Set<AgentTask>();
    public DbSet<RiskItem> RiskItems => Set<RiskItem>();
    public DbSet<GeneratedEmail> GeneratedEmails => Set<GeneratedEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.AgentRuns)
            .WithOne(a => a.Project)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AgentRun>()
            .HasMany(a => a.Tasks)
            .WithOne(t => t.AgentRun)
            .HasForeignKey(t => t.AgentRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AgentRun>()
            .HasMany(a => a.Risks)
            .WithOne(r => r.AgentRun)
            .HasForeignKey(r => r.AgentRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AgentRun>()
            .HasMany(a => a.GeneratedEmails)
            .WithOne(e => e.AgentRun)
            .HasForeignKey(e => e.AgentRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}