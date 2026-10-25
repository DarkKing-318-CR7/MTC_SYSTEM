using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services
{
    public interface IFacilityService
    {
        Task<ApiResponse<List<FacilityTreeNodeDto>>> GetTreeAsync(Guid organizationId);
        Task<ApiResponse<List<FacilityResponseDto>>> GetByOrganizationAsync(Guid organizationId);
        Task<ApiResponse<FacilityResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<FacilityResponseDto>> CreateAsync(CreateFacilityDto dto);
        Task<ApiResponse<FacilityResponseDto>> UpdateAsync(Guid id, UpdateFacilityDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
