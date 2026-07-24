using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Production;

public interface IProductionEngine
{
    bool CanMoveTo(
        ProductionStage currentStage,
        ProductionStage nextStage);

    void MoveTo(
        Handpan handpan,
        ProductionStage nextStage);

    ProductionTransition? GetTransition(
        ProductionStage currentStage);
}