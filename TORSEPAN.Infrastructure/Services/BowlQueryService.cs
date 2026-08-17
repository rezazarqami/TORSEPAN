using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Bowls.Queries.GetAllBowls;
using TORSEPAN.Application.Bowls.Queries.GetBowlById;
using TORSEPAN.Application.Common.Pagination;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Services;

public sealed class BowlQueryService : IBowlQueryService
{
    private readonly TORSEPANDbContext _context;

    public BowlQueryService(TORSEPANDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BowlDto>> GetAllAsync(
        PageRequest pageRequest, int? bowlType, bool? hasNotes,
        Guid? materialId, Guid? scaleId, int? stage,
        CancellationToken cancellationToken)
    {
        var bowls = _context.Bowls
            .AsNoTracking()
            .Where(x => !x.TopAssemblies.Any() && !x.BottomAssemblies.Any());
        if (bowlType.HasValue) bowls = bowls.Where(x => (int)x.BowlType == bowlType.Value);
        if (hasNotes.HasValue) bowls = bowls.Where(x => x.HasNotes == hasNotes.Value);
        if (materialId.HasValue) bowls = bowls.Where(x => x.MaterialId == materialId.Value);
        if (scaleId.HasValue) bowls = bowls.Where(x => x.ScaleId == scaleId.Value);
        if (stage.HasValue) bowls = bowls.Where(x => (int)x.Stage == stage.Value);
        var query = bowls
            .OrderByDescending(x => x.ProductionCode)
            .Select(x => new BowlDto
            {
                Id = x.Id,
                ProductionCode = x.ProductionCode,
                BowlType = (int)x.BowlType,
                HasNotes = x.HasNotes,
                InstrumentType = (int)x.InstrumentType,
                MaterialId = x.MaterialId,
                MaterialName = x.Material.Name,
                ScaleId = x.ScaleId,
                ScaleName = x.Scale != null ? x.Scale.Name : "نامشخص",
                Status = (int)x.Status,
                Stage = (int)x.Stage,
                Operations = x.ProductionEvents
                    .Where(e => e.Result == TORSEPAN.Domain.Enums.EventResult.Completed)
                    .OrderBy(e => e.EventDate)
                    .Select(e => new BowlOperationDto
                    {
                        Action = (int)e.Action,
                        PerformedBy = string.IsNullOrWhiteSpace(e.User.FullName)
                            ? e.User.UserName
                            : e.User.FullName,
                        PerformedAt = e.EventDate
                    })
                    .ToList()
            });

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BowlDto>(
            items,
            pageRequest.Page,
            pageRequest.PageSize,
            totalItems);
    }

    public async Task<BowlDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.Bowls
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BowlDetailDto
            {
                Id = x.Id,
                ProductionCode = x.ProductionCode,
                BowlType = (int)x.BowlType,
                HasNotes = x.HasNotes,
                InstrumentType = (int)x.InstrumentType,
                Status = (int)x.Status,
                Stage = (int)x.Stage
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
