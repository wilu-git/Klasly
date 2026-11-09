using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Klasly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AcademicTerms",
                schema: "dbo",
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
                    table.CheckConstraint("CK_AcademicTerms_Dates", "EndDate > StartDate");
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                schema: "dbo",
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
                    table.CheckConstraint("CK_Subjects_Units", "Units BETWEEN 1 AND 10");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
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
                    table.CheckConstraint("CK_Users_Role", "Role IN (N'Admin', N'Teacher', N'Student', N'Parent')");
                });

            migrationBuilder.CreateTable(
                name: "Guardians",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "dbo",
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
                    table.CheckConstraint("CK_Notifications_Attempts", "Attempts >= 0");
                    table.CheckConstraint("CK_Notifications_Channel", "Channel IN (N'Push', N'Email')");
                    table.CheckConstraint("CK_Notifications_Status", "Status IN (N'Pending', N'Accepted', N'Failed')");
                    table.CheckConstraint("CK_Notifications_Type", "Type IN (N'Absence', N'AtRisk', N'GradePosted')");
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "dbo",
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
                    table.CheckConstraint("CK_Students_YearLevel", "YearLevel BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_Students_Users",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "StudentGuardians",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Guardians",
                        principalColumn: "GuardianId");
                    table.ForeignKey(
                        name: "FK_StudentGuardians_Students",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "AcademicTerms",
                        principalColumn: "TermId");
                    table.ForeignKey(
                        name: "FK_Classes_Subjects",
                        column: x => x.SubjectId,
                        principalSchema: "dbo",
                        principalTable: "Subjects",
                        principalColumn: "SubjectId");
                    table.ForeignKey(
                        name: "FK_Classes_Teachers",
                        column: x => x.TeacherId,
                        principalSchema: "dbo",
                        principalTable: "Teachers",
                        principalColumn: "TeacherId");
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                schema: "dbo",
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
                    table.CheckConstraint("CK_Assessments_MaxScore", "MaxScore > 0");
                    table.CheckConstraint("CK_Assessments_Weight", "WeightPercent > 0 AND WeightPercent <= 100");
                    table.ForeignKey(
                        name: "FK_Assessments_Classes",
                        column: x => x.ClassId,
                        principalSchema: "dbo",
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                });

            migrationBuilder.CreateTable(
                name: "ClassSessions",
                schema: "dbo",
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
                    table.CheckConstraint("CK_ClassSessions_Late", "LateAfterMinutes BETWEEN 0 AND 120");
                    table.CheckConstraint("CK_ClassSessions_Status", "Status IN (N'Scheduled', N'Open', N'Closed', N'Finalized', N'Cancelled')");
                    table.CheckConstraint("CK_ClassSessions_Times", "EndTime > StartTime");
                    table.ForeignKey(
                        name: "FK_ClassSessions_Classes",
                        column: x => x.ClassId,
                        principalSchema: "dbo",
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                schema: "dbo",
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
                    table.CheckConstraint("CK_Enrollments_Status", "Status IN (N'Active', N'Dropped')");
                    table.ForeignKey(
                        name: "FK_Enrollments_Classes",
                        column: x => x.ClassId,
                        principalSchema: "dbo",
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                    table.ForeignKey(
                        name: "FK_Enrollments_Students",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                schema: "dbo",
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
                    table.CheckConstraint("CK_Grades_Score", "Score >= 0");
                    table.ForeignKey(
                        name: "FK_Grades_Assessments",
                        column: x => x.AssessmentId,
                        principalSchema: "dbo",
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId");
                    table.ForeignKey(
                        name: "FK_Grades_Students",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                schema: "dbo",
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
                    table.CheckConstraint("CK_AttendanceRecords_ScanTime", "Source <> N'Scan' OR ScannedAt IS NOT NULL");
                    table.CheckConstraint("CK_AttendanceRecords_Source", "Source IN (N'Scan', N'Auto', N'Manual')");
                    table.CheckConstraint("CK_AttendanceRecords_Status", "Status IN (N'Present', N'Late', N'Absent', N'Excused')");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_ClassSessions",
                        column: x => x.SessionId,
                        principalSchema: "dbo",
                        principalTable: "ClassSessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_MarkedBy",
                        column: x => x.MarkedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Students",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "RiskFlags",
                schema: "dbo",
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
                    table.CheckConstraint("CK_RiskFlags_AbsenceRate", "AbsenceRate BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_RiskFlags_Average", "WeightedAverage IS NULL OR WeightedAverage BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_RiskFlags_Enrollments",
                        column: x => x.EnrollmentId,
                        principalSchema: "dbo",
                        principalTable: "Enrollments",
                        principalColumn: "EnrollmentId");
                });

            migrationBuilder.CreateTable(
                name: "ExcuseRequests",
                schema: "dbo",
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
                    table.CheckConstraint("CK_ExcuseRequests_Status", "Status IN (N'Pending', N'Approved', N'Rejected')");
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_Attendance",
                        column: x => x.AttendanceId,
                        principalSchema: "dbo",
                        principalTable: "AttendanceRecords",
                        principalColumn: "AttendanceId");
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "SystemSettings",
                columns: new[] { "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { "AbsenceThreshold", null, null, "0.20" },
                    { "MaxEmailAttempts", null, null, "5" },
                    { "PassingGrade", null, null, "75" },
                    { "QrStepSeconds", null, null, "30" },
                    { "TimeZoneId", null, null, "Asia/Manila" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_AcademicTerms_Name",
                schema: "dbo",
                table: "AcademicTerms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AcademicTerms_OneActive",
                schema: "dbo",
                table: "AcademicTerms",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UQ_Assessments_Class_Title",
                schema: "dbo",
                table: "Assessments",
                columns: new[] { "ClassId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_MarkedBy",
                schema: "dbo",
                table: "AttendanceRecords",
                column: "MarkedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Student",
                schema: "dbo",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceRecords_Session_Student",
                schema: "dbo",
                table: "AttendanceRecords",
                columns: new[] { "SessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TeacherId",
                schema: "dbo",
                table: "Classes",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TermId",
                schema: "dbo",
                table: "Classes",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "UQ_Classes_Subject_Section_Term",
                schema: "dbo",
                table: "Classes",
                columns: new[] { "SubjectId", "Section", "TermId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_ClassId",
                schema: "dbo",
                table: "ClassSessions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_Status_EndTime",
                schema: "dbo",
                table: "ClassSessions",
                columns: new[] { "Status", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                schema: "dbo",
                table: "Enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrollments_Class_Student",
                schema: "dbo",
                table: "Enrollments",
                columns: new[] { "ClassId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_ReviewedBy",
                schema: "dbo",
                table: "ExcuseRequests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_ExcuseRequests_Attendance",
                schema: "dbo",
                table: "ExcuseRequests",
                column: "AttendanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Student",
                schema: "dbo",
                table: "Grades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UQ_Grades_Assessment_Student",
                schema: "dbo",
                table: "Grades",
                columns: new[] { "AssessmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Guardians_UserId",
                schema: "dbo",
                table: "Guardians",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Outbox",
                schema: "dbo",
                table: "Notifications",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                schema: "dbo",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Notifications_DedupKey",
                schema: "dbo",
                table: "Notifications",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskFlags_Enrollment_Latest",
                schema: "dbo",
                table: "RiskFlags",
                columns: new[] { "EnrollmentId", "ComputedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGuardians_GuardianId",
                schema: "dbo",
                table: "StudentGuardians",
                column: "GuardianId");

            migrationBuilder.CreateIndex(
                name: "UQ_Students_StudentNumber",
                schema: "dbo",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Students_UserId",
                schema: "dbo",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Subjects_Code",
                schema: "dbo",
                table: "Subjects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedBy",
                schema: "dbo",
                table: "SystemSettings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_Teachers_EmployeeNo",
                schema: "dbo",
                table: "Teachers",
                column: "EmployeeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Teachers_UserId",
                schema: "dbo",
                table: "Teachers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                schema: "dbo",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcuseRequests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Grades",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RiskFlags",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "StudentGuardians",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AttendanceRecords",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Assessments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Enrollments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Guardians",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ClassSessions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Classes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AcademicTerms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Subjects",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Teachers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");
        }
    }
}
