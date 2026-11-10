package com.klasly.app

import android.annotation.SuppressLint
import android.app.Application
import android.content.Context
import android.provider.Settings
import com.klasly.app.data.KlaslyApi
import com.klasly.app.data.KlaslyRepository
import com.klasly.app.data.Session
import com.klasly.app.data.TokenStore
import com.klasly.app.push.PushRegistration
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory

/** Creates the one [AppContainer] when the app process starts (registered in the manifest). */
class KlaslyApplication : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        container = AppContainer(this)
        PushRegistration.createChannel(this)
    }
}

/** Builds the app's shared objects by hand (no DI library) and owns the logged-in session. */
class AppContainer(private val context: Context) {

    /** For work that outlives a screen, e.g. sending the FCM token. */
    val appScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    private val tokenStore = TokenStore(context)

    private val _session = MutableStateFlow(tokenStore.load())
    /** Null = show Login. MainActivity picks the screen from the role. */
    val session: StateFlow<Session?> = _session.asStateFlow()

    private val _loginMessage = MutableStateFlow<String?>(null)
    /** Shown once on the Login screen, e.g. "Session expired – please log in". */
    val loginMessage: StateFlow<String?> = _loginMessage.asStateFlow()

    // Device binding uses the Android ID (guide §27.8 check 6); it is per app-signing key, not a tracking ID.
    @SuppressLint("HardwareIds")
    private val deviceId: String =
        Settings.Secure.getString(context.contentResolver, Settings.Secure.ANDROID_ID).orEmpty()

    private val http = OkHttpClient.Builder()
        .addInterceptor { chain ->
            val request = chain.request()
            val token = _session.value?.token
            // No logging interceptor: the JWT must never reach Logcat.
            chain.proceed(
                if (token == null || request.header("Authorization") != null) request
                else request.newBuilder().header("Authorization", "Bearer $token").build(),
            )
        }
        .build()

    private val api: KlaslyApi = Retrofit.Builder()
        .baseUrl(BuildConfig.API_BASE_URL)
        .client(http)
        .addConverterFactory(Json { ignoreUnknownKeys = true }.asConverterFactory("application/json".toMediaType()))
        .build()
        .create(KlaslyApi::class.java)

    val repository = KlaslyRepository(api, deviceId, onSessionExpired = ::logOut)

    fun logIn(session: Session) {
        tokenStore.save(session)
        _loginMessage.value = null
        _session.value = session
        // The phone's push token now belongs to this student (the API takes it off anyone else).
        if (session.role == "Student") PushRegistration.sendToken(context, this)
    }

    fun logOut(message: String? = null) {
        tokenStore.clear()
        _loginMessage.value = message
        _session.value = null
    }
}
