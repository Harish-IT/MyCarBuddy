using Microsoft.VisualBasic;
using System;

namespace MyCarBuddy.API.Models
{

    //Insert Model
    public class LeaveRequestModel
    {
        public int? LeaveId {  get; set; }
        public int?TechID { get; set; }

        public DateTime? FromDate{ get; set; }
        public DateTime? ToDate {  get; set; }
        public string LeaveReason {  get; set; }
        public int? RequestedToId { get; set; }
        public int? Status { get; set; }
    }



}
