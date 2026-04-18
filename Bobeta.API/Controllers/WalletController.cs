using Bobeta.Application.DTOs.Wallet;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Bobeta.API.Controllers;

/// <summary>API for wallet: balance, deposit, withdraw, transaction history. Requires authentication.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController(
    IWalletService walletService,
    IHostEnvironment hostEnvironment,
    IConfiguration configuration) : ControllerBase
{
    private readonly IWalletService _walletService = walletService;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly IConfiguration _configuration = configuration;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Gets the current balance and locked balance for the authenticated player.</summary>
    [HttpGet("balance")]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var balance = await _walletService.GetBalanceAsync(PlayerId, cancellationToken);
        return Ok(balance);
    }

    /// <summary>
    /// Credits the player's wallet without a payment provider. Intended for local/demo testing only;
    /// disabled in production and unless <c>DemoAuth:EnableTestWalletDeposits</c> is true.
    /// </summary>
    [HttpPost("deposit")]
    public async Task<ActionResult<WalletTransactionDto>> Deposit([FromBody] DepositRequest request, CancellationToken cancellationToken)
    {
        if (_hostEnvironment.IsProduction())
        {
            return new ContentResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                Content = "Direct wallet deposits are not enabled in production.",
                ContentType = "text/plain; charset=utf-8"
            };
        }

        if (!_configuration.GetValue("DemoAuth:EnableTestWalletDeposits", false))
        {
            return new ContentResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                Content =
                    "Direct wallet deposits are disabled. Set DemoAuth:EnableTestWalletDeposits to true in appsettings (non-production only).",
                ContentType = "text/plain; charset=utf-8"
            };
        }

        var tx = await _walletService.DepositAsync(PlayerId, request.Amount, cancellationToken);
        return Ok(tx);
    }

    /// <summary>Withdraws the specified amount from the player's wallet.</summary>
    [HttpPost("withdraw")]
    public async Task<ActionResult<WalletTransactionDto>> Withdraw([FromBody] WithdrawRequest request, CancellationToken cancellationToken)
    {
        var tx = await _walletService.WithdrawAsync(PlayerId, request.Amount, cancellationToken);
        return Ok(tx);
    }

    /// <summary>Returns paginated transaction history for the authenticated player.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<WalletTransactionDto>>> GetTransactions([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var list = await _walletService.GetTransactionsAsync(PlayerId, skip, take, cancellationToken);
        return Ok(list);
    }
}
