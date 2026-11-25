using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Models.Authorization;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Products;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Models.Weather;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Auditing;
using PestiTrackBackend.Models.Compliance;
using PestiTrackBackend.Models.Notifications;



namespace PestiTrackBackend.Data;

public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Farm management
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<FarmUser> FarmUsers => Set<FarmUser>();
    public DbSet<Field> Fields => Set<Field>();

    // Farm-level authorization
    public DbSet<FarmRole> FarmRoles => Set<FarmRole>();
    public DbSet<FarmUserRole> FarmUserRoles => Set<FarmUserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<FarmRolePermission> FarmRolePermissions
        => Set<FarmRolePermission>();
        
    public DbSet<Crop> Crops => Set<Crop>();

    public DbSet<FieldCrop> FieldCrops => Set<FieldCrop>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Worker> Workers => Set<Worker>();

    public DbSet<WeatherRecord> WeatherRecords => Set<WeatherRecord>();

    public DbSet<InventoryTransaction> InventoryTransactions
        => Set<InventoryTransaction>();

    public DbSet<InventoryThreshold> InventoryThresholds
        => Set<InventoryThreshold>();

    public DbSet<PesticideApplication> PesticideApplications
        => Set<PesticideApplication>();

    public DbSet<PesticideApplicationProduct> PesticideApplicationProducts
        => Set<PesticideApplicationProduct>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ComplianceRecord> ComplianceRecords
        => Set<ComplianceRecord>();

    public DbSet<FarmNotification> FarmNotifications
        => Set<FarmNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configure the standard ASP.NET Core Identity tables first.
        base.OnModelCreating(builder);

        // Configure custom Identity fields.
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName)
                .HasMaxLength(150)
                .IsRequired();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Description)
                .HasMaxLength(300);
        });
        // Seed the predefined permission catalog.
        builder.Entity<Permission>().HasData(PermissionSeedData.All);

        // Seed protected system-wide roles.
        builder.Entity<ApplicationRole>().HasData(SystemRoleSeedData.All);



        ConfigureFarm(builder);
        ConfigureField(builder);
        ConfigureCrop(builder);
        ConfigureFieldCrop(builder);
        ConfigureFarmUser(builder);
        ConfigureFarmRole(builder);
        ConfigurePermission(builder);
        ConfigureFarmUserRole(builder);
        ConfigureFarmRolePermission(builder);
        ConfigureProduct(builder);
        ConfigureWorker(builder);
        ConfigureWeather(builder);
        ConfigureInventory(builder);
        ConfigureApplications(builder);
        ConfigureAuditLog(builder);
        ConfigureComplianceRecord(builder);
        ConfigureInventoryThreshold(builder);
        ConfigureFarmNotification(builder);
    }

    private static void ConfigureAuditLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.HasKey(log => log.Id);

            entity.Property(log => log.Action)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(log => log.EntityType)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(log => log.EntityId)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(log => log.ChangesJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(log => log.ActorDisplayName)
                .HasMaxLength(150);

            entity.Property(log => log.ActorEmail)
                .HasMaxLength(256);

            entity.Property(log => log.HttpMethod)
                .HasMaxLength(10);

            entity.Property(log => log.RequestPath)
                .HasMaxLength(500);

            entity.Property(log => log.IpAddress)
                .HasMaxLength(64);

            entity.Property(log => log.UserAgent)
                .HasMaxLength(500);

            entity.HasIndex(log => log.OccurredAtUtc);
            entity.HasIndex(log => new
            {
                log.FarmId,
                log.OccurredAtUtc
            });
            entity.HasIndex(log => new
            {
                log.ActorUserId,
                log.OccurredAtUtc
            });
            entity.HasIndex(log => new
            {
                log.EntityType,
                log.EntityId
            });

            entity.HasOne(log => log.ActorUser)
                .WithMany()
                .HasForeignKey(log => log.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFarm(ModelBuilder builder)
    {
        builder.Entity<Farm>(entity =>
        {
            entity.ToTable("Farms");

            entity.HasKey(f => f.Id);

            entity.Property(f => f.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(f => f.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(f => f.Location)
                .HasMaxLength(300);

            entity.Property(f => f.Latitude)
                .HasPrecision(9, 6);

            entity.Property(f => f.Longitude)
                .HasPrecision(9, 6);

            entity.HasIndex(f => f.Code)
                .IsUnique();
        });
    }

    private static void ConfigureFarmUser(ModelBuilder builder)
    {
        builder.Entity<FarmUser>(entity =>
        {
            entity.ToTable("FarmUsers");

            entity.HasKey(fu => fu.Id);

            entity.HasAlternateKey(fu => new { fu.Id, fu.FarmId });

            entity.HasIndex(fu => new { fu.FarmId, fu.UserId })
                .IsUnique();

            entity.HasOne(fu => fu.Farm)
                .WithMany(f => f.UserAssignments)
                .HasForeignKey(fu => fu.FarmId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(fu => fu.User)
                .WithMany(u => u.FarmAssignments)
                .HasForeignKey(fu => fu.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFarmRole(ModelBuilder builder)
    {
        builder.Entity<FarmRole>(entity =>
        {
            entity.ToTable("FarmRoles");

            entity.HasKey(fr => fr.Id);

            entity.HasAlternateKey(fr => new { fr.Id, fr.FarmId });

            entity.Property(fr => fr.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(fr => fr.Description)
                .HasMaxLength(300);

            entity.HasIndex(fr => new { fr.FarmId, fr.Name })
                .IsUnique();

            entity.HasOne(fr => fr.Farm)
                .WithMany(f => f.Roles)
                .HasForeignKey(fr => fr.FarmId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePermission(ModelBuilder builder)
    {
        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Code)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasMaxLength(300);

            entity.HasIndex(p => p.Code)
                .IsUnique();
        });
    }

    private static void ConfigureFarmUserRole(ModelBuilder builder)
    {
        builder.Entity<FarmUserRole>(entity =>
        {
            entity.ToTable("FarmUserRoles");

            entity.HasKey(fur => fur.Id);

            entity.HasIndex(fur => new
            {
                fur.FarmUserId,
                fur.FarmRoleId
            }).IsUnique();

            // The user assignment must belong to the same farm.
            entity.HasOne(fur => fur.FarmUser)
                .WithMany(fu => fu.Roles)
                .HasForeignKey(fur => new
                {
                    fur.FarmUserId,
                    fur.FarmId
                })
                .HasPrincipalKey(fu => new
                {
                    fu.Id,
                    fu.FarmId
                })
                .OnDelete(DeleteBehavior.Restrict);

            // The farm role must belong to that same farm.
            entity.HasOne(fur => fur.FarmRole)
                .WithMany(fr => fr.UserAssignments)
                .HasForeignKey(fur => new
                {
                    fur.FarmRoleId,
                    fur.FarmId
                })
                .HasPrincipalKey(fr => new
                {
                    fr.Id,
                    fr.FarmId
                })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFarmRolePermission(ModelBuilder builder)
    {
        builder.Entity<FarmRolePermission>(entity =>
        {
            entity.ToTable("FarmRolePermissions");

            entity.HasKey(frp => frp.Id);

            entity.HasIndex(frp => new
                {
                    frp.FarmRoleId,
                    frp.PermissionId
                })
                .IsUnique();

            entity.HasOne(frp => frp.FarmRole)
                .WithMany(fr => fr.Permissions)
                .HasForeignKey(frp => frp.FarmRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(frp => frp.Permission)
                .WithMany(p => p.FarmRoles)
                .HasForeignKey(frp => frp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }




    private static void ConfigureField(ModelBuilder builder)
    {
        builder.Entity<Field>(entity =>
        {
            entity.ToTable("Fields");

            entity.HasKey(f => f.Id);

            entity.HasAlternateKey(f => new
            {
                f.Id,
                f.FarmId
            });

            entity.Property(f => f.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(f => f.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(f => f.AreaHectares)
                .HasPrecision(18, 3);

            entity.Property(f => f.Description)
                .HasMaxLength(500);

            // A field code must be unique within its farm.
            entity.HasIndex(f => new { f.FarmId, f.Code })
                .IsUnique();

            entity.HasOne(f => f.Farm)
                .WithMany(farm => farm.Fields)
                .HasForeignKey(f => f.FarmId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }


        private static void ConfigureCrop(ModelBuilder builder)
    {
        builder.Entity<Crop>(entity =>
        {
            entity.ToTable("Crops");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(c => c.ScientificName)
                .HasMaxLength(200);

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.HasIndex(c => c.Name)
                .IsUnique();
        });
    }

    private static void ConfigureFieldCrop(ModelBuilder builder)
    {
        builder.Entity<FieldCrop>(entity =>
        {
            entity.ToTable("FieldCrops");

            entity.HasKey(fc => fc.Id);

            entity.Property(fc => fc.AreaHectares)
                .HasPrecision(18, 3);

            entity.Property(fc => fc.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(fc => new
            {
                fc.FarmId,
                fc.FieldId,
                fc.CropId,
                fc.PlantingDate
            });

            entity.HasOne(fc => fc.Farm)
                .WithMany(farm => farm.FieldCrops)
                .HasForeignKey(fc => fc.FarmId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(fc => fc.Field)
                .WithMany(field => field.CropAssignments)
                .HasForeignKey(fc => new
                {
                    fc.FieldId,
                    fc.FarmId
                })
                .HasPrincipalKey(field => new
                {
                    field.Id,
                    field.FarmId
                })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(fc => fc.Crop)
                .WithMany(crop => crop.FieldAssignments)
                .HasForeignKey(fc => fc.CropId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }


    private static void ConfigureProduct(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(product => product.Id);

            entity.Property(product => product.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(product => product.ActiveIngredient)
                .HasMaxLength(300);

            entity.Property(product => product.Manufacturer)
                .HasMaxLength(200);

            entity.Property(product => product.RegistrationNumber)
                .HasMaxLength(100);

            entity.Property(product => product.Formulation)
                .HasMaxLength(100);

            entity.Property(product => product.Concentration)
                .HasMaxLength(150);

            entity.Property(product => product.HazardClass)
                .HasMaxLength(100);

            entity.Property(product => product.SignalWord)
                .HasMaxLength(50);

            entity.Property(product => product.MaxApplicationRate)
                .HasPrecision(18, 4);

            entity.Property(product => product.MaxApplicationRateUnit)
                .HasMaxLength(50);

            entity.Property(product => product.MaxWindSpeedMetersPerSecond)
                .HasPrecision(6, 2);

            entity.Property(product => product.LabelNotes)
                .HasMaxLength(2000);

            entity.Property(product => product.Description)
                .HasMaxLength(1000);

            entity.HasIndex(product => product.Name);

            entity.HasIndex(product => product.RegistrationNumber);
        });
    }





    private static void ConfigureWorker(ModelBuilder builder)
    {
        builder.Entity<Worker>(entity =>
        {
            entity.ToTable("Workers");

            entity.HasKey(worker => worker.Id);

            entity.Property(worker => worker.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(worker => worker.WorkerCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(worker => worker.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(worker => worker.Email)
                .HasMaxLength(256);

            entity.Property(worker => worker.CertificationNumber)
                .HasMaxLength(100);

            // Worker codes must be unique within each farm.
            entity.HasIndex(worker => new
            {
                worker.FarmId,
                worker.WorkerCode
            }).IsUnique();

            entity.HasOne(worker => worker.Farm)
                .WithMany(farm => farm.Workers)
                .HasForeignKey(worker => worker.FarmId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }




    private static void ConfigureWeather(ModelBuilder builder)
    {
        builder.Entity<WeatherRecord>(entity =>
        {
            entity.ToTable("WeatherRecords");

            entity.HasKey(weather => weather.Id);

            entity.Property(weather => weather.TemperatureCelsius)
                .HasPrecision(6, 2);

            entity.Property(weather => weather.HumidityPercentage)
                .HasPrecision(5, 2);

            entity.Property(weather => weather.RainfallMillimeters)
                .HasPrecision(10, 2);

            entity.Property(weather => weather.WindSpeedMetersPerSecond)
                .HasPrecision(8, 2);

            entity.Property(weather => weather.WindDirectionDegrees)
                .HasPrecision(6, 2);

            entity.Property(weather => weather.Conditions)
                .HasMaxLength(100);

            entity.Property(weather => weather.Source)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(weather => weather.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(weather => new
            {
                weather.FarmId,
                weather.RecordedAtUtc
            });

            entity.HasIndex(weather => new
            {
                weather.FieldId,
                weather.FarmId,
                weather.RecordedAtUtc
            });

            entity.HasOne(weather => weather.Farm)
                .WithMany(farm => farm.WeatherRecords)
                .HasForeignKey(weather => weather.FarmId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(weather => weather.Field)
                .WithMany(field => field.WeatherRecords)
                .HasForeignKey(weather => new
                {
                    weather.FieldId,
                    weather.FarmId
                })
                .HasPrincipalKey(field => new
                {
                    field.Id,
                    field.FarmId
                })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventory(ModelBuilder builder)
    {
        builder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions");

            entity.HasKey(transaction => transaction.Id);

            entity.Property(transaction => transaction.TransactionType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(transaction => transaction.Quantity)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.Property(transaction => transaction.BatchNumber)
                .HasMaxLength(100);

            entity.Property(transaction => transaction.Reference)
                .HasMaxLength(100);

            entity.Property(transaction => transaction.Reason)
               .HasMaxLength(500);    

            entity.Property(transaction => transaction.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(transaction => new
            {
                transaction.FarmId,
                transaction.ProductId,
                transaction.CreatedAtUtc
            });

            entity.HasIndex(transaction => new
            {
                transaction.FarmId,
                transaction.ProductId,
                transaction.BatchNumber
            });

            // Supports duplicate-reference checks within a farm,
            // product, batch, and transaction type.
            entity.HasIndex(transaction => new
            {
                transaction.FarmId,
                transaction.ProductId,
                transaction.BatchNumber,
                transaction.TransactionType,
                transaction.Reference
            })
            .HasDatabaseName("IX_InventoryTransactions_ReferenceCheck")
            .HasFilter("[Reference] IS NOT NULL");


            entity.HasOne(transaction => transaction.Farm)
                .WithMany()
                .HasForeignKey(transaction => transaction.FarmId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(transaction => transaction.Product)
                .WithMany()
                .HasForeignKey(transaction => transaction.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(transaction => transaction.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);



            entity.HasOne(transaction => transaction.ApplicationProduct)
                .WithOne(productLine => productLine.InventoryTransaction)
                .HasForeignKey<InventoryTransaction>(
                    transaction => transaction.PesticideApplicationProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(transaction =>
                    transaction.PesticideApplicationProductId)
                .IsUnique()
                .HasFilter("[PesticideApplicationProductId] IS NOT NULL")
                .HasDatabaseName(
                    "IX_InventoryTransactions_ApplicationProductId");
        });
    }


    private static void ConfigureApplications(ModelBuilder builder)
{
    builder.Entity<PesticideApplication>(entity =>
    {
        entity.ToTable("PesticideApplications");

        entity.HasKey(application => application.Id);

        entity.Property(application => application.TargetPest)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(application => application.ApplicationMethod)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(application => application.Equipment)
            .HasMaxLength(200);

        entity.Property(application => application.AreaTreatedHectares)
            .HasPrecision(18, 3)
            .IsRequired();

        entity.Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        entity.Property(application => application.Notes)
            .HasMaxLength(1000);

        entity.HasIndex(application => new
        {
            application.FarmId,
            application.ApplicationDateUtc
        });

        entity.HasIndex(application => new
        {
            application.FarmId,
            application.Status,
            application.ApplicationDateUtc
        });

        entity.HasIndex(application => new
        {
            application.FarmId,
            application.FieldId
        });

        entity.HasOne(application => application.Farm)
            .WithMany()
            .HasForeignKey(application => application.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(application => application.Field)
            .WithMany()
            .HasForeignKey(application => application.FieldId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(application => application.FieldCrop)
            .WithMany()
            .HasForeignKey(application => application.FieldCropId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(application => application.Worker)
            .WithMany()
            .HasForeignKey(application => application.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(application => application.WeatherRecord)
            .WithMany()
            .HasForeignKey(application => application.WeatherRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(application => application.CreatedByUser)
            .WithMany()
            .HasForeignKey(application => application.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    });

    builder.Entity<PesticideApplicationProduct>(entity =>
    {
            entity.ToTable("PesticideApplicationProducts");

            entity.HasKey(productLine => productLine.Id);

            entity.Property(productLine => productLine.BatchNumber)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(productLine => productLine.QuantityUsed)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.Property(productLine => productLine.ApplicationRate)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.Property(productLine => productLine.RateUnit)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(productLine =>
                productLine.PesticideApplicationId);

            entity.HasIndex(productLine =>
                productLine.ProductId);

            entity.HasOne(productLine => productLine.PesticideApplication)
                .WithMany(application => application.Products)
                .HasForeignKey(productLine => productLine.PesticideApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(productLine => productLine.Product)
                .WithMany()
                .HasForeignKey(productLine => productLine.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureComplianceRecord(ModelBuilder builder)
    {
        builder.Entity<ComplianceRecord>(entity =>
        {
            entity.ToTable("ComplianceRecords");

            entity.HasKey(record => record.Id);

            entity.Property(record => record.RecordType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(record => record.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(record => record.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(record => record.Severity)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(record => record.InspectorName)
                .HasMaxLength(150);

            entity.Property(record => record.Findings)
                .HasMaxLength(2000);

            entity.Property(record => record.CorrectiveAction)
                .HasMaxLength(2000);

            entity.HasIndex(record => new
            {
                record.FarmId,
                record.Status,
                record.CheckedAtUtc
            });

            entity.HasIndex(record => new
            {
                record.FarmId,
                record.RecordType
            });

            entity.HasOne(record => record.Farm)
                .WithMany()
                .HasForeignKey(record => record.FarmId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(record => record.Field)
                .WithMany()
                .HasForeignKey(record => record.FieldId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(record => record.PesticideApplication)
                .WithMany()
                .HasForeignKey(record => record.PesticideApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(record => record.Worker)
                .WithMany()
                .HasForeignKey(record => record.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(record => record.CreatedByUser)
                .WithMany()
                .HasForeignKey(record => record.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventoryThreshold(ModelBuilder builder)
    {
        builder.Entity<InventoryThreshold>(entity =>
        {
            entity.ToTable("InventoryThresholds");

            entity.HasKey(threshold => threshold.Id);

            entity.Property(threshold => threshold.MinimumQuantity)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.HasIndex(threshold => new
            {
                threshold.FarmId,
                threshold.ProductId
            }).IsUnique();

            entity.HasOne(threshold => threshold.Farm)
                .WithMany()
                .HasForeignKey(threshold => threshold.FarmId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(threshold => threshold.Product)
                .WithMany()
                .HasForeignKey(threshold => threshold.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFarmNotification(ModelBuilder builder)
    {
        builder.Entity<FarmNotification>(entity =>
        {
            entity.ToTable("FarmNotifications");

            entity.HasKey(notification => notification.Id);

            entity.Property(notification => notification.DeduplicationKey)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(notification => notification.Category)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(notification => notification.Severity)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(notification => notification.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(notification => notification.Message)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(notification => notification.ActionPath)
                .HasMaxLength(200);

            entity.HasIndex(notification => new
            {
                notification.FarmId,
                notification.DeduplicationKey
            }).IsUnique();

            entity.HasIndex(notification => new
            {
                notification.FarmId,
                notification.IsResolved,
                notification.IsRead
            });

            entity.HasOne(notification => notification.Farm)
                .WithMany()
                .HasForeignKey(notification => notification.FarmId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}