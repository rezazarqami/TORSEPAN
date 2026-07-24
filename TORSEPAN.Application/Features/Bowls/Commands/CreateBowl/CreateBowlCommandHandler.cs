using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

public sealed class CreateBowlCommandHandler
    : IRequestHandler<CreateBowlCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICodeGenerator _codeGenerator;

    public CreateBowlCommandHandler(
        IUnitOfWork unitOfWork,
        ICodeGenerator codeGenerator)
    {
        _unitOfWork = unitOfWork;
        _codeGenerator = codeGenerator;
    }

    public async Task<Result<Guid>> Handle(
        CreateBowlCommand request,
        CancellationToken cancellationToken)
    {
        var productionCode =
            await _codeGenerator.GenerateProductionCodeAsync(cancellationToken);

        var bowl = new Bowl(
            productionCode,
            request.BowlType,
            request.HasNotes,
            request.InstrumentType,
            request.NoteCount);

        await _unitOfWork.Bowls.AddAsync(bowl);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(bowl.Id);
    }
}