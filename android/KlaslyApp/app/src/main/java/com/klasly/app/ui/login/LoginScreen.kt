package com.klasly.app.ui.login

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.autofill.ContentType
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.contentType
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp

/** Stateless login form: [state] in, events out. [notice] is the one-off message (e.g. session expired). */
@Composable
fun LoginScreen(
    state: LoginUiState,
    notice: String?,
    onEmailChange: (String) -> Unit,
    onPasswordChange: (String) -> Unit,
    onLogIn: () -> Unit,
) {
    var showPassword by rememberSaveable { mutableStateOf(false) }
    val message = state.error ?: notice

    Scaffold { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .imePadding()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp, vertical = 32.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
        ) {
            Column(Modifier.widthIn(max = 420.dp).fillMaxWidth()) {
                Text(
                    "Klasly",
                    style = MaterialTheme.typography.displaySmall,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.semantics { heading() },
                )
                Text(
                    "Log in with your school email.",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Spacer(Modifier.height(32.dp))

                OutlinedTextField(
                    value = state.email,
                    onValueChange = onEmailChange,
                    label = { Text("Email") },
                    singleLine = true,
                    enabled = !state.loading,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email, imeAction = ImeAction.Next),
                    modifier = Modifier.fillMaxWidth().semantics { contentType = ContentType.EmailAddress },
                )
                Spacer(Modifier.height(12.dp))
                OutlinedTextField(
                    value = state.password,
                    onValueChange = onPasswordChange,
                    label = { Text("Password") },
                    singleLine = true,
                    enabled = !state.loading,
                    visualTransformation = if (showPassword) VisualTransformation.None else PasswordVisualTransformation(),
                    trailingIcon = {
                        TextButton(onClick = { showPassword = !showPassword }) {
                            Text(if (showPassword) "Hide" else "Show")
                        }
                    },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password, imeAction = ImeAction.Done),
                    keyboardActions = KeyboardActions(onDone = { onLogIn() }),
                    modifier = Modifier.fillMaxWidth().semantics { contentType = ContentType.Password },
                )

                if (message != null) {
                    Spacer(Modifier.height(12.dp))
                    Text(
                        message,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium,
                        modifier = Modifier.semantics { liveRegion = LiveRegionMode.Polite },
                    )
                }

                Spacer(Modifier.height(24.dp))
                Button(
                    onClick = onLogIn,
                    enabled = !state.loading,
                    modifier = Modifier.fillMaxWidth().heightIn(min = 52.dp),
                ) {
                    if (state.loading) {
                        CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
                        Spacer(Modifier.size(12.dp))
                        Text("Logging in…")
                    } else {
                        Text("Log in")
                    }
                }
            }
        }
    }
}
