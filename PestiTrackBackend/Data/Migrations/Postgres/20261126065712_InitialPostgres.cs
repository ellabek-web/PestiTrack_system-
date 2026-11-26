using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PestiTrackBackend.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Crops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ScientificName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Farms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Farms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActiveIngredient = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Formulation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Concentration = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    HazardClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SignalWord = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RegistrationExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PreHarvestIntervalDays = table.Column<int>(type: "integer", nullable: true),
                    ReEntryIntervalHours = table.Column<int>(type: "integer", nullable: true),
                    MaxApplicationRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxApplicationRateUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MaxWindSpeedMetersPerSecond = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    LabelNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorDisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeduplicationKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionPath = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmNotifications_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmRoles", x => x.Id);
                    table.UniqueConstraint("AK_FarmRoles_Id_FarmId", x => new { x.Id, x.FarmId });
                    table.ForeignKey(
                        name: "FK_FarmRoles_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmUsers", x => x.Id);
                    table.UniqueConstraint("AK_FarmUsers_Id_FarmId", x => new { x.Id, x.FarmId });
                    table.ForeignKey(
                        name: "FK_FarmUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmUsers_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AreaHectares = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.Id);
                    table.UniqueConstraint("AK_Fields_Id_FarmId", x => new { x.Id, x.FarmId });
                    table.ForeignKey(
                        name: "FK_Fields_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsApplicator = table.Column<bool>(type: "boolean", nullable: false),
                    CertificationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CertificationExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workers_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinimumQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryThresholds_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryThresholds_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmRolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmRolePermissions_FarmRoles_FarmRoleId",
                        column: x => x.FarmRoleId,
                        principalTable: "FarmRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FarmRolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmUserRoles_FarmRoles_FarmRoleId_FarmId",
                        columns: x => new { x.FarmRoleId, x.FarmId },
                        principalTable: "FarmRoles",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmUserRoles_FarmUsers_FarmUserId_FarmId",
                        columns: x => new { x.FarmUserId, x.FarmId },
                        principalTable: "FarmUsers",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldCrops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedHarvestDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualHarvestDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AreaHectares = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldCrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldCrops_Crops_CropId",
                        column: x => x.CropId,
                        principalTable: "Crops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCrops_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCrops_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeatherRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    HumidityPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    RainfallMillimeters = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    WindSpeedMetersPerSecond = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    WindDirectionDegrees = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    Conditions = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherRecords_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeatherRecords_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PesticideApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldCropId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetPest = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApplicationMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Equipment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AreaTreatedHectares = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    WeatherRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PesticideApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_FieldCrops_FieldCropId",
                        column: x => x.FieldCropId,
                        principalTable: "FieldCrops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_WeatherRecords_WeatherRecordId",
                        column: x => x.WeatherRecordId,
                        principalTable: "WeatherRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    PesticideApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InspectorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Findings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_PesticideApplications_PesticideApplicatio~",
                        column: x => x.PesticideApplicationId,
                        principalTable: "PesticideApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PesticideApplicationProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PesticideApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuantityUsed = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ApplicationRate = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    RateUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PesticideApplicationProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PesticideApplicationProducts_PesticideApplications_Pesticid~",
                        column: x => x.PesticideApplicationId,
                        principalTable: "PesticideApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplicationProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PesticideApplicationProductId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_PesticideApplicationProducts_Pesticid~",
                        column: x => x.PesticideApplicationProductId,
                        principalTable: "PesticideApplicationProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "IsSystemRole", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "20000000-0000-0000-0000-000000000001", "Full system-wide administrative access.", true, "SystemAdministrator", "SYSTEMADMINISTRATOR" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "20000000-0000-0000-0000-000000000002", "Read-only system-wide audit access.", true, "SystemAuditor", "SYSTEMAUDITOR" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "farms.view", "View farm information.", "View farms" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "farms.manage", "Create and update farm information.", "Manage farms" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "fields.view", "View field information.", "View fields" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "fields.manage", "Create and update fields.", "Manage fields" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "crops.view", "View crop information.", "View crops" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "crops.manage", "Create and update crop information.", "Manage crops" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "products.view", "View pesticide product information.", "View products" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "products.manage", "Create and update pesticide products.", "Manage products" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "applications.view", "View pesticide application records.", "View applications" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "applications.create", "Create pesticide application records.", "Create applications" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "applications.approve", "Approve pesticide application records.", "Approve applications" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "inventory.view", "View inventory records.", "View inventory" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "inventory.manage", "Manage inventory transactions.", "Manage inventory" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "workers.view", "View worker information.", "View workers" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "workers.manage", "Create and update worker information.", "Manage workers" },
                    { new Guid("10000000-0000-0000-0000-000000000016"), "weather.view", "View weather records.", "View weather" },
                    { new Guid("10000000-0000-0000-0000-000000000017"), "weather.manage", "Create and update weather records.", "Manage weather" },
                    { new Guid("10000000-0000-0000-0000-000000000018"), "reports.view", "View reports.", "View reports" },
                    { new Guid("10000000-0000-0000-0000-000000000019"), "reports.export", "Export report data.", "Export reports" },
                    { new Guid("10000000-0000-0000-0000-000000000020"), "farm-users.view", "View users assigned to a farm.", "View farm users" },
                    { new Guid("10000000-0000-0000-0000-000000000021"), "farm-users.manage", "Manage farm user assignments and farm roles.", "Manage farm users" },
                    { new Guid("10000000-0000-0000-0000-000000000022"), "audit.view", "View farm audit records.", "View audit trail" },
                    { new Guid("10000000-0000-0000-0000-000000000023"), "inventory.issue", "Issue pesticide products from farm inventory.", "Issue inventory" },
                    { new Guid("10000000-0000-0000-0000-000000000024"), "compliance.view", "View certification monitoring, safety intervals, and compliance records.", "View safety and compliance" },
                    { new Guid("10000000-0000-0000-0000-000000000025"), "compliance.manage", "Create and update farm compliance records and corrective actions.", "Manage safety and compliance" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_FarmId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "FarmId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAtUtc",
                table: "AuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_CreatedByUserId",
                table: "ComplianceRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_FarmId_RecordType",
                table: "ComplianceRecords",
                columns: new[] { "FarmId", "RecordType" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_FarmId_Status_CheckedAtUtc",
                table: "ComplianceRecords",
                columns: new[] { "FarmId", "Status", "CheckedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_FieldId",
                table: "ComplianceRecords",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_PesticideApplicationId",
                table: "ComplianceRecords",
                column: "PesticideApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_WorkerId",
                table: "ComplianceRecords",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Crops_Name",
                table: "Crops",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmNotifications_FarmId_DeduplicationKey",
                table: "FarmNotifications",
                columns: new[] { "FarmId", "DeduplicationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmNotifications_FarmId_IsResolved_IsRead",
                table: "FarmNotifications",
                columns: new[] { "FarmId", "IsResolved", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmRolePermissions_FarmRoleId_PermissionId",
                table: "FarmRolePermissions",
                columns: new[] { "FarmRoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmRolePermissions_PermissionId",
                table: "FarmRolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmRoles_FarmId_Name",
                table: "FarmRoles",
                columns: new[] { "FarmId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Farms_Code",
                table: "Farms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmUserRoles_FarmRoleId_FarmId",
                table: "FarmUserRoles",
                columns: new[] { "FarmRoleId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmUserRoles_FarmUserId_FarmId",
                table: "FarmUserRoles",
                columns: new[] { "FarmUserId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmUserRoles_FarmUserId_FarmRoleId",
                table: "FarmUserRoles",
                columns: new[] { "FarmUserId", "FarmRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmUsers_FarmId_UserId",
                table: "FarmUsers",
                columns: new[] { "FarmId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmUsers_UserId",
                table: "FarmUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCrops_CropId",
                table: "FieldCrops",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCrops_FarmId_FieldId_CropId_PlantingDate",
                table: "FieldCrops",
                columns: new[] { "FarmId", "FieldId", "CropId", "PlantingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldCrops_FieldId_FarmId",
                table: "FieldCrops",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_Fields_FarmId_Code",
                table: "Fields",
                columns: new[] { "FarmId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryThresholds_FarmId_ProductId",
                table: "InventoryThresholds",
                columns: new[] { "FarmId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryThresholds_ProductId",
                table: "InventoryThresholds",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ApplicationProductId",
                table: "InventoryTransactions",
                column: "PesticideApplicationProductId",
                unique: true,
                filter: "\"PesticideApplicationProductId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CreatedByUserId",
                table: "InventoryTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_FarmId_ProductId_BatchNumber",
                table: "InventoryTransactions",
                columns: new[] { "FarmId", "ProductId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_FarmId_ProductId_CreatedAtUtc",
                table: "InventoryTransactions",
                columns: new[] { "FarmId", "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReferenceCheck",
                table: "InventoryTransactions",
                columns: new[] { "FarmId", "ProductId", "BatchNumber", "TransactionType", "Reference" },
                filter: "\"Reference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplicationProducts_PesticideApplicationId",
                table: "PesticideApplicationProducts",
                column: "PesticideApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplicationProducts_ProductId",
                table: "PesticideApplicationProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_CreatedByUserId",
                table: "PesticideApplications",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FarmId_ApplicationDateUtc",
                table: "PesticideApplications",
                columns: new[] { "FarmId", "ApplicationDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FarmId_FieldId",
                table: "PesticideApplications",
                columns: new[] { "FarmId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FarmId_Status_ApplicationDateUtc",
                table: "PesticideApplications",
                columns: new[] { "FarmId", "Status", "ApplicationDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FieldCropId",
                table: "PesticideApplications",
                column: "FieldCropId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FieldId",
                table: "PesticideApplications",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_WeatherRecordId",
                table: "PesticideApplications",
                column: "WeatherRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_WorkerId",
                table: "PesticideApplications",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_RegistrationNumber",
                table: "Products",
                column: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherRecords_FarmId_RecordedAtUtc",
                table: "WeatherRecords",
                columns: new[] { "FarmId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherRecords_FieldId_FarmId_RecordedAtUtc",
                table: "WeatherRecords",
                columns: new[] { "FieldId", "FarmId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_FarmId_WorkerCode",
                table: "Workers",
                columns: new[] { "FarmId", "WorkerCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ComplianceRecords");

            migrationBuilder.DropTable(
                name: "FarmNotifications");

            migrationBuilder.DropTable(
                name: "FarmRolePermissions");

            migrationBuilder.DropTable(
                name: "FarmUserRoles");

            migrationBuilder.DropTable(
                name: "InventoryThresholds");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "FarmRoles");

            migrationBuilder.DropTable(
                name: "FarmUsers");

            migrationBuilder.DropTable(
                name: "PesticideApplicationProducts");

            migrationBuilder.DropTable(
                name: "PesticideApplications");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "FieldCrops");

            migrationBuilder.DropTable(
                name: "WeatherRecords");

            migrationBuilder.DropTable(
                name: "Workers");

            migrationBuilder.DropTable(
                name: "Crops");

            migrationBuilder.DropTable(
                name: "Fields");

            migrationBuilder.DropTable(
                name: "Farms");
        }
    }
}
