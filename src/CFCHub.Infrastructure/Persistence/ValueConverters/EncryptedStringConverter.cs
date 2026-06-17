using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CFCHub.Infrastructure.Persistence.ValueConverters;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IDataProtectionService dataProtectionService, ITenantContext tenantContext)
        : base(
            v => dataProtectionService.Encrypt(v, tenantContext.TenantId.ToString()),
            v => dataProtectionService.Decrypt(v, tenantContext.TenantId.ToString()),
            mappingHints: null)
    {
    }

    public EncryptedStringConverter()
        : base(v => v, v => v, mappingHints: null)
    {
    }
}
