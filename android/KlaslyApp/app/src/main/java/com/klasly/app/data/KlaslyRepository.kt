package com.klasly.app.data

import kotlinx.serialization.SerializationException
import retrofit2.HttpException
import java.io.IOException

/** The answer to an API call. The UI shows [Failed.message] as is: it is the API's own text. */
sealed interface ApiResult<out T> {
    data class Ok<T>(val value: T) : ApiResult<T>
    data class Failed(val message: String, val unauthorized: Boolean = false) : ApiResult<Nothing>
}

const val NETWORK_ERROR = "Can't reach Klasly. Check your connection."

/**
 * Calls the Web API and never throws to the UI.
 * A 401 on a call made with the saved token means the session is over: [onSessionExpired] logs the user out.
 */
class KlaslyRepository(
    private val api: KlaslyApi,
    private val deviceId: String,
    private val onSessionExpired: (message: String) -> Unit,
) {

    // Login's 401 is "Invalid email or password.", not an expired session.
    suspend fun login(email: String, password: String): ApiResult<LoginResponse> =
        call(endsSessionOn401 = false) { api.login(LoginRequest(email.trim(), password)) }

    /** Registers this phone for the student who just logged in with [token]. */
    suspend fun registerDevice(token: String): ApiResult<DeviceResponse> =
        call(endsSessionOn401 = false) { api.registerDevice("Bearer $token", RegisterDeviceRequest(deviceId)) }

    suspend fun scan(qrToken: String): ApiResult<ScanResponse> =
        call { api.scan(ScanRequest(qrToken = qrToken, deviceId = deviceId)) }

    /** Sends the FCM token in the background; a 401 here never logs the student out (they find out on their next scan). */
    suspend fun setFcmToken(token: String): ApiResult<Unit> =
        call(endsSessionOn401 = false) { api.setFcmToken(FcmTokenRequest(token)) }

    private suspend fun <T> call(endsSessionOn401: Boolean = true, block: suspend () -> T): ApiResult<T> = try {
        ApiResult.Ok(block())
    } catch (e: HttpException) {
        // Retrofit has already buffered the error body, so reading it here does no network I/O.
        val message = errorMessage(e.response()?.errorBody()?.string())
        val unauthorized = e.code() == 401
        if (unauthorized && endsSessionOn401) onSessionExpired(message)
        ApiResult.Failed(message, unauthorized)
    } catch (e: IOException) {
        ApiResult.Failed(NETWORK_ERROR)
    } catch (e: SerializationException) {
        ApiResult.Failed(FALLBACK_ERROR)
    }
}
