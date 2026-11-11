package com.klasly.app.data

import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.PUT

/** The Web API endpoints the app uses (Milestones 6 and 10). */
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

    /** Saves this phone's FCM token for grade pushes (204, no body). */
    @PUT("students/me/fcm-token")
    suspend fun setFcmToken(@Body body: FcmTokenRequest)
}
