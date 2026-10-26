using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services
{
    public interface IOrganizationService
    {
        Task<ApiResponse<List<OrganizationResponseDto>>> GetAllAsync();
        Task<ApiResponse<OrganizationResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<OrganizationResponseDto>> CreateAsync(CreateOrganizationDto dto);
        Task<ApiResponse<OrganizationResponseDto>> UpdateAsync(Guid id, UpdateOrganizationDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
