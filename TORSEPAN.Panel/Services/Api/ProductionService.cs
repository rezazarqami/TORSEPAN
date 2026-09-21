using TORSEPAN.Panel.Models;

namespace TORSEPAN.Panel.Services.Api;

public sealed class ProductionService
{
    private readonly ApiClient _api;

    public ProductionService(ApiClient api)
    {
        _api = api;
    }

    public async Task<ProductionDashboardDto?> GetDashboardAsync(DateTime? selectedDate = null)
    {
        return await _api.GetAsync<ProductionDashboardDto>(
            "production/dashboard" + (selectedDate.HasValue ? $"?date={selectedDate:yyyy-MM-dd}" : string.Empty));
    }

    public async Task<IReadOnlyList<ProductionStageItemDto>> GetQueueAsync()
    {
        var result =
            await _api.GetAsync<IReadOnlyList<ProductionStageItemDto>>(
                "production/queue");

        return result ?? [];
    }

    public async Task<bool> ChangeStageAsync(ChangeProductionStageRequest request)
    {
        return await _api.PostAsync<ChangeProductionStageRequest, bool>(
            "production/change-stage",
            request);
    }

    public async Task<IReadOnlyList<WarehouseHandpanDto>> GetWarehouseAsync()
    {
        return await _api.GetAsync<List<WarehouseHandpanDto>>(
            "production/warehouse") ?? [];
    }
    public Task<WarehouseHandpanDto?> GetWarehouseDetailsAsync(Guid id) => _api.GetAsync<WarehouseHandpanDto>($"production/warehouse/{id}/details");
    public async Task<List<WarehouseGalleryPhotoDto>> GetWarehouseGalleryAsync(IEnumerable<Guid> ids) => await _api.PostAsync<object,List<WarehouseGalleryPhotoDto>>("production/warehouse/gallery",new{HandpanIds=ids}) ?? [];
    public async Task<SaleOperationResult?> SellAsync(Guid id,string? buyerName,string? buyerPhoneNumber,decimal? price,string? destination,Guid? partyId,bool isPaid,DateTime? dueDate,bool activateWarranty=false,Guid? leadSourceId=null,Guid? referrerId=null) => await _api.PostAsync<object,SaleOperationResult>($"production/{id}/sell",new { BuyerName=buyerName,BuyerPhoneNumber=buyerPhoneNumber,Price=price,Destination=destination,PartyId=partyId,IsPaid=isPaid,DueDate=dueDate,ActivateWarranty=activateWarranty,IsExportSale=false,LeadSourceId=leadSourceId,ReferrerId=referrerId });
    public async Task<SaleOperationResult?> SellManyAsync(IReadOnlyCollection<Guid> ids,string? buyerName,string? buyerPhoneNumber,decimal? price,decimal? receivedAmount,bool isExportSale,string? destination,Guid? partyId,bool activateWarranty=false,Guid? leadSourceId=null,Guid? referrerId=null) => await _api.PostAsync<object,SaleOperationResult>("production/sales/bulk",new { HandpanIds=ids,Sale=new { BuyerName=buyerName,BuyerPhoneNumber=buyerPhoneNumber,Price=price,ReceivedAmount=receivedAmount,IsExportSale=isExportSale,Destination=destination,PartyId=partyId,IsPaid=false,DueDate=(DateTime?)null,ActivateWarranty=activateWarranty,LeadSourceId=leadSourceId,ReferrerId=referrerId } });
    public async Task UpdateSaleAsync(Guid id,string? buyerName,string? buyerPhoneNumber,decimal? price,decimal? receivedAmount,bool isExportSale,string? destination,Guid? partyId,bool activateWarranty=false,Guid? leadSourceId=null,Guid? referrerId=null) => await _api.PutAsync<object,object?>($"production/{id}/sale",new { BuyerName=buyerName,BuyerPhoneNumber=buyerPhoneNumber,Price=price,ReceivedAmount=receivedAmount,IsExportSale=isExportSale,Destination=destination,PartyId=partyId,IsPaid=false,DueDate=(DateTime?)null,ActivateWarranty=activateWarranty,LeadSourceId=leadSourceId,ReferrerId=referrerId });
    public async Task<IReadOnlyList<SaleItemDto>> GetSalesAsync() => await _api.GetAsync<List<SaleItemDto>>("production/sales") ?? [];
    public Task RollbackAsync(Guid id) => _api.PostAsync<object, object?>($"production/{id}/rollback", new { });
    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"production/{id}");
}
