package com.klasly.app.ui.login

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.klasly.app.AppContainer
import com.klasly.app.data.ApiResult
import com.klasly.app.data.Session
import com.klasly.app.data.parseUtc
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class LoginUiState(
    val email: String = "",
    val password: String = "",
    val loading: Boolean = false,
    val error: String? = null,
)

const val STAFF_REFUSED = "This app is for students and parents. Please use the Klasly website."

class LoginViewModel(private val container: AppContainer) : ViewModel() {

    private val _state = MutableStateFlow(LoginUiState())
    val state: StateFlow<LoginUiState> = _state.asStateFlow()

    fun onEmailChange(value: String) = _state.update { it.copy(email = value, error = null) }

    fun onPasswordChange(value: String) = _state.update { it.copy(password = value, error = null) }

    fun logIn() {
        val current = _state.value
        if (current.loading) return
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            val error = signIn(current.email, current.password)
            // On success the session switches the screen; the password isn't kept for the next login.
            _state.update { it.copy(loading = false, error = error, password = if (error == null) "" else it.password) }
        }
    }

    /** Returns the message to show, or null once the session is saved. */
    private suspend fun signIn(email: String, password: String): String? {
        val login = when (val result = container.repository.login(email, password)) {
            is ApiResult.Ok -> result.value
            is ApiResult.Failed -> return result.message
        }
        when (login.role) {
            // The phone is registered before anything is saved, so a 409 ("Another device is
            // already registered…") leaves the student logged out on this phone.
            "Student" -> when (val device = container.repository.registerDevice(login.token)) {
                is ApiResult.Ok -> Unit
                is ApiResult.Failed -> return device.message
            }
            "Parent" -> Unit
            else -> return STAFF_REFUSED
        }
        container.logIn(Session(login.token, parseUtc(login.expiresAtUtc), login.role, login.fullName))
        return null
    }
}
