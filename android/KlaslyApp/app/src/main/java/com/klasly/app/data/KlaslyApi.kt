package com.klasly.app.data

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

/** The Web API endpoints the app uses (Milestones 6, 10 and 13). */
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

    /** The student's own attendance and grades this term (Milestone 13). */
    @GET("students/me/overview")
    suspend fun myOverview(): StudentOverview

    /** The parent's linked children, by name. */
    @GET("parent/children")
    suspend fun children(): List<LinkedStudent>

    /** One linked child's overview; 403 for any other id. */
    @GET("parent/children/{studentId}/overview")
    suspend fun childOverview(@Path("studentId") studentId: Int): StudentOverview
}
