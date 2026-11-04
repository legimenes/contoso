namespace Core.Abstractions.Identity;

public interface IAppClientProvider
{
    Task<bool> ExistsByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken);
    Task<CreatedAppClientCredential> CreateAsync(Guid merchantId, string displayName, CancellationToken cancellationToken);
    Task<AppClientCredential?> FindByClientIdAsync(string clientId, CancellationToken cancellationToken);
    Task<bool> ValidateSecretAsync(string clientId, string clientSecret, CancellationToken cancellationToken);
    Task<CreatedAppClientCredential?> RotateSecretAsync(string clientId, CancellationToken cancellationToken);
}
