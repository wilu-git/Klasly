package com.klasly.app.push

import android.Manifest
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import com.google.firebase.FirebaseApp
import com.google.firebase.messaging.FirebaseMessaging
import com.klasly.app.AppContainer
import kotlinx.coroutines.launch

/** Grade-push setup: the notification channel, sending the FCM token to the API, and the one-time permission ask. */
object PushRegistration {
    const val CHANNEL_ID = "grades"

    /** Without google-services.json there is no Firebase app; every push call is then skipped. */
    fun isAvailable(context: Context): Boolean = FirebaseApp.getApps(context).isNotEmpty()

    fun createChannel(context: Context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val channel = NotificationChannel(CHANNEL_ID, "Grades", NotificationManager.IMPORTANCE_DEFAULT).apply {
            description = "A notification when your teacher posts a grade"
        }
        context.getSystemService(NotificationManager::class.java).createNotificationChannel(channel)
    }

    /**
     * Gets this phone's current token and sends it for the logged-in student.
     * shortcut: getToken/onNewToken are deprecated since firebase-messaging 25.1.0 in favour of installation-ID
     * registration (register() + onRegistered). They still work; move over once the API's FCM send is confirmed
     * with installation IDs on a real Firebase project (Milestone 10 Task 9).
     */
    @Suppress("DEPRECATION")
    fun sendToken(context: Context, container: AppContainer) {
        if (!isAvailable(context)) return
        FirebaseMessaging.getInstance().token.addOnSuccessListener { token ->
            container.appScope.launch { container.repository.setFcmToken(token) }
        }
    }
}

private const val PREFS = "klasly_push"
private const val ASKED = "askedNotificationPermission"

/** Asks for the notification permission once, the first time a student reaches the scan screen (Android 13+). */
@Composable
fun AskNotificationPermissionOnce() {
    if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return
    val context = LocalContext.current
    val launcher = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { }
    LaunchedEffect(Unit) {
        val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        val granted = ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) ==
            PackageManager.PERMISSION_GRANTED
        if (!granted && !prefs.getBoolean(ASKED, false) && PushRegistration.isAvailable(context)) {
            prefs.edit().putBoolean(ASKED, true).apply()
            launcher.launch(Manifest.permission.POST_NOTIFICATIONS)
        }
    }
}
