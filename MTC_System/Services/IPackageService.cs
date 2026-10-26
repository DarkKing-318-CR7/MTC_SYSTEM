using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs.Packages;

namespace MTC_System.Services
{
    public interface IPackageService
    {
        Task<ApiResponse<List<PackageResponseDto>>> GetAllAsync();

        Task<ApiResponse<PackageResponseDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<PackageResponseDto>> CreateAsync(
            PackageCreateDto dto);

        Task<ApiResponse<PackageResponseDto>> UpdateAsync(
            Guid id,
            PackageUpdateDto dto);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}