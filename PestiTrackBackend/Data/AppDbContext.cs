using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Models.Authorization;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security;
using PestiTrackBackend.Models.Crops;



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

}