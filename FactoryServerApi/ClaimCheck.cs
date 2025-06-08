using FactoryServerApi.Http.Responses;

namespace FactoryServerApi;

internal record struct ClaimCheck(bool IsClaimed, string? InitialAdminAuthToken, FactoryServerError? Error);
