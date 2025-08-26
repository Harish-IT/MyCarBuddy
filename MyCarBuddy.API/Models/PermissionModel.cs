namespace MyCarBuddy.API.Models
{
    public class PermissionModel
    {
        public string name {  get; set; }
        public string page {  get; set; }
    }
    public class UpdatePermission
    {
        public int Id {  get; set; }
        public string name { get; set; }
        public string page { get; set; }
    }
}
