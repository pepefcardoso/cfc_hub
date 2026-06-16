using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Compliance;

namespace CFCHub.Infrastructure.ExternalServices.Detran.Adapters;

public class SpDetranAdapter : IDetranAdapter
{
    private readonly HttpClient _httpClient;

    public SpDetranAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("detran-sp");
    }

    public async Task<CnhStatusResult> GetCnhStatusAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/cnh/{cpf}/status", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            return CnhStatusResult.Unavailable;
        }

        var result = await response.Content.ReadFromJsonAsync<CnhStatusResult>(cancellationToken: cancellationToken);
        return result ?? CnhStatusResult.Unavailable;
    }
}
