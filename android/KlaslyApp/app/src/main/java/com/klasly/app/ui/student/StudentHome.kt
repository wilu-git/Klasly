package com.klasly.app.ui.student

import androidx.annotation.DrawableRes
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.consumeWindowInsets
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.painterResource
import com.klasly.app.R
import com.klasly.app.ui.overview.AttendanceCard
import com.klasly.app.ui.overview.GradesCard
import com.klasly.app.ui.overview.OverviewContent
import com.klasly.app.ui.overview.OverviewHeader

private enum class Tab(val label: String, @param:DrawableRes val icon: Int) {
    Scan("Scan", R.drawable.ic_tab_scan),
    Attendance("Attendance", R.drawable.ic_tab_attendance),
    Grades("Grades", R.drawable.ic_tab_grades),
}

/**
 * Student home: Scan / Attendance / Grades in a bottom bar. [scan] is the existing Scan screen;
 * the two other tabs share [overview] so switching tabs doesn't reload.
 */
@Composable
fun StudentHome(overview: StudentOverviewViewModel, onLogOut: () -> Unit, scan: @Composable () -> Unit) {
    var tab by rememberSaveable { mutableIntStateOf(Tab.Scan.ordinal) }
    val state by overview.state.collectAsState()
    val subtitle by overview.subtitle.collectAsState()

    Scaffold(
        bottomBar = {
            NavigationBar {
                Tab.entries.forEach { t ->
                    NavigationBarItem(
                        selected = tab == t.ordinal,
                        onClick = { tab = t.ordinal },
                        icon = { Icon(painterResource(t.icon), contentDescription = null) }, // the label names it
                        label = { Text(t.label) },
                    )
                }
            }
        },
    ) { inner ->
        when (Tab.entries[tab]) {
            // The Scan screen draws under the status bar itself; only keep it above the bottom bar.
            Tab.Scan -> {
                val bottom = PaddingValues(bottom = inner.calculateBottomPadding())
                Box(Modifier.padding(bottom).consumeWindowInsets(bottom)) { scan() }
            }
            Tab.Attendance -> OverviewContent(
                state, overview::refresh, Modifier.padding(inner),
                header = { OverviewHeader("My attendance", subtitle, onLogOut) },
            ) { AttendanceCard(it) }
            Tab.Grades -> OverviewContent(
                state, overview::refresh, Modifier.padding(inner),
                header = { OverviewHeader("My grades", subtitle, onLogOut) },
            ) { GradesCard(it) }
        }
    }
}
