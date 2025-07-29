namespace MyCarBuddy.API.Models
{
    //Insert Model Class
    public class CustomerAddressesModel
    {
        public int AddressID {  get; set; }
        public int CustID {  get; set; }
        public string AddressLine1 {  get; set; }
        public string AddressLine2 { get; set; }
        public int StateID {  get; set; }
        public int CityID {  get; set; }
        public decimal Pincode {  get; set; }
        public decimal Latitude {  get; set; }
        public decimal Longitude { get; set; }
        public bool IsDefault {  get; set; }
        public int? CreatedBy {  get; set; } 

        public int? ModifiedBy { get; set; }

        public bool? IsActive { get; set; }

    }
    //update model class
    public class UpdateCustomerAddresses
    {
        public int AddressID { get; set; }
        public int CustID { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public int StateID { get; set; }
        public int CityID { get; set; }
        public decimal Pincode { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsDefault { get; set; }
        public int? CreatedBy { get; set; }

        public int? ModifiedBy { get; set; }

        public bool? IsActive { get; set; }
    }
}
