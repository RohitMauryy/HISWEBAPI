using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO
{
    public class CreateUpdateVendorMasterRequest
    {
        public int VendorId { get; set; } = 0;

        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [StringLength(15, ErrorMessage = "Type cannot exceed 15 characters")]
        public string Type { get; set; }

        [Required(ErrorMessage = "Vendor name is required")]
        [StringLength(256, ErrorMessage = "Vendor name cannot exceed 256 characters")]
        public string VendorName { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(15, ErrorMessage = "Contact number cannot exceed 15 characters")]
        public string ContactNo { get; set; }

        [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$",ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }

        [StringLength(100, ErrorMessage = "DL number cannot exceed 100 characters")]
        public string DLNO { get; set; }

        [Required(ErrorMessage = "GSTIN number is required")]
        [StringLength(100, ErrorMessage = "GSTIN number cannot exceed 100 characters")]
        public string GSTINNo { get; set; }

        public string Address { get; set; }

        public int? CountryId { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public int? CityId { get; set; }

        [StringLength(100, ErrorMessage = "Mapping branch cannot exceed 100 characters")]

        [Required(ErrorMessage = "Mapping BranchId is required")]
        public string MappingBranch { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }

        [Required(ErrorMessage = "Pincode is required")]
        public int Pincode { get; set; }

    }

    public class CreateUpdateVendorMasterResponse
    {
        public int VendorId { get; set; }
    }

    public class GetVendorMasterRequest
    {
        public int? VendorId { get; set; }
        public int? IsActive { get; set; }
    }
}