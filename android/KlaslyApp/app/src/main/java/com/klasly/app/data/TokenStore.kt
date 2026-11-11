package com.klasly.app.data

import android.content.Context
import android.content.SharedPreferences
import androidx.core.content.edit
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import java.io.IOException
import java.security.GeneralSecurityException
import java.time.Instant

/** Who is logged in. The JWT stops working at [expiresAt]; the API checks it, the app only uses it to skip a dead login. */
data class Session(val token: String, val expiresAt: Instant, val role: String, val fullName: String)

/** Keeps the session in encrypted preferences, so the JWT is never stored in plain text. */
class TokenStore(context: Context) {

    private val prefs: SharedPreferences = open(context.applicationContext)

    /** The saved session, or null when there is none or it has expired. */
    fun load(): Session? {
        val token = prefs.getString(KEY_TOKEN, null) ?: return null
        val session = Session(
            token = token,
            expiresAt = Instant.ofEpochMilli(prefs.getLong(KEY_EXPIRES, 0)),
            role = prefs.getString(KEY_ROLE, null).orEmpty(),
            fullName = prefs.getString(KEY_NAME, null).orEmpty(),
        )
        return session.takeIf { it.expiresAt.isAfter(Instant.now()) }
    }

    fun save(session: Session) = prefs.edit {
        putString(KEY_TOKEN, session.token)
        putLong(KEY_EXPIRES, session.expiresAt.toEpochMilli())
        putString(KEY_ROLE, session.role)
        putString(KEY_NAME, session.fullName)
    }

    fun clear() = prefs.edit { clear() }

    private companion object {
        const val FILE = "klasly_session"
        const val KEY_TOKEN = "token"
        const val KEY_EXPIRES = "expires_at"
        const val KEY_ROLE = "role"
        const val KEY_NAME = "full_name"

        // shortcut: security-crypto is deprecated since 2025 but still works on SDK 26+;
        // replace this function with DataStore + an Android Keystore key if it ever breaks.
        @Suppress("DEPRECATION")
        fun open(context: Context): SharedPreferences {
            fun create() = EncryptedSharedPreferences.create(
                context,
                FILE,
                MasterKey.Builder(context).setKeyScheme(MasterKey.KeyScheme.AES256_GCM).build(),
                EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
                EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
            )
            return try {
                create()
            } catch (e: GeneralSecurityException) {
                // A broken keystore (rare, after a restore or OS update) can't decrypt the file:
                // drop it and start logged out instead of crashing.
                context.deleteSharedPreferences(FILE)
                create()
            } catch (e: IOException) {
                context.deleteSharedPreferences(FILE)
                create()
            }
        }
    }
}
