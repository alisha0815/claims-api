namespace ClaimsApi.Dtos;

public record RefreshResult(RefreshStatus Status, LoginResponse? Response);
