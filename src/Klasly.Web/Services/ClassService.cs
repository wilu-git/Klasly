using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Klasly.Web.Services;

/// <summary>Shapes class pages from several API calls (classes, terms, subjects, accounts). No business rules here.</summary>
public class ClassService(
    IClassRepository classes, ITermRepository terms, ISubjectRepository subjects, IAccountRepository accounts) : IClassService
{
    public async Task<ClassListViewModel> GetListAsync(int? termId, CancellationToken ct)
    {
        var termList = await terms.ListAsync(ct);
        // No filter chosen → the active term; if no term is active, show all (decision 3).
        var selected = termId ?? termList.FirstOrDefault(t => t.IsActive)?.TermId ?? 0;

        var options = termList
            .Select(t => new SelectListItem(t.IsActive ? $"{t.Name} (active)" : t.Name, t.TermId.ToString(), t.TermId == selected))
            .Prepend(new SelectListItem("All terms", "0", selected == 0))
            .ToList();

        return new ClassListViewModel(await classes.ListAsync(selected == 0 ? null : selected, ct), options, selected);
    }

    public async Task<ClassFormViewModel> GetNewFormAsync(CancellationToken ct)
    {
        var model = new ClassFormViewModel();
        var termList = await LoadOptionsAsync(model, ct);
        model.TermId = termList.FirstOrDefault(t => t.IsActive)?.TermId; // new classes usually belong to the current term
        return model;
    }

    public async Task<ClassFormViewModel> GetEditFormAsync(int id, CancellationToken ct)
    {
        var c = await classes.GetAsync(id, ct);
        var model = new ClassFormViewModel
        {
            Id = c.ClassId, SubjectId = c.SubjectId, TeacherId = c.TeacherId, TermId = c.TermId,
            Section = c.Section, Schedule = c.Schedule, Room = c.Room,
        };
        await LoadOptionsAsync(model, ct);
        return model;
    }

    public Task FillOptionsAsync(ClassFormViewModel model, CancellationToken ct) => LoadOptionsAsync(model, ct);

    private async Task<List<TermResponse>> LoadOptionsAsync(ClassFormViewModel model, CancellationToken ct)
    {
        var subjectList = await subjects.ListAsync(ct);
        var teacherList = await accounts.ListAsync("Teacher", ct);
        var termList = await terms.ListAsync(ct);

        model.SubjectOptions = subjectList.Select(s => new SelectListItem($"{s.Code} – {s.Title}", s.SubjectId.ToString())).ToList();
        // shortcut: inactive teachers are left out, so a class whose teacher was deactivated must pick a new one on edit.
        model.TeacherOptions = teacherList.Where(t => t.IsActive && t.TeacherId is not null)
            .Select(t => new SelectListItem(t.FullName, t.TeacherId.ToString())).ToList();
        model.TermOptions = termList.Select(t => new SelectListItem(t.IsActive ? $"{t.Name} (active)" : t.Name, t.TermId.ToString())).ToList();
        return termList;
    }

    public Task CreateAsync(ClassFormViewModel model, CancellationToken ct) => classes.CreateAsync(ToRequest(model), ct);

    public Task UpdateAsync(ClassFormViewModel model, CancellationToken ct) => classes.UpdateAsync(model.Id!.Value, ToRequest(model), ct);

    public async Task<ClassDetailsViewModel> GetDetailsAsync(int id, CancellationToken ct)
    {
        var c = await classes.GetAsync(id, ct);
        var enrollments = await classes.ListEnrollmentsAsync(id, ct);
        var students = await accounts.ListAsync("Student", ct);

        // Dropped students can be picked again; Active ones are already in the class.
        var activeIds = enrollments.Where(e => e.Status == "Active").Select(e => e.StudentId).ToHashSet();
        var options = students
            .Where(s => s.IsActive && s.StudentId is { } sid && !activeIds.Contains(sid))
            .Select(s => new SelectListItem($"{s.FullName} ({s.StudentNumber})", s.StudentId.ToString()))
            .ToList();

        return new ClassDetailsViewModel(c, enrollments, options);
    }

    public Task EnrollAsync(int classId, int studentId, CancellationToken ct) =>
        classes.EnrollAsync(new EnrollmentRequest(classId, studentId), ct);

    public Task DropAsync(int enrollmentId, CancellationToken ct) => classes.DropAsync(enrollmentId, ct);

    private static ClassRequest ToRequest(ClassFormViewModel m) =>
        new(m.SubjectId!.Value, m.TeacherId!.Value, m.TermId!.Value, m.Section, m.Schedule, m.Room);
}
