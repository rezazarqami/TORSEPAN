using TORSEPAN.Domain.Production;

namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public static class RegisterProductionOperationMapper
{
    public static RegisterProductionOperationResponse ToResponse(
        Guid handpanId,
        string serialNumber,
        ProductionTransition transition)
    {
        return new RegisterProductionOperationResponse(
            handpanId,
            serialNumber,
            transition.CurrentStage.ToString(),
            transition.NextStage.ToString(),
            transition.Action.ToString());
    }
}