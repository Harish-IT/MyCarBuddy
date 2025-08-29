namespace MyCarBuddy.API.Models
{
    public class rolehaspermissionsModel
    {
        public int permission_id { get; set; }
        public int role_id { get; set; }
    }

    public class Updaterolehaspermissions
    {
        public int id { get; set; }
        public int permission_id { get; set; }
        public int role_id { get; set; }

        

    }
}
