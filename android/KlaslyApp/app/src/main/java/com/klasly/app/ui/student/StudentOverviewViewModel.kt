package com.klasly.app.ui.student

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.klasly.app.AppContainer
import com.klasly.app.data.ApiResult
import com.klasly.app.ui.overview.OverviewUiState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

/** The student's own overview, shared by the Attendance and Grades tabs; loaded once, reloaded on pull. */
class StudentOverviewViewModel(private val container: AppContainer) : ViewModel() {

    private val _state = MutableStateFlow<OverviewUiState>(OverviewUiState.Loading)
    val state: StateFlow<OverviewUiState> = _state.asStateFlow()

    /** Shown under the screen title, e.g. "Aira Mendoza · 1st Sem 2026-27". */
    private val _subtitle = MutableStateFlow<String?>(null)
    val subtitle: StateFlow<String?> = _subtitle.asStateFlow()

    init {
        refresh()
    }

    fun refresh() {
        val current = _state.value
        _state.value = if (current is OverviewUiState.Ready) current.copy(refreshing = true) else OverviewUiState.Loading
        viewModelScope.launch {
            _state.value = when (val result = container.repository.myOverview()) {
                is ApiResult.Ok -> {
                    _subtitle.value = listOfNotNull(result.value.fullName, result.value.termName).joinToString(" · ")
                    OverviewUiState.Ready(result.value.classes)
                }
                // A 401 has already logged the student out; anything else is shown with Try again.
                is ApiResult.Failed -> OverviewUiState.Error(result.message)
            }
        }
    }
}
