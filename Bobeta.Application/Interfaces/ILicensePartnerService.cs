using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

public interface ILicensePartnerService
{
  Task<IReadOnlyList<LicensePartnerListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<LicensePartnerListItemDto?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default);
  Task<LicensePartnerListItemDto> RegisterAsync(RegisterLicensePartnerRequest request, Guid createdById, CancellationToken cancellationToken = default);
  Task<LicensePartnerCountryDto> AssignCountryAsync(AssignLicensePartnerCountryRequest request, Guid createdById, CancellationToken cancellationToken = default);
  Task<LicensePartnerCountryDto> UpdateRevenueShareAsync(UpdateLicensePartnerRevenueShareRequest request, Guid createdById, CancellationToken cancellationToken = default);
}
