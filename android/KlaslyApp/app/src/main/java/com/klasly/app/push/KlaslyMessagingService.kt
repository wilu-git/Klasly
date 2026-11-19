package com.klasly.app.push

// DEFERRED: Firebase grade push. The whole file is commented out until a Firebase project exists; remove the
// opening and closing block-comment markers below (and re-enable the <service> in AndroidManifest.xml) to restore it.
/*
import android.Manifest
import android.app.PendingIntent
import android.content.Intent
import android.content.pm.PackageManager
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.klasly.app.KlaslyApplication
import com.klasly.app.MainActivity
import com.klasly.app.R
import kotlinx.coroutines.launch

/**
 * Receives grade pushes. With the app in the background Android shows the notification itself (on the "grades"
 * channel from the manifest); this class shows it while the app is open, and sends a renewed token to the API.
 */
class KlaslyMessagingService : FirebaseMessagingService() {

    // shortcut: deprecated with getToken (see PushRegistration.sendToken); still called by firebase-messaging 26.
    @Deprecated("Replaced by onRegistered in firebase-messaging 25.1.0; kept until the installation-ID send is verified.")
    override fun onNewToken(token: String) {
        val container = (application as KlaslyApplication).container
        // Only a logged-in student owns the token; otherwise the next student login sends it.
        if (container.session.value?.role == "Student") {
            container.appScope.launch { container.repository.setFcmToken(token) }
        }
    }

    override fun onMessageReceived(message: RemoteMessage) {
        val notification = message.notification ?: return
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED &&
            android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.TIRAMISU
        ) return

        val open = PendingIntent.getActivity(
            this, 0, Intent(this, MainActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP),
            PendingIntent.FLAG_IMMUTABLE,
        )
        val shown = NotificationCompat.Builder(this, PushRegistration.CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_notification)
            .setContentTitle(notification.title ?: "Klasly")
            .setContentText(notification.body)
            .setContentIntent(open)
            .setAutoCancel(true)
            .build()
        NotificationManagerCompat.from(this).notify(message.messageId.hashCode(), shown)
    }
}
*/
