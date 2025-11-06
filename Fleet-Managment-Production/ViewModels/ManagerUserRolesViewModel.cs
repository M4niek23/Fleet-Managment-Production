namespace Fleet_Managment_Production.ViewModels
{
    public class RoleSelectionViewModel
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }

    }

    public class ManagerUserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<RoleSelectionViewModel> Roles { get; set; }
    }
}
