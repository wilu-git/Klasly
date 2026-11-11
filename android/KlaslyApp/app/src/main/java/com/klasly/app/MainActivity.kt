package com.klasly.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.lifecycle.viewmodel.compose.viewModel
import com.klasly.app.push.AskNotificationPermissionOnce
import com.klasly.app.ui.login.LoginScreen
import com.klasly.app.ui.login.LoginViewModel
import com.klasly.app.ui.parent.ParentHomeScreen
import com.klasly.app.ui.parent.ParentViewModel
import com.klasly.app.ui.scan.ScanScreen
import com.klasly.app.ui.scan.ScanViewModel
import com.klasly.app.ui.student.StudentHome
import com.klasly.app.ui.student.StudentOverviewViewModel
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
                    "Student" -> {
                        AskNotificationPermissionOnce()
                        // Keyed by token: the next login starts with a fresh scan state.
                        val vm = viewModel(key = current.token) { ScanViewModel(container) }
                        val state by vm.state.collectAsState()
                        val overview = viewModel(key = "overview-${current.token}") { StudentOverviewViewModel(container) }
                        StudentHome(overview, onLogOut = { container.logOut() }) {
                            ScanScreen(current.fullName, state, vm::onQrRead, vm::scanAgain, onLogOut = { container.logOut() })
                        }
                    }
                    "Parent" -> {
                        val vm = viewModel(key = current.token) { ParentViewModel(container) }
                        ParentHomeScreen(vm, onLogOut = { container.logOut() })
                    }
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
