namespace PestiTrackBackend.Security;

public static class PermissionCodes
{
    // Farms
    public const string FarmsView = "farms.view";
    public const string FarmsManage = "farms.manage";

    // Fields
    public const string FieldsView = "fields.view";
    public const string FieldsManage = "fields.manage";

    // Crops
    public const string CropsView = "crops.view";
    public const string CropsManage = "crops.manage";

    // Products
    public const string ProductsView = "products.view";
    public const string ProductsManage = "products.manage";

    // Pesticide applications
    public const string ApplicationsView = "applications.view";
    public const string ApplicationsCreate = "applications.create";
    public const string ApplicationsApprove = "applications.approve";

   // Inventory
    public const string InventoryView = "inventory.view";
    public const string InventoryManage = "inventory.manage";
    public const string InventoryIssue = "inventory.issue";

    // Workers
    public const string WorkersView = "workers.view";
    public const string WorkersManage = "workers.manage";

    // Weather
    public const string WeatherView = "weather.view";
    public const string WeatherManage = "weather.manage";

    // Reports
    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";

    // Farm user assignments
    public const string FarmUsersView = "farm-users.view";
    public const string FarmUsersManage = "farm-users.manage";

    // Audit
    public const string AuditView = "audit.view";

    // Compliance
    public const string ComplianceView = "compliance.view";
    public const string ComplianceManage = "compliance.manage";
}