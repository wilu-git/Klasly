package com.klasly.app.data

import org.junit.Assert.assertEquals
import org.junit.Test
import java.time.Instant
import java.time.temporal.ChronoUnit

class ApiTextTest {

    @Test
    fun detailWinsOverEverythingElse() = assertEquals(
        "Invalid QR code",
        errorMessage("""{"title":"Bad Request","status":400,"detail":"Invalid QR code","errors":{"x":["y"]}}"""),
    )

    @Test
    fun firstValidationErrorBeatsTheGenericTitle() = assertEquals(
        "The Email field is required.",
        errorMessage(
            """{"title":"One or more validation errors occurred.","status":400,
               "errors":{"Email":["The Email field is required.","Second message"],"Password":["Other"]}}""",
        ),
    )

    @Test
    fun titleWhenThereIsNoDetailOrErrors() =
        assertEquals("Not Found", errorMessage("""{"title":"Not Found","status":404,"detail":""}"""))

    @Test
    fun fallbackForEmptyNullOrNonJsonBodies() {
        for (body in listOf(null, "", "<html>Bad gateway</html>", "[]", "{}")) {
            assertEquals("body: $body", FALLBACK_ERROR, errorMessage(body))
        }
    }

    @Test
    fun parsesDotNetUtcTimes() {
        val expected = Instant.parse("2026-11-10T01:03:00Z")
        assertEquals(expected, parseUtc("2026-11-10T01:03:00Z"))
        // .NET writes 7 decimal places
        assertEquals(expected, parseUtc("2026-11-10T01:03:00.1234567Z").truncatedTo(ChronoUnit.SECONDS))
        // a DateTime read back from SQL Server has no "Z"; the API's times are always UTC
        assertEquals(expected, parseUtc("2026-11-10T01:03:00"))
    }
}
