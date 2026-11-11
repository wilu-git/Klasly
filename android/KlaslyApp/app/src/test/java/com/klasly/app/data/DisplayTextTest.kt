package com.klasly.app.data

import kotlinx.serialization.json.Json
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class DisplayTextTest {

    @Test
    fun percentRoundsToOneDecimalAndDashWhenMissing() {
        assertEquals("20%", percent(0.2))
        assertEquals("100%", percent(1.0))
        assertEquals("33.3%", percent(1.0 / 3))
        assertEquals("—", percent(null))
    }

    @Test
    fun averageOrNoGradesYet() {
        assertEquals("87.5", average(87.5))
        assertEquals("90", average(90.0))
        assertEquals("74.99", average(74.99))
        assertEquals("No grades yet", average(null))
    }

    @Test
    fun scoreOverMaxOrNotGradedYet() {
        assertEquals("87.5 / 100", score(87.5, 100.0))
        assertEquals("18 / 20", score(18.0, 20.0))
        assertEquals("Not graded yet", score(null, 100.0))
    }

    @Test
    fun manilaDateTimeIsUtcPlus8() {
        assertEquals("Nov 10, 9:00 AM", manilaDateTime("2026-11-10T01:00:00Z"))
        assertEquals("Nov 11, 12:52 AM", manilaDateTime("2026-11-10T16:52:00"))
    }

    @Test
    fun overviewBodyParsesWithNulls() {
        val body = """
            {"studentId":1,"studentNumber":"2026-00001","fullName":"Aira Mendoza","termName":null,
             "classes":[{"classId":3,"subjectCode":"ELECT3","subjectTitle":"Mobile Application Development",
               "section":"BSCpE 3-A","teacherName":"Rosa Villanueva","present":4,"late":1,"absent":12,"excused":1,
               "absenceRate":0.75,"weightedAverage":null,"isAtRisk":null,"riskComputedAtUtc":null,
               "attendance":[{"sessionId":9,"startTimeUtc":"2026-11-10T01:00:00","status":"Absent"}],
               "grades":[{"assessmentId":5,"title":"Quiz 1","type":"Quiz","maxScore":20,"weightPercent":40,
                          "score":null,"postedAtUtc":null}]}]}
        """.trimIndent()

        val overview = Json { ignoreUnknownKeys = true }.decodeFromString<StudentOverview>(body)

        assertNull(overview.termName)
        val c = overview.classes.single()
        assertEquals(0.75, c.absenceRate!!, 0.0)
        assertNull(c.isAtRisk)
        assertEquals("Absent", c.attendance.single().status)
        assertNull(c.grades.single().score)
        assertEquals(20.0, c.grades.single().maxScore, 0.0)
    }

    @Test
    fun childListParses() {
        val body = """[{"studentId":1,"studentNumber":"2026-00001","studentName":"Aira Mendoza","relationship":"Mother","receivesAlerts":true}]"""
        val child = Json { ignoreUnknownKeys = true }.decodeFromString<List<LinkedStudent>>(body).single()
        assertEquals(1, child.studentId)
        assertEquals("Aira Mendoza", child.studentName)
    }
}
