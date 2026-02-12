using System.Collections.Generic;
using HISWEBAPI.DTO;
using HISWEBAPI.Models;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IStoreRepository
    {
        ServiceResult<CreateUpdateVendorMasterResponse> CreateUpdateVendorMaster(CreateUpdateVendorMasterRequest request, AllGlobalValues globalValues);

        ServiceResult<IEnumerable<VendorMasterModel>> GetVendorMasterList(int? vendorId = null,int? isActive = null);
    }
}