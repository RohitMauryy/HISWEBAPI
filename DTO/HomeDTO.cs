using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO
{
    public class ResponseMessageRequest
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }
        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; }
        [Required(ErrorMessage = "AlertCode is required")]
        public string AlertCode { get; set; }
        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; }
        public bool IsActive { get; set; }
    }

    // Country Master Request
    public class GetCountryMasterRequest
    {
        public int? IsActive { get; set; } // null = all, 0 = inactive, 1 = active
    }

    // State Master Request
    public class GetStateMasterRequest
    {
        public int? IsActive { get; set; }

        [Required(ErrorMessage = "CountryId is required")]
        public int CountryId { get; set; }
    }

    // District Master Request
    public class GetDistrictMasterRequest
    {
        public int? IsActive { get; set; }

        [Required(ErrorMessage = "StateId is required")]
        public int StateId { get; set; }
    }

    // City Master Request
    public class GetCityMasterRequest
    {
        public int? IsActive { get; set; }

        [Required(ErrorMessage = "DistrictId is required")]
        public int DistrictId { get; set; }
    }

    public class GetPincodeMasterRequest
    {
        public int? IsActive { get; set; }

        [Required(ErrorMessage = "CityId is required")]
        public int CityId { get; set; }
    }

    public class InsuranceCompanyModel
    {
        public int InsuranceCompanyId { get; set; }
        public string InsuranceCompanyName { get; set; }
    }

    public class CorporateModel
    {
        public int CorporateId { get; set; }
        public string CorporateName { get; set; }
        public int InsuranceCompanyId { get; set; }
        public int IsActive { get; set; }
    }

    public class GetCorporateListRequest
    {
       
        public int? InsuranceCompanyId { get; set; }

     
        public int? IsActive { get; set; }
    }

    public class FileStreamResult
    {
        public FileStream FileStream { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }

    public class FileBase64Result
    {
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public double FileSizeMB { get; set; }
        public string Base64Data { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class FileExistsResult
    {
        public bool Exists { get; set; }
        public string FilePath { get; set; }
    }
}
