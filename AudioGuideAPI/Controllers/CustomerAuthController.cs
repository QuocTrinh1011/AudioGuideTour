using AudioGuideAPI.Data;
using AudioGuideAPI.Helpers;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerAuthController : ControllerBase
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    private readonly AppDbContext _context;

    public CustomerAuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<CustomerSessionResponse>> Login([FromBody] CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Vui lòng nhập số điện thoại hoặc email và mật khẩu.");
        }

        var normalizedIdentifier = request.Identifier.Trim();
        var normalizedEmail = normalizedIdentifier.ToLowerInvariant();

        var account = await _context.CustomerAccounts
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.Phone == normalizedIdentifier || x.Email.ToLower() == normalizedEmail,
                cancellationToken);

        if (account == null || !PasswordHashHelper.VerifyPassword(request.Password.Trim(), account.PasswordHash, account.PasswordSalt))
        {
            return Unauthorized("Tài khoản hoặc mật khẩu không đúng.");
        }

        if (!account.IsPaid || !account.IsActive)
        {
            return BadRequest("Tài khoản này chưa được kích hoạt. Vui lòng hoàn tất thanh toán gói đăng ký trước khi đăng nhập.");
        }

        account.SessionToken = Guid.NewGuid().ToString("N");
        account.SessionExpiresAt = DateTime.UtcNow.Add(SessionLifetime);
        account.LastLoginAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapSession(account));
    }

    [HttpGet("session")]
    public async Task<ActionResult<CustomerSessionResponse>> ValidateSession([FromQuery] string accountId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized();
        }

        var account = await _context.CustomerAccounts
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == accountId && x.SessionToken == token, cancellationToken);

        if (account == null)
        {
            return Unauthorized();
        }

        if (account.SessionExpiresAt.HasValue && account.SessionExpiresAt.Value < DateTime.UtcNow)
        {
            account.SessionToken = string.Empty;
            account.SessionExpiresAt = null;
            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return Unauthorized();
        }

        if (!account.IsPaid || !account.IsActive)
        {
            return Unauthorized();
        }

        account.SessionExpiresAt = DateTime.UtcNow.Add(SessionLifetime);
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapSession(account));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] CustomerLogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccountId) || string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Ok();
        }

        var account = await _context.CustomerAccounts
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.SessionToken == request.SessionToken, cancellationToken);

        if (account == null)
        {
            return Ok();
        }

        account.SessionToken = string.Empty;
        account.SessionExpiresAt = null;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    private static CustomerSessionResponse MapSession(CustomerAccount account) => new()
    {
        AccountId = account.Id,
        RegistrationId = account.RegistrationId,
        FullName = account.FullName,
        Phone = account.Phone,
        Email = account.Email,
        PreferredLanguage = account.PreferredLanguage,
        SessionToken = account.SessionToken,
        SessionExpiresAt = account.SessionExpiresAt,
        IsPaid = account.IsPaid,
        IsActive = account.IsActive,
        Status = account.Status
    };
}
