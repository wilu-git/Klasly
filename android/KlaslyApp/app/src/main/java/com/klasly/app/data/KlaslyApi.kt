package com.klasly.app.data

import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST

/** The three Web API endpoints the app uses (built in Milestone 6). */
interface KlaslyApi {

    @POST("auth/login")
    suspend fun login(@Body body: LoginRequest): LoginResponse

    /** Takes the token explicitly: it runs right after login, before the session is saved. */
    @POST("students/me/device")
    suspend fun registerDevice(
        @Header("Authorization") bearer: String,
        @Body body: RegisterDeviceRequest,
    ): DeviceResponse

    @POST("attendance/scan")
    suspend fun scan(@Body body: ScanRequest): ScanResponse
}
