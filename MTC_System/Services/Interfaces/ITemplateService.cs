using MTC_System.Common;
using MTC_System.DTOs.Templates;

namespace MTC_System.Services.Interfaces;

public interface ITemplateService
{
    Task<IReadOnlyList<TemplateResponse>> GetAllAsync(UserScopeContext userScope, Guid? organizationId, Guid? facilityId, CancellationToken cancellationToken);
    Task<TemplateResponse> GetByIdAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken);
    Task<TemplateResponse> CreateAsync(TemplateUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken);
    Task<TemplateResponse> UpdateAsync(Guid id, TemplateUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken);
}
