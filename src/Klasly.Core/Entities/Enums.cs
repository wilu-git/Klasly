namespace Klasly.Core.Entities;

// Every enum below is stored in SQL Server as nvarchar guarded by a CHECK constraint
// (guide Section 27.7), never as an int.

public enum UserRole { Admin, Teacher, Student, Parent }

public enum EnrollmentStatus { Active, Dropped }

public enum SessionStatus { Scheduled, Open, Closed, Finalized, Cancelled }

public enum AttendanceStatus { Present, Late, Absent, Excused }

public enum AttendanceSource { Scan, Auto, Manual }

public enum ExcuseStatus { Pending, Approved, Rejected }

public enum NotificationChannel { Push, Email }

public enum NotificationType { Absence, AtRisk, GradePosted }

public enum NotificationStatus { Pending, Accepted, Failed }
