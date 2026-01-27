using System.Collections.Generic;
using HISWEBAPI.DTO;
using HISWEBAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IHomeRepository
    {
        ServiceResult<IEnumerable<BranchModel>> GetActiveBranchList();
        ServiceResult<IEnumerable<PickListModel>> GetPickListMaster(string fieldName);
        ServiceResult<AllGlobalValues> GetAllGlobalValues();
        ServiceResult<string> ClearAllCache();
        ServiceResult<IEnumerable<CountryMasterModel>> GetCountryMaster(int? isActive);
        ServiceResult<IEnumerable<StateMasterModel>> GetStateMaster(int countryId, int? isActive);
        ServiceResult<IEnumerable<DistrictMasterModel>> GetDistrictMaster(int stateId, int? isActive);
        ServiceResult<IEnumerable<CityMasterModel>> GetCityMaster(int districtId, int? isActive);
        ServiceResult<IEnumerable<PincodeMasterModel>> GetPincodeMaster(int cityId, int? isActive);
        ServiceResult<IEnumerable<InsuranceCompanyModel>> GetAllInsuranceCompanyList();
        ServiceResult<IEnumerable<CorporateModel>> GetCorporateListByInsuranceCompanyId(int? insuranceCompanyId, int? isActive);
        ServiceResult<DTO.FileStreamResult> GetFile(string filePath);
        ServiceResult<FileBase64Result> GetFileAsBase64(string filePath);
        ServiceResult<FileExistsResult> CheckFileExists(string filePath);

    }
}