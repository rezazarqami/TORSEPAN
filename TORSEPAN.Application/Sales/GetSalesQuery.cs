Exit code: 0
Wall time: 0.5 seconds
Output:
using MediatR;
namespace TORSEPAN.Application.Sales;
public sealed record GetSalesQuery : IRequest<IReadOnlyList<SaleItemResponse>>;
public sealed class SaleItemResponse { public Guid HandpanId{get;set;} public bool IsBowl{get;set;} public string ItemType{get;set;}="ساز"; public string SerialNumber{get;set;}=""; public string BuyerName{get;set;}=""; public DateTime SoldAt{get;set;} public string SoldBy{get;set;}=""; public string MaterialName{get;set;}=""; public string ScaleName{get;set;}=""; public string TopBowlCode{get;set;}=""; public string BottomBowlCode{get;set;}=""; }

