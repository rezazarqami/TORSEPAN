using MediatR;

namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed class RegisterProductionOperationCommandHandler
    : IRequestHandler<RegisterProductionOperationCommand, Guid>
{
    public async Task<Guid> Handle(
        RegisterProductionOperationCommand request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return Guid.NewGuid();
    }
}