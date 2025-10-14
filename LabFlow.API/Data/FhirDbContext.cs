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

    /// <summary>
    /// Observation resources table (laboratory results)
    /// </summary>
    public DbSet<ObservationEntity> Observations { get; set; } = null!;

    /// <summary>
    /// DiagnosticReport resources table (grouped laboratory reports)
    /// </summary>
    public DbSet<DiagnosticReportEntity> DiagnosticReports { get; set; } = null!;

    /// <summary>
    /// ServiceRequest resources table (laboratory orders)
    /// </summary>
    public DbSet<ServiceRequestEntity> ServiceRequests { get; set; } = null!;

    /// <summary>
    /// Users table (JWT authentication)
    /// </summary>
    public DbSet<UserEntity> Users { get; set; } = null!;

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

        // Configure Observation entity
        modelBuilder.Entity<ObservationEntity>(entity =>
        {
            entity.ToTable("Observations");

            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes for FHIR search performance

            // Index for searching by patient reference (most common for lab results)
            entity.HasIndex(e => e.PatientId)
                .HasDatabaseName("IX_Observations_PatientId");

            // Index for searching by observation code (LOINC)
            entity.HasIndex(e => e.Code)
                .HasDatabaseName("IX_Observations_Code");

            // Index for searching by status
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Observations_Status");

            // Index for searching by category
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_Observations_Category");

            // Index for searching by date
            entity.HasIndex(e => e.EffectiveDateTime)
                .HasDatabaseName("IX_Observations_EffectiveDateTime");

            // Index for _lastUpdated FHIR search parameter
            entity.HasIndex(e => e.LastUpdated)
                .HasDatabaseName("IX_Observations_LastUpdated");

            // Index for filtering out soft-deleted resources
            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Observations_IsDeleted");

            // Compound index for patient + date queries (very common pattern)
            entity.HasIndex(e => new { e.PatientId, e.EffectiveDateTime })
                .HasDatabaseName("IX_Observations_PatientId_EffectiveDateTime");

            // Column configurations
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();

            var jsonColumnType = Database.IsNpgsql() ? "jsonb" : "TEXT";
            entity.Property(e => e.FhirJson)
                .HasColumnType(jsonColumnType)
                .IsRequired();

            entity.Property(e => e.PatientId)
                .HasMaxLength(50);

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.CodeDisplay)
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .HasMaxLength(20);

            entity.Property(e => e.Category)
                .HasMaxLength(50);

            entity.Property(e => e.ValueUnit)
                .HasMaxLength(50);

            entity.Property(e => e.ValueCodeableConcept)
                .HasMaxLength(100);

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

        // Configure DiagnosticReport entity
        modelBuilder.Entity<DiagnosticReportEntity>(entity =>
        {
            entity.ToTable("DiagnosticReports");

            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes for FHIR search performance

            // Index for searching by patient reference (most common)
            entity.HasIndex(e => e.PatientId)
                .HasDatabaseName("IX_DiagnosticReports_PatientId");

            // Index for searching by report code
            entity.HasIndex(e => e.Code)
                .HasDatabaseName("IX_DiagnosticReports_Code");

            // Index for searching by status
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_DiagnosticReports_Status");

            // Index for searching by category
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_DiagnosticReports_Category");

            // Index for searching by effective date
            entity.HasIndex(e => e.EffectiveDateTime)
                .HasDatabaseName("IX_DiagnosticReports_EffectiveDateTime");

            // Index for searching by issued date
            entity.HasIndex(e => e.Issued)
                .HasDatabaseName("IX_DiagnosticReports_Issued");

            // Index for _lastUpdated FHIR search parameter
            entity.HasIndex(e => e.LastUpdated)
                .HasDatabaseName("IX_DiagnosticReports_LastUpdated");

            // Index for filtering out soft-deleted resources
            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_DiagnosticReports_IsDeleted");

            // Compound index for patient + date queries (common pattern)
            entity.HasIndex(e => new { e.PatientId, e.Issued })
                .HasDatabaseName("IX_DiagnosticReports_PatientId_Issued");

            // Column configurations
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();

            var jsonColumnType = Database.IsNpgsql() ? "jsonb" : "TEXT";
            entity.Property(e => e.FhirJson)
                .HasColumnType(jsonColumnType)
                .IsRequired();

            entity.Property(e => e.PatientId)
                .HasMaxLength(50);

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.CodeDisplay)
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .HasMaxLength(20);

            entity.Property(e => e.Category)
                .HasMaxLength(50);

            entity.Property(e => e.ResultIds)
                .HasMaxLength(1000); // Comma-separated list of observation IDs

            entity.Property(e => e.Conclusion)
                .HasMaxLength(2000);

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

        // Configure ServiceRequest entity
        modelBuilder.Entity<ServiceRequestEntity>(entity =>
        {
            entity.ToTable("ServiceRequests");

            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes for FHIR search performance

            // Index for searching by patient reference (most common for lab orders)
            entity.HasIndex(e => e.PatientId)
                .HasDatabaseName("IX_ServiceRequests_PatientId");

            // Index for searching by service code (LOINC)
            entity.HasIndex(e => e.Code)
                .HasDatabaseName("IX_ServiceRequests_Code");

            // Index for searching by status
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_ServiceRequests_Status");

            // Index for searching by intent
            entity.HasIndex(e => e.Intent)
                .HasDatabaseName("IX_ServiceRequests_Intent");

            // Index for searching by category
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_ServiceRequests_Category");

            // Index for searching by authored date
            entity.HasIndex(e => e.AuthoredOn)
                .HasDatabaseName("IX_ServiceRequests_AuthoredOn");

            // Index for searching by requester
            entity.HasIndex(e => e.RequesterId)
                .HasDatabaseName("IX_ServiceRequests_RequesterId");

            // Index for searching by performer
            entity.HasIndex(e => e.PerformerId)
                .HasDatabaseName("IX_ServiceRequests_PerformerId");

            // Index for searching by occurrence date
            entity.HasIndex(e => e.OccurrenceDateTime)
                .HasDatabaseName("IX_ServiceRequests_OccurrenceDateTime");

            // Index for _lastUpdated FHIR search parameter
            entity.HasIndex(e => e.LastUpdated)
                .HasDatabaseName("IX_ServiceRequests_LastUpdated");

            // Index for filtering out soft-deleted resources
            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_ServiceRequests_IsDeleted");

            // Compound index for patient + authored date queries (common pattern: "recent orders for patient")
            entity.HasIndex(e => new { e.PatientId, e.AuthoredOn })
                .HasDatabaseName("IX_ServiceRequests_PatientId_AuthoredOn");

            // Column configurations
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();

            var jsonColumnType = Database.IsNpgsql() ? "jsonb" : "TEXT";
            entity.Property(e => e.FhirJson)
                .HasColumnType(jsonColumnType)
                .IsRequired();

            entity.Property(e => e.PatientId)
                .HasMaxLength(50);

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.CodeDisplay)
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .HasMaxLength(20);

            entity.Property(e => e.Intent)
                .HasMaxLength(20);

            entity.Property(e => e.Category)
                .HasMaxLength(50);

            entity.Property(e => e.Priority)
                .HasMaxLength(20);

            entity.Property(e => e.RequesterId)
                .HasMaxLength(50);

            entity.Property(e => e.PerformerId)
                .HasMaxLength(50);

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

        // Configure User entity (JWT authentication)
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");

            // Primary key
            entity.HasKey(e => e.Id);

            // Unique index for email (used for login)
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            // Index for role-based queries
            entity.HasIndex(e => e.Role)
                .HasDatabaseName("IX_Users_Role");

            // Index for filtering active users
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

            // Index for filtering out soft-deleted users
            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Users_IsDeleted");

            // Column configurations
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();

            // BCrypt hash is always 60 characters
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.FullName)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdated)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
        });
    }
}
