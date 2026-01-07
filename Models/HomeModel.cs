namespace HISWEBAPI.Models
{

    public class CountryMasterModel
    {
        public int CountryId { get; set; }
        public string CountryName { get; set; }
        public string Currency { get; set; }
        public decimal? ConversionFactor { get; set; }
        public int IsActive { get; set; }
    }

    public class StateMasterModel
    {
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public string StateName { get; set; }
        public int IsActive { get; set; }
    }

    public class DistrictMasterModel
    {
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public int DistrictId { get; set; }
        public string DistrictName { get; set; }
        public int IsActive { get; set; }
    }

    public class CityMasterModel
    {
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public int DistrictId { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int IsActive { get; set; }
    }

    public class PincodeMasterModel
    {
        public int CityId { get; set; }
        public int PincodeId { get; set; }
        public int Pincode { get; set; }
        public int IsActive { get; set; }
    }
}
