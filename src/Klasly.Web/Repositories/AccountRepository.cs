using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class AccountRepository(HttpClient http) : IAccountRepository
{
    public Task<List<AccountResponse>> ListAsync(string? role, CancellationToken ct) =>
        http.GetApiAsync<List<AccountResponse>>(role is null ? "api/accounts" : $"api/accounts?role={Uri.EscapeDataString(role)}", ct);

    public Task<AccountResponse> GetAsync(int userId, CancellationToken ct) =>
        http.GetApiAsync<AccountResponse>($"api/accounts/{userId}", ct);

    public Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct) =>
        http.PostApiAsync<AccountResponse>("api/accounts", request, ct);

    public Task<AccountResponse> UpdateAsync(int userId, UpdateAccountRequest request, CancellationToken ct) =>
        http.PutApiAsync<AccountResponse>($"api/accounts/{userId}", request, ct);

    public Task DeactivateAsync(int userId, CancellationToken ct) => http.DeleteApiAsync($"api/accounts/{userId}", ct);

    public Task<List<LinkedStudentResponse>> ListLinkedStudentsAsync(int parentUserId, CancellationToken ct) =>
        http.GetApiAsync<List<LinkedStudentResponse>>($"api/accounts/{parentUserId}/students", ct);

    public Task<LinkedStudentResponse> LinkStudentAsync(int parentUserId, LinkStudentRequest request, CancellationToken ct) =>
        http.PostApiAsync<LinkedStudentResponse>($"api/accounts/{parentUserId}/students", request, ct);

    public Task UnlinkStudentAsync(int parentUserId, int studentId, CancellationToken ct) =>
        http.DeleteApiAsync($"api/accounts/{parentUserId}/students/{studentId}", ct);
}
