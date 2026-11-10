package com.klasly.app.ui.scan

import android.Manifest
import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.provider.Settings
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.ImageAnalysis
import androidx.camera.mlkit.vision.MlKitAnalyzer
import androidx.camera.view.LifecycleCameraController
import androidx.camera.view.PreviewView
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.google.mlkit.vision.barcode.BarcodeScannerOptions
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.barcode.common.Barcode

// Fixed status colours (white text on them passes contrast in light and dark mode).
private val PresentGreen = Color(0xFF1B6E34)
private val LateAmber = Color(0xFF9A5B00)

/** Student home: camera preview that checks in on the first QR it reads. */
@Composable
fun ScanScreen(
    fullName: String,
    state: ScanUiState,
    onQrRead: (String) -> Unit,
    onScanAgain: () -> Unit,
    onLogOut: () -> Unit,
) {
    val context = LocalContext.current
    var granted by remember {
        mutableStateOf(ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED)
    }
    var askedOnce by rememberSaveable { mutableStateOf(false) }
    val permissionLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) {
        granted = it
        askedOnce = true
    }
    LaunchedEffect(Unit) { if (!granted && !askedOnce) permissionLauncher.launch(Manifest.permission.CAMERA) }

    Surface(Modifier.fillMaxSize(), color = Color.Black) {
        Box(Modifier.fillMaxSize()) {
            if (granted) {
                CameraPreview(onQrRead, Modifier.fillMaxSize())
                // Aiming guide only; ML Kit reads a QR anywhere in the frame.
                Box(
                    Modifier
                        .align(Alignment.Center)
                        .size(260.dp)
                        .border(BorderStroke(3.dp, Color.White.copy(alpha = 0.9f)), RoundedCornerShape(24.dp)),
                )
            }

            Header(fullName, onLogOut, Modifier.align(Alignment.TopCenter))

            Box(
                Modifier
                    .align(Alignment.BottomCenter)
                    .fillMaxWidth()
                    .navigationBarsPadding()
                    .padding(16.dp),
            ) {
                if (!granted) {
                    PermissionCard(onAllow = {
                        val activity = context as Activity
                        when {
                            ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) ==
                                PackageManager.PERMISSION_GRANTED -> granted = true // allowed in Settings meanwhile
                            !askedOnce || activity.shouldShowRequestPermissionRationale(Manifest.permission.CAMERA) ->
                                permissionLauncher.launch(Manifest.permission.CAMERA)
                            // "Don't ask again": Android won't show the dialog any more, so open the app's settings.
                            else -> context.startActivity(
                                Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.fromParts("package", context.packageName, null)),
                            )
                        }
                    })
                } else {
                    StatusCard(state, onScanAgain)
                }
            }
        }
    }
}

@Composable
private fun Header(fullName: String, onLogOut: () -> Unit, modifier: Modifier) {
    Surface(modifier.fillMaxWidth(), color = MaterialTheme.colorScheme.surface.copy(alpha = 0.92f)) {
        Row(
            Modifier
                .statusBarsPadding()
                .padding(start = 20.dp, end = 8.dp, top = 8.dp, bottom = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(Modifier.weight(1f)) {
                Text("Scan to check in", style = MaterialTheme.typography.titleLarge)
                Text(fullName, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            TextButton(onClick = onLogOut, modifier = Modifier.heightIn(min = 48.dp)) { Text("Log out") }
        }
    }
}

@Composable
private fun StatusCard(state: ScanUiState, onScanAgain: () -> Unit) {
    Surface(
        shape = RoundedCornerShape(24.dp),
        color = MaterialTheme.colorScheme.surface,
        tonalElevation = 2.dp,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(
            Modifier
                .padding(20.dp)
                .semantics { liveRegion = LiveRegionMode.Polite },
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            when (state) {
                ScanUiState.Scanning -> Text(
                    "Point your camera at the QR code on your teacher's screen.",
                    style = MaterialTheme.typography.bodyLarge,
                )
                ScanUiState.Sending -> Row(verticalAlignment = Alignment.CenterVertically) {
                    CircularProgressIndicator(Modifier.size(24.dp), strokeWidth = 3.dp)
                    Spacer(Modifier.size(16.dp))
                    Text("Checking you in…", style = MaterialTheme.typography.bodyLarge)
                }
                is ScanUiState.Checked -> {
                    Surface(shape = RoundedCornerShape(12.dp), color = if (state.present) PresentGreen else LateAmber) {
                        Text(
                            if (state.present) "Present" else "Late",
                            color = Color.White,
                            style = MaterialTheme.typography.headlineMedium,
                            fontWeight = FontWeight.Bold,
                            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                        )
                    }
                    Text(state.classLabel, style = MaterialTheme.typography.titleMedium)
                    Text("Checked in at ${state.time}", style = MaterialTheme.typography.bodyLarge)
                    Button(onClick = onScanAgain, modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp)) { Text("Done") }
                }
                is ScanUiState.Refused -> {
                    Text(
                        state.message,
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.error,
                    )
                    Button(onClick = onScanAgain, modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp)) { Text("Scan again") }
                }
            }
        }
    }
}

@Composable
private fun PermissionCard(onAllow: () -> Unit) {
    Surface(shape = RoundedCornerShape(24.dp), color = MaterialTheme.colorScheme.surface, modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(20.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Text("Klasly needs the camera to scan your teacher's QR code.", style = MaterialTheme.typography.bodyLarge)
            Button(onClick = onAllow, modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp)) { Text("Allow camera") }
        }
    }
}

@Composable
private fun CameraPreview(onQrRead: (String) -> Unit, modifier: Modifier) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val currentOnQrRead by rememberUpdatedState(onQrRead)
    val controller = remember { LifecycleCameraController(context) }

    DisposableEffect(lifecycleOwner) {
        val scanner = BarcodeScanning.getClient(
            BarcodeScannerOptions.Builder().setBarcodeFormats(Barcode.FORMAT_QR_CODE).build(),
        )
        val mainExecutor = ContextCompat.getMainExecutor(context)
        controller.setImageAnalysisAnalyzer(
            mainExecutor,
            MlKitAnalyzer(listOf(scanner), ImageAnalysis.COORDINATE_SYSTEM_ORIGINAL, mainExecutor) { result ->
                result.getValue(scanner)?.firstOrNull()?.rawValue?.let(currentOnQrRead)
            },
        )
        controller.bindToLifecycle(lifecycleOwner)
        onDispose {
            controller.unbind()
            controller.clearImageAnalysisAnalyzer()
            scanner.close()
        }
    }

    AndroidView(
        factory = { PreviewView(it).apply { this.controller = controller } },
        modifier = modifier.semantics { contentDescription = "Camera preview" },
    )
}
