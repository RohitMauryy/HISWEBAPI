namespace HISWEBAPI.Models
{
    public class VendorMasterModel
    {
        public int VendorId { get; set; }
        public int TypeId { get; set; }
        public string Type { get; set; }
        public string VendorName { get; set; }
        public string ContactNo { get; set; }
        public string Email { get; set; }
        public string DLNO { get; set; }
        public string GSTINNo { get; set; }
        public string Address { get; set; }
        public string FullAddress { get; set; }
        public int? CountryId { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public int? CityId { get; set; }
        public int? Pincode { get; set; }
        public string MappingBranch { get; set; }
        public int IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedOn { get; set; }
    }
}