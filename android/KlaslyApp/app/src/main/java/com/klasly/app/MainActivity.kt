package com.klasly.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.klasly.app.ui.login.LoginScreen
import com.klasly.app.ui.login.LoginViewModel
import com.klasly.app.ui.parent.ParentHomeScreen
import com.klasly.app.ui.theme.KlaslyTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val container = (application as KlaslyApplication).container
        setContent {
            KlaslyTheme {
                val session by container.session.collectAsState()
                val current = session
                when (current?.role) {
                    "Student" -> Scaffold { padding ->
                        // Replaced by the Scan QR screen in Task 6.
                        Column(Modifier.fillMaxSize().padding(padding).padding(24.dp)) {
                            Text("Hi, ${current.fullName}. Scanning comes next.")
                            OutlinedButton(onClick = { container.logOut() }) { Text("Log out") }
                        }
                    }
                    "Parent" -> ParentHomeScreen(current.fullName, onLogOut = { container.logOut() })
                    else -> {
                        val vm = viewModel { LoginViewModel(container) }
                        val state by vm.state.collectAsState()
                        val notice by container.loginMessage.collectAsState()
                        LoginScreen(state, notice, vm::onEmailChange, vm::onPasswordChange, vm::logIn)
                    }
                }
            }
        }
    }
}
