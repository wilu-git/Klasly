using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Klasly.Web.Services;

public class AccountService(IAccountRepository accounts) : IAccountService
{
    public Task<List<AccountResponse>> ListAsync(string? role, CancellationToken ct) => accounts.ListAsync(role, ct);

    public async Task<AccountFormViewModel> GetFormAsync(int userId, CancellationToken ct)
    {
        var a = await accounts.GetAsync(userId, ct);
        return new AccountFormViewModel
        {
            Id = a.UserId, Role = a.Role, FullName = a.FullName, Email = a.Email, IsActive = a.IsActive,
            StudentNumber = a.StudentNumber, YearLevel = a.YearLevel, Program = a.Program,
            EmployeeNo = a.EmployeeNo, Department = a.Department,
            ContactEmail = a.ContactEmail, ContactNumber = a.ContactNumber,
            ParentLinks = a.Role == "Parent" ? await GetParentLinksAsync(a.UserId, ct) : null,
        };
    }

    public async Task<ParentLinksViewModel> GetParentLinksAsync(int parentUserId, CancellationToken ct)
    {
        var linked = await accounts.ListLinkedStudentsAsync(parentUserId, ct);
        var students = await accounts.ListAsync("Student", ct);
        var linkedIds = linked.Select(l => l.StudentId).ToHashSet();

        var options = students
            .Where(s => s.IsActive && s.StudentId is { } id && !linkedIds.Contains(id))
            .Select(s => new SelectListItem($"{s.FullName} ({s.StudentNumber})", s.StudentId.ToString()))
            .ToList();

        return new ParentLinksViewModel(parentUserId, linked, options);
    }

    public Task CreateAsync(AccountFormViewModel m, CancellationToken ct) => accounts.CreateAsync(
        new CreateAccountRequest(m.FullName, m.Email, m.Password ?? string.Empty, m.Role,
            m.StudentNumber, m.YearLevel, m.Program, m.EmployeeNo, m.Department, m.ContactEmail, m.ContactNumber), ct);

    public Task UpdateAsync(AccountFormViewModel m, CancellationToken ct) => accounts.UpdateAsync(m.Id!.Value,
        new UpdateAccountRequest(m.FullName, m.Email, m.IsActive, m.NewPassword,
            m.StudentNumber, m.YearLevel, m.Program, m.EmployeeNo, m.Department, m.ContactEmail, m.ContactNumber), ct);

    public Task DeactivateAsync(int userId, CancellationToken ct) => accounts.DeactivateAsync(userId, ct);

    public Task ResetDeviceAsync(int userId, CancellationToken ct) => accounts.ResetDeviceAsync(userId, ct);

    public Task LinkStudentAsync(int parentUserId, int studentId, string? relationship, bool receivesAlerts, CancellationToken ct) =>
        accounts.LinkStudentAsync(parentUserId, new LinkStudentRequest(studentId, relationship, receivesAlerts), ct);

    public Task UnlinkStudentAsync(int parentUserId, int studentId, CancellationToken ct) =>
        accounts.UnlinkStudentAsync(parentUserId, studentId, ct);
}
