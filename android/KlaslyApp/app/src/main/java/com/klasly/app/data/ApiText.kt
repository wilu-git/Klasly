package com.klasly.app.data

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.contentOrNull
import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneOffset

const val FALLBACK_ERROR = "Something went wrong. Please try again."

/**
 * The text to show for a failed API call, read from its problem-details body:
 * `detail` (the API's own message, e.g. "QR expired – rescan") → the first validation error → `title`.
 */
fun errorMessage(body: String?): String {
    val json = try {
        Json.parseToJsonElement(body.orEmpty()) as? JsonObject
    } catch (e: IllegalArgumentException) { // SerializationException: empty, HTML or truncated body
        null
    } ?: return FALLBACK_ERROR

    fun text(key: String) = (json[key] as? JsonPrimitive)?.contentOrNull?.takeIf { it.isNotBlank() }
    val firstValidationError = (json["errors"] as? JsonObject)?.values
        ?.firstNotNullOfOrNull { ((it as? JsonArray)?.firstOrNull() as? JsonPrimitive)?.contentOrNull }

    return text("detail") ?: firstValidationError ?: text("title") ?: FALLBACK_ERROR
}

/** Reads a UTC time from the API; a value without "Z" (a DateTime read back from SQL Server) is UTC too. */
fun parseUtc(text: String): Instant =
    if (text.endsWith("Z")) Instant.parse(text) else LocalDateTime.parse(text).toInstant(ZoneOffset.UTC)
