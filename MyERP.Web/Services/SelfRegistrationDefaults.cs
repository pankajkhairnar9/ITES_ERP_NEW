namespace MyERP.Web.Services
{
    public static class SelfRegistrationDefaults
    {
        public const string RoleName = "SelfRegistrant";
        public const string RoleDescription = "Self-registered account with dashboard-only access.";
        public const string PermissionCode = "dashboard.view";

        public static bool IsSelfRegistrantRole(string name)
        {
            return string.Equals(name?.Trim(), RoleName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
