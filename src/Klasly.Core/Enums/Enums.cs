namespace Klasly.Core.Enums;

/// <summary>Stored as nvarchar; guarded by CK_Users_Role.</summary>
public enum UserRole
{
    Admin,
    Teacher,
    Student,
    Parent
}

/// <summary>Stored as nvarchar; guarded by CK_Enrollments_Status.</summary>
public enum EnrollmentStatus
{
    Active,
    Dropped
}

/// <summary>Stored as nvarchar; guarded by CK_ClassSessions_Status.</summary>
public enum SessionStatus
{
    Scheduled,
    Open,
    Closed,
    Finalized,
    Cancelled
}

/// <summary>Stored as nvarchar; guarded by CK_AttendanceRecords_Status.</summary>
public enum AttendanceStatus
{
    Present,
    Late,
    Absent,
    Excused
}

/// <summary>Stored as nvarchar; guarded by CK_AttendanceRecords_Source.</summary>
public enum AttendanceSource
{
    Scan,
    Auto,
    Manual
}

/// <summary>Stored as nvarchar; guarded by CK_ExcuseRequests_Status.</summary>
public enum ExcuseStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>Stored as nvarchar; guarded by CK_Notifications_Channel.</summary>
public enum NotificationChannel
{
    Push,
    Email
}

/// <summary>Stored as nvarchar; guarded by CK_Notifications_Status.</summary>
public enum NotificationStatus
{
    Pending,
    Accepted,
    Failed
}

/// <summary>Stored as nvarchar; guarded by CK_Notifications_Type.</summary>
public enum NotificationType
{
    Absence,
    AtRisk,
    GradePosted
}
