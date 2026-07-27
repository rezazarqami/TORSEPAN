using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserPreview;

public sealed record GetUserPreviewQuery(Guid UserId)
    : IRequest<UserPreviewDto?>;