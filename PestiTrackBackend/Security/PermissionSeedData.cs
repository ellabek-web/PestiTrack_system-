using PestiTrackBackend.Models.Authorization;

namespace PestiTrackBackend.Security;

public static class PermissionSeedData
{
    public static readonly Permission[] All =
    [
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Code = PermissionCodes.FarmsView, Name = "View farms", Description = "View farm information." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Code = PermissionCodes.FarmsManage, Name = "Manage farms", Description = "Create and update farm information." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Code = PermissionCodes.FieldsView, Name = "View fields", Description = "View field information." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Code = PermissionCodes.FieldsManage, Name = "Manage fields", Description = "Create and update fields." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Code = PermissionCodes.CropsView, Name = "View crops", Description = "View crop information." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Code = PermissionCodes.CropsManage, Name = "Manage crops", Description = "Create and update crop information." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Code = PermissionCodes.ProductsView, Name = "View products", Description = "View pesticide product information." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Code = PermissionCodes.ProductsManage, Name = "Manage products", Description = "Create and update pesticide products." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), Code = PermissionCodes.ApplicationsView, Name = "View applications", Description = "View pesticide application records." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"), Code = PermissionCodes.ApplicationsCreate, Name = "Create applications", Description = "Create pesticide application records." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000011"), Code = PermissionCodes.ApplicationsApprove, Name = "Approve applications", Description = "Approve pesticide application records." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"), Code = PermissionCodes.InventoryView, Name = "View inventory", Description = "View inventory records." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000013"), Code = PermissionCodes.InventoryManage, Name = "Manage inventory", Description = "Manage inventory transactions." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000014"), Code = PermissionCodes.WorkersView, Name = "View workers", Description = "View worker information." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000015"), Code = PermissionCodes.WorkersManage, Name = "Manage workers", Description = "Create and update worker information." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000016"), Code = PermissionCodes.WeatherView, Name = "View weather", Description = "View weather records." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000017"), Code = PermissionCodes.WeatherManage, Name = "Manage weather", Description = "Create and update weather records." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000018"), Code = PermissionCodes.ReportsView, Name = "View reports", Description = "View reports." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000019"), Code = PermissionCodes.ReportsExport, Name = "Export reports", Description = "Export report data." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000020"), Code = PermissionCodes.FarmUsersView, Name = "View farm users", Description = "View users assigned to a farm." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000021"), Code = PermissionCodes.FarmUsersManage, Name = "Manage farm users", Description = "Manage farm user assignments and farm roles." },

        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000022"), Code = PermissionCodes.AuditView, Name = "View audit trail", Description = "View farm audit records." }
    ];
}