namespace Identity.Core.Permissions
{
    public  enum UserPermissions : ulong
    {
        Read = 1,
        Edit = 2,
        ChangePassword = 4,
        Delete = 8,
    }
}
