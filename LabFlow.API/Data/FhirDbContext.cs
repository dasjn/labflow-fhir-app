using LabFlow.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LabFlow.API.Data;

/// <summary>
/// Entity Framework DbContext for FHIR resources
/// Simple and focused on essential FHIR resource storage
/// </summary>
public class FhirDbContext : DbContext
{
    public FhirDbContext(DbContextOptions<FhirDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Patient resources table
    /// </summary>
    public DbSet<PatientEntity> Patients { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Patient entity
        modelBuilder.Entity<PatientEntity>(entity =>
        {
            entity.ToTable("Patients");

            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes for FHIR search performance
            // These make FHIR search parameters fast

            // Index for searching by name (most common search)
            entity.HasIndex(e => e.FamilyName)
                .HasDatabaseName("IX_Patients_FamilyName");

            entity.HasIndex(e => e.GivenName)
                .HasDatabaseName("IX_Patients_GivenName");

            // Index for searching by identifier (medical record number)
            entity.HasIndex(e => e.Identifier)
                .HasDatabaseName("IX_Patients_Identifier");

            // Index for searching by birthdate
            entity.HasIndex(e => e.BirthDate)
                .HasDatabaseName("IX_Patients_BirthDate");

            // Index for _lastUpdated FHIR search parameter
            entity.HasIndex(e => e.LastUpdated)
                .HasDatabaseName("IX_Patients_LastUpdated");

            // Index for filtering out soft-deleted resources
            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Patients_IsDeleted");

            // Column configurations
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();

            // FhirJson column type depends on database provider
            // PostgreSQL: jsonb (binary JSON, more efficient)
            // SQLite: TEXT (SQLite doesn't have native JSON type)
            var jsonColumnType = Database.IsNpgsql() ? "jsonb" : "TEXT";
            entity.Property(e => e.FhirJson)
                .HasColumnType(jsonColumnType)
                .IsRequired();

            entity.Property(e => e.FamilyName)
                .HasMaxLength(100);

            entity.Property(e => e.GivenName)
                .HasMaxLength(100);

            entity.Property(e => e.Identifier)
                .HasMaxLength(100);

            entity.Property(e => e.Gender)
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdated)
                .IsRequired();

            entity.Property(e => e.VersionId)
                .IsRequired();

            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
        });
    }
}
