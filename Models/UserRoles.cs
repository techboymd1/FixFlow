namespace FixFlow.Models;

public static class UserRoles
{
    public const string OwnerAdmin = "Owner/Admin";
    public const string FrontDesk = "Front Desk";
    public const string Technician = "Technician";

    public static readonly IReadOnlyList<string> All =
    [
        OwnerAdmin,
        FrontDesk,
        Technician
    ];
}
