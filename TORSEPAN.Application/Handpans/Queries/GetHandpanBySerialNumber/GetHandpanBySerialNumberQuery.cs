using MediatR;
using TORSEPAN.Application.Handpans.Queries.GetAllHandpans;

namespace TORSEPAN.Application.Handpans.Queries.GetHandpanBySerialNumber;

public sealed record GetHandpanBySerialNumberQuery(string SerialNumber)
    : IRequest<HandpanDto?>;