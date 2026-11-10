package com.klasly.app.ui.scan

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.klasly.app.AppContainer
import com.klasly.app.data.ApiResult
import com.klasly.app.data.parseUtc
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

sealed interface ScanUiState {
    data object Scanning : ScanUiState
    data object Sending : ScanUiState
    /** [present] false = Late. [classLabel] e.g. "ELECT3 · BSCpE 3-A", [time] in Manila, e.g. "9:03 AM". */
    data class Checked(val present: Boolean, val classLabel: String, val time: String) : ScanUiState
    data class Refused(val message: String) : ScanUiState
}

private val manilaTime = DateTimeFormatter.ofPattern("h:mm a", Locale.US).withZone(ZoneId.of("Asia/Manila"))

class ScanViewModel(private val container: AppContainer) : ViewModel() {

    private val _state = MutableStateFlow<ScanUiState>(ScanUiState.Scanning)
    val state: StateFlow<ScanUiState> = _state.asStateFlow()

    /** Called for every QR the camera sees; only the first one while scanning is sent (one QR = one request). */
    fun onQrRead(text: String) {
        if (_state.value != ScanUiState.Scanning) return
        _state.value = ScanUiState.Sending
        viewModelScope.launch {
            _state.value = when (val result = container.repository.scan(text)) {
                is ApiResult.Ok -> with(result.value) {
                    ScanUiState.Checked(status == "Present", "$subjectCode · $section", manilaTime.format(parseUtc(scannedAtUtc)))
                }
                // A 401 has already logged the student out (the Login screen shows why); nothing to show here.
                is ApiResult.Failed -> ScanUiState.Refused(result.message)
            }
        }
    }

    /** "Done" after a check-in, or "Scan again" after a refusal. */
    fun scanAgain() {
        _state.value = ScanUiState.Scanning
    }
}
