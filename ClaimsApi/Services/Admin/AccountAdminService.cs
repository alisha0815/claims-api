using ClaimsApi.Auth;
using ClaimsApi.Data;
using ClaimsApi.Dtos.Admin;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Services.Admin;

public class AccountAdminService(AppDbContext db) : IAccountAdminService
{
    public async Task<IReadOnlyList<HandlerResponse>> GetAllHandlersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var query = db.Users.AsNoTracking();

        var handlers = query
            .Where(u => u.Role == Roles.Handler)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new HandlerResponse(u.Id, u.Username, u.CreatedAt));

        return await handlers.ToListAsync(cancellationToken);
    }
}
