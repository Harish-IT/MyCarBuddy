namespace MyCarBuddy.API.Models
{
    public class CustomerVehiclesModel
    {
        public int? VehicleID { get; set; }

        public int? CustID { get; set; }
        public string VehicleNumber { get; set; }
        public string YearOfPurchase { get; set; }

        public string EngineType {  get; set; }
        public string KilometersDriven {  get; set; }
         public string TransmissionType { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }

    }
}
