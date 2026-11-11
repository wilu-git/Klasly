package com.klasly.app.ui.parent

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.klasly.app.AppContainer
import com.klasly.app.data.ApiResult
import com.klasly.app.data.LinkedStudent
import com.klasly.app.data.StudentOverview
import com.klasly.app.ui.overview.OverviewUiState
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

/** The parent's Child overview: the linked children, the one picked, and that child's overview. */
class ParentViewModel(private val container: AppContainer) : ViewModel() {

    private val _children = MutableStateFlow<List<LinkedStudent>>(emptyList())
    val children: StateFlow<List<LinkedStudent>> = _children.asStateFlow()

    private val _selectedId = MutableStateFlow<Int?>(null)
    val selectedId: StateFlow<Int?> = _selectedId.asStateFlow()

    private val _state = MutableStateFlow<OverviewUiState>(OverviewUiState.Loading)
    val state: StateFlow<OverviewUiState> = _state.asStateFlow()

    /** e.g. "2026-00001 · 1st Sem 2026-27", under the child's name. */
    private val _subtitle = MutableStateFlow<String?>(null)
    val subtitle: StateFlow<String?> = _subtitle.asStateFlow()

    private var loading: Job? = null

    init {
        refresh()
    }

    /** Reloads the children (they may have been linked or unlinked) and the picked child's overview. */
    fun refresh() {
        startLoading()
        loading = viewModelScope.launch {
            when (val result = container.repository.children()) {
                is ApiResult.Failed -> _state.value = OverviewUiState.Error(result.message)
                is ApiResult.Ok -> {
                    _children.value = result.value
                    val keep = result.value.firstOrNull { it.studentId == _selectedId.value } ?: result.value.firstOrNull()
                    _selectedId.value = keep?.studentId
                    if (keep == null) {
                        _state.value = OverviewUiState.Empty("No children are linked to your account yet. Ask the school admin.")
                    } else {
                        loadOverview(keep.studentId)
                    }
                }
            }
        }
    }

    fun select(studentId: Int) {
        if (studentId == _selectedId.value) return
        _selectedId.value = studentId
        _state.value = OverviewUiState.Loading // don't show the previous child's cards under the new name
        loading?.cancel()
        loading = viewModelScope.launch { loadOverview(studentId) }
    }

    private suspend fun loadOverview(studentId: Int) {
        _state.value = when (val result = container.repository.childOverview(studentId)) {
            is ApiResult.Ok -> {
                _subtitle.value = subtitleOf(result.value)
                OverviewUiState.Ready(result.value.classes)
            }
            is ApiResult.Failed -> OverviewUiState.Error(result.message)
        }
    }

    private fun startLoading() {
        loading?.cancel()
        val current = _state.value
        _state.value = if (current is OverviewUiState.Ready) current.copy(refreshing = true) else OverviewUiState.Loading
    }

    private fun subtitleOf(o: StudentOverview) = listOfNotNull(o.studentNumber, o.termName).joinToString(" · ")
}
