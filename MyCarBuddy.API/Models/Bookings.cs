using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace MyCarBuddy.API.Models
{
    //public class BookingInsertDTO
    //{
    //    // BookingIds fields
    //    public string BookingTrackID { get; set; }
    //    public string BookingStatus { get; set; }

    //    // Bookings fields
    //    public int CustID { get; set; }
    //    public int? TechID { get; set; }
    //    public string TechFullName { get; set; }
    //    public string TechPhoneNumber { get; set; }
    //    public string CustFullName { get; set; }
    //    public string CustPhoneNumber { get; set; }
    //    public string CustEmail { get; set; }
    //    public int StateID { get; set; }
    //    public int CityID { get; set; }
    //    public string Pincode { get; set; }
    //    public string FullAddress { get; set; }
    //    public string Longitude { get; set; }
    //    public string Latitude { get; set; }
    //    public string PackageIds { get; set; }
    //    public string PackagePrice { get; set; }
    //    public decimal? TotalPrice { get; set; }
    //    public string CouponCode { get; set; }
    //    public decimal CouponAmount { get; set; }
    //    public string BookingFrom { get; set; }
    //    public string PaymentMethod { get; set; }
    //    public string Notes { get; set; }
    //    public DateTime BookingDate { get; set; }
    //    public string TimeSlot { get; set; }
    //    public bool IsOthers { get; set; }
    //    public string OthersFullName { get; set; }
    //    public string OthersPhoneNumber { get; set; }
    //    public int CreatedBy { get; set; }
    //    public DateTime? CreatedDate { get; set; }
    //    public int? ModifiedBy { get; set; }
    //    public DateTime? ModifiedDate { get; set; }
    //    public bool IsActive { get; set; }

    //    // Optional images
    //    public List<IFormFile> Images { get; set; }

    //    public int VechicleID { get; set; }
    //    public decimal? GSTAmount { get; set; }

    //}

    public class RazorpayPaymentRequest
    {
        public int BookingID { get; set; }
        public decimal AmountPaid { get; set; }
        public string RazorpayPaymentId { get; set; }
        public string RazorpayOrderId { get; set; }
        public string RazorpaySignature { get; set; }
    }
    public class RazorOrderRequest
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
    }
    public class RazorCombinedPaymentRequest
    {
        public int BookingID { get; set; }
        public int Amount { get; set; }

        // These 2 will come only after frontend completes Razorpay payment
        public string RazorpayPaymentId { get; set; }
        public string RazorpaySignature { get; set; }
    }

    public class BookingUpdateDTO
    {
        public int BookingID { get; set; }
        public string BookingStatus { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalPrice { get; set; }

        public int CustID { get; set; }

        public int TechID { get; set; }

        public int ModifiedBy { get; set; }
    }




    public class BookingInsertDTO
    {
        public string? BookingTrackID { get; set; }
        public int CustID { get; set; }
        public string? TechFullName { get; set; }
        public string? TechPhoneNumber { get; set; }
        public string? CustFullName { get; set; }
        public string? CustPhoneNumber { get; set; }
        public string? CustEmail { get; set; }
        public int StateID { get; set; }
        public int CityID { get; set; }
        public string? Pincode { get; set; }
        public string? FullAddress { get; set; }
        public string? BookingStatus { get; set; }
        public string? Longitude { get; set; }
        public string? Latitude { get; set; }
        public string? PackageIds { get; set; }
        public string? PackagePrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal GSTAmount { get; set; }
        public string? CouponCode { get; set; }
        public decimal CouponAmount { get; set; }
        public string? BookingFrom { get; set; }
        public string? PaymentMethod { get; set; }   // "COS" or "Online"
        public string? Notes { get; set; }
        public DateTime BookingDate { get; set; }
        public string? TimeSlot { get; set; }
        public bool IsOthers { get; set; }
        public string? OthersFullName { get; set; }
        public string? OthersPhoneNumber { get; set; }
        public int CreatedBy { get; set; }
        public int VechicleID { get; set; }

        public int? BookingOTP { get; set; }
        // public List<IFormFile> Images { get; set; } // if you use images
    }

    public class PaymentConfirmRequest
    {
        public int BookingID { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMode { get; set; } = "";      // "Razorpay" or "COS"

        // Razorpay fields (when PaymentMode == "Razorpay")
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpaySignature { get; set; }

        // COD fields (when PaymentMode == "COS")
        public string? TransactionId { get; set; }         // your internal receipt no.
    }

    public class CashPaymentFinalizeRequest
    {
        public int BookingID { get; set; }
        public decimal AmountPaid { get; set; }
        public string TransactionId { get; set; }
    }

    public class BookingStatusUpdate
    {
        public int BookingID { get; set; }
        public string BookingStatus {  get; set; }
    }

}
