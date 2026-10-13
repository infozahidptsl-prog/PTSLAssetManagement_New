using System.Threading.Tasks;

namespace AssetManagement.Core.Interfaces;

public interface IAiValidationService
{
    Task ValidatePurchaseAsync(MappingValidationResult<CanonicalPurchaseDto> mappedResult);
    Task ValidateDeviceRegistrationAsync(MappingValidationResult<CanonicalDeviceDto> mappedResult);
}
