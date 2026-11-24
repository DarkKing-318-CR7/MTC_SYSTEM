using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs.Subscriptions;

namespace MTC_System.Services
{
    public interface ISubscriptionService
    {
        Task<ApiResponse<List<SubscriptionResponseDto>>> GetAllAsync();

        Task<ApiResponse<SubscriptionResponseDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<SubscriptionResponseDto>> CreateAsync(
            SubscriptionCreateDto dto);

        Task<ApiResponse<SubscriptionResponseDto>> UpdateAsync(
            Guid id,
            SubscriptionUpdateDto dto);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}