using System.ComponentModel.DataAnnotations;

namespace Klasly.Web.ViewModels;

/// <summary>The "Start session" form on a class page. The session starts when the form is posted; times are Manila time.</summary>
public class StartSessionViewModel
{
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Pick the end time."), Display(Name = "Ends at")]
    public DateTime? EndTime { get; set; }

    [Required(ErrorMessage = "Enter the minutes until Late."), Range(0, 120, ErrorMessage = "Use 0 to 120 minutes."),
     Display(Name = "Late after (minutes)")]
    public int? LateAfterMinutes { get; set; } = 15;
}
