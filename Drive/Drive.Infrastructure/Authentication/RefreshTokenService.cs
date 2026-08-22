using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using Npgsql;
using Drive.Application.Features.Auth.Models;

namespace Drive.Infrastructure.Authentication;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly DriveDbContext _dbContext;

    public RefreshTokenService(DriveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(tokenBytes);

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(rawToken)));

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            RevokedAt = null,
            ReplacedByTokenId = null
        };

        _dbContext.Set<RefreshToken>().Add(refreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    public async Task<Guid?> ValidateAsync(
    string refreshToken,
    CancellationToken cancellationToken = default)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        var token = await _dbContext.Set<RefreshToken>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (token is null)
        {
            return null;
        }

        if (token.RevokedAt.HasValue)
        {
            return null;
        }

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return token.UserId;
    }

    public async Task<RefreshTokenRotationResult?> RotateAsync(
    string refreshToken,
    CancellationToken cancellationToken = default)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var currentToken = await _dbContext.Set<RefreshToken>()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (currentToken is null)
        {
            return null;
        }

        if (currentToken.RevokedAt.HasValue)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        if (currentToken.ExpiresAt <= now)
        {
            return null;
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var newRawToken = Convert.ToBase64String(tokenBytes);

        var newTokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(newRawToken)));

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = currentToken.UserId,
            TokenHash = newTokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
            RevokedAt = null,
            ReplacedByTokenId = null
        };

        currentToken.RevokedAt = now;
        currentToken.ReplacedByTokenId = newToken.Id;

        _dbContext.Set<RefreshToken>().Add(newToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new RefreshTokenRotationResult
        {
            UserId = currentToken.UserId,
            RefreshToken = newRawToken
        };
    }

    public async Task<bool> RevokeAsync(
    string refreshToken,
    CancellationToken cancellationToken = default)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        var token = await _dbContext.Set<RefreshToken>()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (token is null)
        {
            return false;
        }

        if (token.RevokedAt.HasValue)
        {
            return false;
        }

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}