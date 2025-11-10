using System.Collections.Generic;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;

namespace HISWEBAPI.Repositories.Interfaces
{
  
    public interface IPageConfigRepository
    {
       
        ServiceResult<int> CreateUpdatePageConfig(PageConfigRequest request, AllGlobalValues globalValues);

        ServiceResult<IEnumerable<PageConfigResponse>> GetPageConfig(string configKey = null);
    }
}