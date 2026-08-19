using System.Collections.Generic;

namespace BackupSystem.Server.ViewModels
{

    public class UserListViewModel
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public IList<string>? Roles { get; set; }
    }

    public class CreateUserViewModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }



}