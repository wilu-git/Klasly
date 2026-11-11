package com.klasly.app.ui.parent

import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.klasly.app.ui.overview.ChildClassCard
import com.klasly.app.ui.overview.OverviewContent
import com.klasly.app.ui.overview.OverviewHeader

/** Parent home: the Child overview. Several children → chips to pick one; one child → shown straight away. */
@Composable
fun ParentHomeScreen(vm: ParentViewModel, onLogOut: () -> Unit) {
    val children by vm.children.collectAsState()
    val selectedId by vm.selectedId.collectAsState()
    val state by vm.state.collectAsState()
    val subtitle by vm.subtitle.collectAsState()
    val selected = children.firstOrNull { it.studentId == selectedId }

    Scaffold { padding ->
        OverviewContent(
            state, vm::refresh, Modifier.padding(padding),
            header = {
                Column {
                    OverviewHeader(selected?.studentName ?: "My children", subtitle.takeIf { selected != null }, onLogOut)
                    if (children.size > 1) {
                        Row(
                            Modifier.horizontalScroll(rememberScrollState()).padding(horizontal = 16.dp),
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            children.forEach { child ->
                                FilterChip(
                                    selected = child.studentId == selectedId,
                                    onClick = { vm.select(child.studentId) },
                                    label = { Text(child.studentName) },
                                    modifier = Modifier.heightIn(min = 48.dp),
                                )
                            }
                        }
                    }
                }
            },
        ) { ChildClassCard(it) }
    }
}
