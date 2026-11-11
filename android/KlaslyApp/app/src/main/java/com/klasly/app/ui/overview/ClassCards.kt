package com.klasly.app.ui.overview

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.klasly.app.data.ClassOverview
import com.klasly.app.data.average
import com.klasly.app.data.manilaDateTime
import com.klasly.app.data.percent
import com.klasly.app.data.score

/** What the student tabs and the parent's Child overview show. */
sealed interface OverviewUiState {
    data object Loading : OverviewUiState

    /** The API's message (or "Can't reach Klasly…"); the screen offers Try again. */
    data class Error(val message: String) : OverviewUiState

    /** Nothing to load at all, e.g. a parent with no linked children. */
    data class Empty(val message: String) : OverviewUiState

    /** [classes] empty = no classes this term. [refreshing] = a pull-to-refresh is running over this data. */
    data class Ready(val classes: List<ClassOverview>, val refreshing: Boolean = false) : OverviewUiState
}

// Fixed status colours (white text on them passes contrast in light and dark mode), as on the Scan screen.
private val OnTrackGreen = Color(0xFF1B6E34)

/**
 * Loading / error / empty / list, with pull-to-refresh. [header] scrolls with the list;
 * [card] draws one class.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OverviewContent(
    state: OverviewUiState,
    onRefresh: () -> Unit,
    modifier: Modifier = Modifier,
    header: @Composable () -> Unit = {},
    card: @Composable (ClassOverview) -> Unit,
) {
    PullToRefreshBox(
        isRefreshing = state is OverviewUiState.Ready && state.refreshing,
        onRefresh = onRefresh,
        modifier = modifier.fillMaxSize(),
    ) {
        when (state) {
            OverviewUiState.Loading -> Column(Modifier.fillMaxSize()) {
                header()
                Row(Modifier.padding(24.dp), verticalAlignment = Alignment.CenterVertically) {
                    CircularProgressIndicator(Modifier.size(24.dp), strokeWidth = 3.dp)
                    Spacer(Modifier.size(16.dp))
                    Text("Loading…", style = MaterialTheme.typography.bodyLarge)
                }
            }
            // Scrollable so a pull still refreshes on these states.
            is OverviewUiState.Error -> Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
                header()
                Column(Modifier.padding(24.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    Text("Couldn't load your records", style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.error)
                    Text(state.message, style = MaterialTheme.typography.bodyLarge)
                    Button(onClick = onRefresh, modifier = Modifier.heightIn(min = 48.dp)) { Text("Try again") }
                }
            }
            is OverviewUiState.Empty -> Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
                header()
                Text(state.message, style = MaterialTheme.typography.bodyLarge, modifier = Modifier.padding(24.dp))
            }
            is OverviewUiState.Ready -> LazyColumn(
                Modifier.fillMaxSize(),
                contentPadding = PaddingValues(bottom = 24.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                item { header() }
                if (state.classes.isEmpty()) {
                    item { Text("No classes this term.", style = MaterialTheme.typography.bodyLarge, modifier = Modifier.padding(horizontal = 24.dp)) }
                }
                items(state.classes, key = { it.classId }) { Column(Modifier.padding(horizontal = 16.dp)) { card(it) } }
            }
        }
    }
}

/** Screen title row: [title], a line below it, and Log out (same as the Scan header). */
@Composable
fun OverviewHeader(title: String, subtitle: String?, onLogOut: () -> Unit) {
    Row(
        Modifier.fillMaxWidth().padding(start = 24.dp, end = 8.dp, top = 16.dp, bottom = 8.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(title, style = MaterialTheme.typography.headlineSmall, modifier = Modifier.semantics { heading() })
            if (subtitle != null) {
                Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
        TextButton(onClick = onLogOut, modifier = Modifier.heightIn(min = 48.dp)) { Text("Log out") }
    }
}

/** Subject, section and teacher; the at-risk badge and the early-warning line only when [ClassOverview.isAtRisk] is set. */
@Composable
private fun ClassHeading(c: ClassOverview) {
    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        Text("${c.subjectCode} · ${c.subjectTitle}", style = MaterialTheme.typography.titleMedium, modifier = Modifier.semantics { heading() })
        Text("${c.section} · ${c.teacherName}", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        if (c.isAtRisk != null) {
            Spacer(Modifier.size(4.dp))
            Surface(
                shape = RoundedCornerShape(8.dp),
                color = if (c.isAtRisk) MaterialTheme.colorScheme.error else OnTrackGreen,
            ) {
                Text(
                    if (c.isAtRisk) "At risk" else "On track",
                    color = if (c.isAtRisk) MaterialTheme.colorScheme.onError else Color.White,
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                )
            }
            Text(
                "This is an early-warning indicator for review, not a final grade." +
                    (c.riskComputedAtUtc?.let { " Updated ${manilaDateTime(it)}." } ?: ""),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
private fun Fact(label: String, value: String) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        Text(label, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
    }
}

@Composable
private fun AttendanceSummary(c: ClassOverview) {
    Fact("Present · Late · Absent · Excused", "${c.present} · ${c.late} · ${c.absent} · ${c.excused}")
    Fact("Absence rate", percent(c.absenceRate))
}

@Composable
private fun AttendanceRows(c: ClassOverview) {
    if (c.attendance.isEmpty()) {
        Text("No sessions recorded yet.", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
    c.attendance.forEach { a ->
        val absent = a.status == "Absent"
        Row(Modifier.fillMaxWidth().heightIn(min = 32.dp), verticalAlignment = Alignment.CenterVertically) {
            Text(manilaDateTime(a.startTimeUtc), style = MaterialTheme.typography.bodyMedium, modifier = Modifier.weight(1f))
            Text(
                a.status,
                style = MaterialTheme.typography.bodyMedium,
                color = if (absent) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurface,
                fontWeight = if (absent) FontWeight.Bold else FontWeight.Normal,
            )
        }
    }
}

@Composable
private fun GradeRows(c: ClassOverview) {
    if (c.grades.isEmpty()) {
        Text("No assessments yet.", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
    c.grades.forEach { g ->
        Row(Modifier.fillMaxWidth().heightIn(min = 40.dp), verticalAlignment = Alignment.CenterVertically) {
            Column(Modifier.weight(1f)) {
                Text(g.title, style = MaterialTheme.typography.bodyMedium)
                Text("${g.type} · ${average(g.weightPercent)}%", style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            Text(
                score(g.score, g.maxScore),
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = if (g.score == null) FontWeight.Normal else FontWeight.SemiBold,
                color = if (g.score == null) MaterialTheme.colorScheme.onSurfaceVariant else MaterialTheme.colorScheme.onSurface,
            )
        }
    }
}

@Composable
private fun ClassCard(content: @Composable () -> Unit) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) { content() }
    }
}

/** Student Attendance tab: the summary and every record. */
@Composable
fun AttendanceCard(c: ClassOverview) = ClassCard {
    ClassHeading(c)
    AttendanceSummary(c)
    HorizontalDivider()
    AttendanceRows(c)
}

/** Student Grades tab: every assessment and the weighted average. */
@Composable
fun GradesCard(c: ClassOverview) = ClassCard {
    ClassHeading(c)
    Fact("Weighted average", average(c.weightedAverage))
    HorizontalDivider()
    GradeRows(c)
}

/** Parent Child overview: the risk badge and summary, with records and grades behind Show buttons. */
@Composable
fun ChildClassCard(c: ClassOverview) = ClassCard {
    ClassHeading(c)
    AttendanceSummary(c)
    Fact("Weighted average", average(c.weightedAverage))
    Expandable("attendance records (${c.attendance.size})") { AttendanceRows(c) }
    Expandable("grades (${c.grades.size})") { GradeRows(c) }
}

@Composable
private fun Expandable(label: String, content: @Composable () -> Unit) {
    var open by rememberSaveable(label) { mutableStateOf(false) }
    // No side padding, so the label lines up with the card's text; the height keeps a 48 dp tap target.
    TextButton(onClick = { open = !open }, contentPadding = PaddingValues(vertical = 12.dp), modifier = Modifier.heightIn(min = 48.dp)) {
        Text(if (open) "Hide $label" else "Show $label")
    }
    if (open) content()
}
