namespace MyCarBuddy.API.Models
{

    //insert model
    public class CityModel
    {
        public int CityID { get; set; }
        public int StateID {  get; set; }
        public string CityName { get; set; }
        public bool IsActive { get; set; }


    }

    //Update Model

    public class UpdateCity
    {
        public int CityID { get; set; }
        public int StateID { get; set; }
        public string CityName { get; set; }
        public bool IsActive { get; set; }

    }
}
