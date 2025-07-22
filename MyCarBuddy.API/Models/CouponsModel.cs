using Microsoft.JSInterop;
using System;

namespace MyCarBuddy.API.Models
{
    public class CouponsModel
    {
        public int?CouponID { get; set; }
        public string Code {  get; set; }
        public string Description { get; set; }
        public  string DiscountType {  get; set; }
        public decimal DiscountValue {  get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTill { get; set; }
        public int? MaxUsagePerUser {  get; set; }
        public bool? IsActive { get; set; }
        public int? CreatedBy {  get; set; }
        public int? ModifiedBy { get; set; }
    }
}
