namespace TORSEPAN.Panel.Services.Api;

public abstract class BaseApiService(ApiClient apiClient)
{
    protected readonly ApiClient Api = apiClient;
}