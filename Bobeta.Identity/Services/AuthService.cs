using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Auth;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Identity.Services;

/// <summary>Implements phone-based auth: send OTP, verify OTP, register player (create player + wallet, issue JWT).</summary>
public class AuthService : IAuthService
{
    private const string OtpMessageFormat = "Your Bobeta verification code is {0}. Do not share this code.";

    private readonly OtpService _otpService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ISmsService? _smsService;

    public AuthService(
        OtpService otpService,
        IJwtTokenService jwtTokenService,
        IPlayerRepository playerRepository,
        IWalletRepository walletRepository,
        ISmsService? smsService = null)
    {
        _otpService = otpService;
        _jwtTokenService = jwtTokenService;
        _playerRepository = playerRepository;
        _walletRepository = walletRepository;
        _smsService = smsService;
    }

    /// <inheritdoc />
    public async Task SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var code = await _otpService.GenerateAndStoreOtpAsync(phoneNumber, cancellationToken);
        var normalized = PhoneNumberHelper.Normalize(phoneNumber);
        var message = string.Format(OtpMessageFormat, code);
        if (_smsService != null)
            await _smsService.SendOtpAsync(normalized, message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VerifyOtpResult> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        var valid = await _otpService.ValidateOtpAsync(phoneNumber, code, cancellationToken);
        if (!valid) return new VerifyOtpResult(false, null);
        var player = await _playerRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        var token = player != null ? _jwtTokenService.GenerateToken(player.Id, player.PlayerName) : null;
        return new VerifyOtpResult(true, token);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterPlayerAsync(string phoneNumber, string playerName, CancellationToken cancellationToken = default)
    {
        var existing = await _playerRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("Player already registered with this phone number.");

        var player = new Player
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            PlayerName = playerName,
            Language = "en",
            CreatedAt = DateTime.UtcNow,
            IsVerified = true,
            Status = PlayerStatus.Active
        };
        await _playerRepository.AddAsync(player, cancellationToken);

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Balance = 0,
            LockedBalance = 0,
            UpdatedAt = DateTime.UtcNow
        };
        await _walletRepository.AddAsync(wallet, cancellationToken);

        var token = _jwtTokenService.GenerateToken(player.Id, player.PlayerName);
        return new AuthResponse(token, player.Id, player.PlayerName, true);
    }
}
