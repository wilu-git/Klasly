using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Klasly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicTerms",
                columns: table => new
                {
                    TermId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicTerms", x => x.TermId);
                    table.CheckConstraint("CK_AcademicTerms_Dates", "[EndDate] > [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                    table.CheckConstraint("CK_Subjects_Units", "[Units] BETWEEN 1 AND 10");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FcmToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegisteredDeviceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN (N'Admin', N'Teacher', N'Student', N'Parent')");
                });

            migrationBuilder.CreateTable(
                name: "Guardians",
                columns: table => new
                {
                    GuardianId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.GuardianId);
                    table.ForeignKey(
                        name: "FK_Guardians_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DedupKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.CheckConstraint("CK_Notifications_Attempts", "[Attempts] >= 0");
                    table.CheckConstraint("CK_Notifications_Channel", "[Channel] IN (N'Push', N'Email')");
                    table.CheckConstraint("CK_Notifications_Status", "[Status] IN (N'Pending', N'Accepted', N'Failed')");
                    table.CheckConstraint("CK_Notifications_Type", "[Type] IN (N'Absence', N'AtRisk', N'GradePosted')");
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StudentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    YearLevel = table.Column<int>(type: "int", nullable: false),
                    Program = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                    table.CheckConstraint("CK_Students_YearLevel", "[YearLevel] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_Students_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                    table.ForeignKey(
                        name: "FK_SystemSettings_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EmployeeNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.TeacherId);
                    table.ForeignKey(
                        name: "FK_Teachers_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "StudentGuardians",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    GuardianId = table.Column<int>(type: "int", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReceivesAlerts = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGuardians", x => new { x.StudentId, x.GuardianId });
                    table.ForeignKey(
                        name: "FK_StudentGuardians_Guardians",
                        column: x => x.GuardianId,
                        principalTable: "Guardians",
                        principalColumn: "GuardianId");
                    table.ForeignKey(
                        name: "FK_StudentGuardians_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Room = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.ClassId);
                    table.ForeignKey(
                        name: "FK_Classes_AcademicTerms",
                        column: x => x.TermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "TermId");
                    table.ForeignKey(
                        name: "FK_Classes_Subjects",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId");
                    table.ForeignKey(
                        name: "FK_Classes_Teachers",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "TeacherId");
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    AssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DatePosted = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.AssessmentId);
                    table.CheckConstraint("CK_Assessments_MaxScore", "[MaxScore] > 0");
                    table.CheckConstraint("CK_Assessments_Weight", "[WeightPercent] > 0 AND [WeightPercent] <= 100");
                    table.ForeignKey(
                        name: "FK_Assessments_Classes",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                });

            migrationBuilder.CreateTable(
                name: "ClassSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    LateAfterMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    QrNonce = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    ClosedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSessions", x => x.SessionId);
                    table.CheckConstraint("CK_ClassSessions_Late", "[LateAfterMinutes] BETWEEN 0 AND 120");
                    table.CheckConstraint("CK_ClassSessions_Status", "[Status] IN (N'Scheduled', N'Open', N'Closed', N'Finalized', N'Cancelled')");
                    table.CheckConstraint("CK_ClassSessions_Times", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_ClassSessions_Classes",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    EnrollmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.EnrollmentId);
                    table.CheckConstraint("CK_Enrollments_Status", "[Status] IN (N'Active', N'Dropped')");
                    table.ForeignKey(
                        name: "FK_Enrollments_Classes",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                    table.ForeignKey(
                        name: "FK_Enrollments_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    GradeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.GradeId);
                    table.CheckConstraint("CK_Grades_Score", "[Score] >= 0");
                    table.ForeignKey(
                        name: "FK_Grades_Assessments",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId");
                    table.ForeignKey(
                        name: "FK_Grades_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    MarkedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Source = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MarkedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.AttendanceId);
                    table.CheckConstraint("CK_AttendanceRecords_ScanTime", "[Source] <> N'Scan' OR [ScannedAt] IS NOT NULL");
                    table.CheckConstraint("CK_AttendanceRecords_Source", "[Source] IN (N'Scan', N'Auto', N'Manual')");
                    table.CheckConstraint("CK_AttendanceRecords_Status", "[Status] IN (N'Present', N'Late', N'Absent', N'Excused')");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_ClassSessions",
                        column: x => x.SessionId,
                        principalTable: "ClassSessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_MarkedBy",
                        column: x => x.MarkedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "RiskFlags",
                columns: table => new
                {
                    RiskFlagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    AbsenceRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    WeightedAverage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsAtRisk = table.Column<bool>(type: "bit", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskFlags", x => x.RiskFlagId);
                    table.CheckConstraint("CK_RiskFlags_AbsenceRate", "[AbsenceRate] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_RiskFlags_Average", "[WeightedAverage] IS NULL OR [WeightedAverage] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_RiskFlags_Enrollments",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "EnrollmentId");
                });

            migrationBuilder.CreateTable(
                name: "ExcuseRequests",
                columns: table => new
                {
                    ExcuseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcuseRequests", x => x.ExcuseId);
                    table.CheckConstraint("CK_ExcuseRequests_Status", "[Status] IN (N'Pending', N'Approved', N'Rejected')");
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_Attendance",
                        column: x => x.AttendanceId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "AttendanceId");
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.InsertData(
                table: "AcademicTerms",
                columns: new[] { "TermId", "EndDate", "IsActive", "Name", "StartDate" },
                values: new object[] { 1, new DateOnly(2026, 12, 19), true, "1st Sem 2026-27", new DateOnly(2026, 8, 17) });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "SubjectId", "Code", "Title", "Units" },
                values: new object[,]
                {
                    { 1, "ELECT3", "Mobile Application Development", 3 },
                    { 2, "IT302", "Software Engineering", 3 }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { "AbsenceThreshold", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, "0.20" },
                    { "MaxEmailAttempts", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, "5" },
                    { "PassingGrade", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, "75" },
                    { "QrStepSeconds", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, "30" },
                    { "TimeZoneId", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, "Asia/Manila" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "Email", "FcmToken", "FullName", "IsActive", "PasswordHash", "RegisteredDeviceId", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "admin@klasly.test", null, "Klasly Administrator", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Admin", null },
                    { 2, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "rosa.villanueva@klasly.test", null, "Rosa Villanueva", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Teacher", null },
                    { 3, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "mark.delossantos@klasly.test", null, "Mark Delos Santos", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Teacher", null },
                    { 4, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "aira.mendoza@klasly.test", null, "Aira Mendoza", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Student", null },
                    { 5, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "bryan.lacsamana@klasly.test", null, "Bryan Lacsamana", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Student", null },
                    { 6, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "carla.dimaano@klasly.test", null, "Carla Dimaano", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Student", null },
                    { 7, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "dennis.orpilla@klasly.test", null, "Dennis Orpilla", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Student", null },
                    { 8, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "ellen.bautista@klasly.test", null, "Ellen Bautista", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Student", null },
                    { 9, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "francis.nuqui@klasly.test", null, "Francis Nuqui", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Student", null },
                    { 10, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "marites.mendoza@klasly.test", null, "Marites Mendoza", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Parent", null },
                    { 11, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "rodel.dimaano@klasly.test", null, "Rodel Dimaano", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Parent", null },
                    { 12, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), "linda.bautista@klasly.test", null, "Linda Bautista", true, "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==", null, "Parent", null }
                });

            migrationBuilder.InsertData(
                table: "Guardians",
                columns: new[] { "GuardianId", "ContactEmail", "ContactNumber", "UserId" },
                values: new object[,]
                {
                    { 1, "marites.mendoza@klasly.test", "+639170000001", 10 },
                    { 2, "rodel.dimaano@klasly.test", "+639170000002", 11 },
                    { 3, "linda.bautista@klasly.test", "+639170000003", 12 }
                });

            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "StudentId", "Program", "StudentNumber", "UserId", "YearLevel" },
                values: new object[,]
                {
                    { 1, "BSCpE", "2026-00001", 4, 3 },
                    { 2, "BSCpE", "2026-00002", 5, 3 },
                    { 3, "BSCpE", "2026-00003", 6, 3 },
                    { 4, "BSCpE", "2026-00004", 7, 3 },
                    { 5, "BSCpE", "2026-00005", 8, 3 },
                    { 6, "BSCpE", "2026-00006", 9, 3 }
                });

            migrationBuilder.InsertData(
                table: "Teachers",
                columns: new[] { "TeacherId", "Department", "EmployeeNo", "UserId" },
                values: new object[,]
                {
                    { 1, "College of Computing", "EMP-1001", 2 },
                    { 2, "College of Computing", "EMP-1002", 3 }
                });

            migrationBuilder.InsertData(
                table: "Classes",
                columns: new[] { "ClassId", "Room", "Schedule", "Section", "SubjectId", "TeacherId", "TermId" },
                values: new object[,]
                {
                    { 1, "CL-301", "MWF 07:30-09:00", "BSCpE 3-A", 1, 1, 1 },
                    { 2, "CL-205", "TTh 13:00-14:30", "BSCpE 3-A", 2, 2, 1 }
                });

            migrationBuilder.InsertData(
                table: "StudentGuardians",
                columns: new[] { "GuardianId", "StudentId", "ReceivesAlerts", "Relationship" },
                values: new object[,]
                {
                    { 1, 1, true, "Mother" },
                    { 1, 2, true, "Mother" },
                    { 2, 3, true, "Father" },
                    { 2, 4, true, "Father" },
                    { 3, 5, true, "Guardian" },
                    { 3, 6, true, "Guardian" }
                });

            migrationBuilder.InsertData(
                table: "Enrollments",
                columns: new[] { "EnrollmentId", "ClassId", "StudentId" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 1, 2 },
                    { 3, 1, 3 },
                    { 4, 1, 4 },
                    { 5, 1, 5 },
                    { 6, 1, 6 },
                    { 7, 2, 1 },
                    { 8, 2, 2 },
                    { 9, 2, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_AcademicTerms_Name",
                table: "AcademicTerms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AcademicTerms_OneActive",
                table: "AcademicTerms",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UQ_Assessments_Class_Title",
                table: "Assessments",
                columns: new[] { "ClassId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_MarkedBy",
                table: "AttendanceRecords",
                column: "MarkedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Student",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceRecords_Session_Student",
                table: "AttendanceRecords",
                columns: new[] { "SessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TeacherId",
                table: "Classes",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TermId",
                table: "Classes",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "UQ_Classes_Subject_Section_Term",
                table: "Classes",
                columns: new[] { "SubjectId", "Section", "TermId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_ClassId",
                table: "ClassSessions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_Status_EndTime",
                table: "ClassSessions",
                columns: new[] { "Status", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrollments_Class_Student",
                table: "Enrollments",
                columns: new[] { "ClassId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_ReviewedBy",
                table: "ExcuseRequests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_ExcuseRequests_Attendance",
                table: "ExcuseRequests",
                column: "AttendanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Student",
                table: "Grades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UQ_Grades_Assessment_Student",
                table: "Grades",
                columns: new[] { "AssessmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Guardians_UserId",
                table: "Guardians",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Outbox",
                table: "Notifications",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Notifications_DedupKey",
                table: "Notifications",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskFlags_Enrollment_Latest",
                table: "RiskFlags",
                columns: new[] { "EnrollmentId", "ComputedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGuardians_GuardianId",
                table: "StudentGuardians",
                column: "GuardianId");

            migrationBuilder.CreateIndex(
                name: "UQ_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Students_UserId",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedBy",
                table: "SystemSettings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_Teachers_EmployeeNo",
                table: "Teachers",
                column: "EmployeeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Teachers_UserId",
                table: "Teachers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcuseRequests");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RiskFlags");

            migrationBuilder.DropTable(
                name: "StudentGuardians");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Guardians");

            migrationBuilder.DropTable(
                name: "ClassSessions");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "AcademicTerms");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
