using ClaimsApi.Dtos.Admin;

namespace ClaimsApi.Services.Admin;

public interface IAccountAdminService
{
    Task<IReadOnlyList<HandlerResponse>> GetAllHandlersAsync(
        CancellationToken cancellationToken = default
    );
}
