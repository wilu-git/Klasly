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
