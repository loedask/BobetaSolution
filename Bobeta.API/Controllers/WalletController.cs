using Bobeta.Application.DTOs.Wallet;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController(IWalletService walletService) : ControllerBase
{
    private readonly IWalletService _walletService = walletService;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    [HttpGet("balance")]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var balance = await _walletService.GetBalanceAsync(PlayerId, cancellationToken);
        return Ok(balance);
    }

    /// <summary>Credits the player's wallet (e.g. after MoMo deposit confirmation).</summary>
    [HttpPost("deposit")]
    public async Task<ActionResult<WalletTransactionDto>> Deposit([FromBody] DepositRequest request, CancellationToken cancellationToken)
    {
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
