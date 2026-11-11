package com.klasly.app.data

import kotlinx.serialization.Serializable

// Bodies of the Klasly Web API (Klasly.Core/DTOs). Times are UTC strings, read with parseUtc.

@Serializable
data class LoginRequest(val email: String, val password: String)

/** [role] is Admin, Teacher, Student or Parent. */
@Serializable
data class LoginResponse(
    val token: String,
    val expiresAtUtc: String,
    val userId: Int,
    val fullName: String,
    val role: String,
)

@Serializable
data class RegisterDeviceRequest(val deviceId: String)

@Serializable
data class DeviceResponse(val deviceId: String)

@Serializable
data class ScanRequest(val qrToken: String, val deviceId: String)

/** [status] is "Present" or "Late", decided by the API. */
@Serializable
data class ScanResponse(
    val status: String,
    val scannedAtUtc: String,
    val subjectCode: String,
    val section: String,
)

/** Body of PUT students/me/fcm-token. */
@Serializable
data class FcmTokenRequest(val token: String)

/** GET students/me/overview and parent/children/{id}/overview. [termName] null = no active term (no classes). */
@Serializable
data class StudentOverview(
    val studentId: Int,
    val studentNumber: String,
    val fullName: String,
    val termName: String? = null,
    val classes: List<ClassOverview>,
)

/**
 * One class this term. Counts and lists are live; the four risk fields are the Worker's latest check and are
 * null until it has run. [isAtRisk] is always null for the student's own overview.
 */
@Serializable
data class ClassOverview(
    val classId: Int,
    val subjectCode: String,
    val subjectTitle: String,
    val section: String,
    val teacherName: String,
    val present: Int,
    val late: Int,
    val absent: Int,
    val excused: Int,
    val absenceRate: Double? = null,
    val weightedAverage: Double? = null,
    val isAtRisk: Boolean? = null,
    val riskComputedAtUtc: String? = null,
    val attendance: List<AttendanceItem>,
    val grades: List<GradeItem>,
)

/** One attendance record, newest first. [status] is Present, Late, Absent or Excused. */
@Serializable
data class AttendanceItem(val sessionId: Int, val startTimeUtc: String, val status: String)

/** One assessment; [score] null = not graded yet. */
@Serializable
data class GradeItem(
    val assessmentId: Int,
    val title: String,
    val type: String,
    val maxScore: Double,
    val weightPercent: Double,
    val score: Double? = null,
    val postedAtUtc: String? = null,
)

/** One child linked to the logged-in parent (GET parent/children). */
@Serializable
data class LinkedStudent(val studentId: Int, val studentNumber: String, val studentName: String)
