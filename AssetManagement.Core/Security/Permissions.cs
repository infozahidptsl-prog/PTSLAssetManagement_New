namespace AssetManagement.Core.Security;

public static class Permissions
{
    public static class Devices
    {
        public const string View = "Permissions.Devices.View";
        public const string Create = "Permissions.Devices.Create";
        public const string Edit = "Permissions.Devices.Edit";
        public const string Delete = "Permissions.Devices.Delete";
        public const string Assign = "Permissions.Devices.Assign";
        public const string Return = "Permissions.Devices.Return";
        public const string Repair = "Permissions.Devices.Repair";
    }

    public static class Employees
    {
        public const string View = "Permissions.Employees.View";
        public const string Create = "Permissions.Employees.Create";
        public const string Edit = "Permissions.Employees.Edit";
        public const string Delete = "Permissions.Employees.Delete";
    }

    public static class Settings
    {
        public const string View = "Permissions.Settings.View";
        public const string Manage = "Permissions.Settings.Manage";
    }

    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Manage = "Permissions.Users.Manage";
        public const string ManageRoles = "Permissions.Users.ManageRoles";
    }

    public static class Reports
    {
        public const string View = "Permissions.Reports.View";
    }

    public static class Requisitions
    {
        public const string View   = "Permissions.Requisitions.View";
        public const string Create = "Permissions.Requisitions.Create";
        public const string Approve = "Permissions.Requisitions.Approve";
    }

    public static List<string> GetAllPermissions()
    {
        return new List<string>
        {
            Devices.View, Devices.Create, Devices.Edit, Devices.Delete, Devices.Assign, Devices.Return, Devices.Repair,
            Employees.View, Employees.Create, Employees.Edit, Employees.Delete,
            Settings.View, Settings.Manage,
            Users.View, Users.Manage, Users.ManageRoles,
            Reports.View,
            Requisitions.View, Requisitions.Create, Requisitions.Approve
        };
    }
}
