using Bobeta.Application.Common;
using Bobeta.Application.Configuration;
using Bobeta.Application.DTOs.Auth;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bobeta.Identity.Services;

/// <summary>Implements phone-based auth: send OTP, verify OTP, register player (create player + wallet, issue JWT).</summary>
public class AuthService : IAuthService
{
    private const string OtpMessageFormatFallback = "Your Bobeta verification code is {0}. Do not share this code.";

    private readonly OtpService _otpService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ISmsService? _smsService;
    private readonly IOtpRateLimitService? _otpRateLimitService;
    private readonly IOptions<CountrySettings>? _countrySettings;
    private readonly ISmsTemplateProvider? _smsTemplateProvider;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        OtpService otpService,
        IJwtTokenService jwtTokenService,
        IPlayerRepository playerRepository,
        IWalletRepository walletRepository,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        ISmsService? smsService = null,
        IOtpRateLimitService? otpRateLimitService = null,
        IOptions<CountrySettings>? countrySettings = null,
        ISmsTemplateProvider? smsTemplateProvider = null)
    {
        _otpService = otpService;
        _jwtTokenService = jwtTokenService;
        _playerRepository = playerRepository;
        _walletRepository = walletRepository;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
        _logger = logger;
        _smsService = smsService;
        _otpRateLimitService = otpRateLimitService;
        _countrySettings = countrySettings;
        _smsTemplateProvider = smsTemplateProvider;
    }

    /// <inheritdoc />
    public async Task SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default, string? clientIp = null)
    {
        if (_otpRateLimitService != null)
        {
            var (allowed, errorMessage) = await _otpRateLimitService.CheckAndRecordAsync(phoneNumber, clientIp, cancellationToken);
            if (!allowed)
                throw new InvalidOperationException(errorMessage ?? "Too many OTP requests. Please try again later.");
        }

        var code = await _otpService.GenerateAndStoreOtpAsync(phoneNumber, cancellationToken);
        var normalized = PhoneNumberHelper.Normalize(phoneNumber);
        if (ShouldSkipSmsForConfiguredDemoNumber(normalized))
        {
            if (_hostEnvironment.IsProduction())
            {
                _logger.LogWarning(
                    "Static demo OTP enabled in Production: SMS skipped for Phone={Phone}. Set DemoAuth:EnableStaticOtp=false when testing is complete.",
                    normalized);
            }
            else
            {
                _logger.LogInformation("Skipping SMS for demo number (use DemoAuth static OTP). Phone={Phone}", normalized);
            }

            return;
        }

        var template = _smsTemplateProvider?.GetOtpMessageTemplate(_countrySettings?.Value?.DefaultLanguage) ?? OtpMessageFormatFallback;
        var message = string.Format(template, code);
        if (_smsService != null)
            await _smsService.SendOtpAsync(normalized, message, cancellationToken);
    }

    /// <summary>No SMS when static OTP is enabled for this number (any environment when configured).</summary>
    private bool ShouldSkipSmsForConfiguredDemoNumber(string normalizedPhone) =>
        DemoEnvironmentHelper.IsStaticOtpEnabled(_configuration) &&
        DemoEnvironmentHelper.IsConfiguredDemoPhoneNumber(_configuration, normalizedPhone);

    /// <inheritdoc />
    public async Task<VerifyOtpResult> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        var (valid, errorMessage) = await _otpService.ValidateOtpAsync(phoneNumber, code, cancellationToken);
        if (!valid) return new VerifyOtpResult(Valid: false, ErrorMessage: errorMessage);
        var player = await _playerRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        var token = player != null ? _jwtTokenService.GenerateToken(player.Id, player.PlayerName) : null;
        return new VerifyOtpResult(Valid: true, Token: token, PlayerId: player?.Id, PlayerName: player?.PlayerName);
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
            CountryCode = CountryCatalog.ResolveCountryCodeFromPhone(phoneNumber),
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
