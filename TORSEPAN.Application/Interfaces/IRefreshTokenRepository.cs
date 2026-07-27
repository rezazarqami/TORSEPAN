using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);

    Task AddAsync(RefreshToken refreshToken);

    void Update(RefreshToken refreshToken);
}