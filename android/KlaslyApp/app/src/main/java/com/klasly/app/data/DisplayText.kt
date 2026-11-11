package com.klasly.app.data

import java.text.DecimalFormat
import java.text.DecimalFormatSymbols
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

// Text for the attendance and grade screens. Pure functions, so they are unit-tested (DisplayTextTest).

private fun format(pattern: String) = DecimalFormat(pattern, DecimalFormatSymbols(Locale.US))
private val oneDecimal = format("0.#")
private val twoDecimals = format("0.##")
private val manila = DateTimeFormatter.ofPattern("MMM d, h:mm a", Locale.US).withZone(ZoneId.of("Asia/Manila"))

/** 0.2 → "20%"; null (the Worker hasn't checked yet) → "—". */
fun percent(rate: Double?): String = rate?.let { oneDecimal.format(it * 100) + "%" } ?: "—"

/** 87.5 → "87.5"; null → "No grades yet". */
fun average(value: Double?): String = value?.let(twoDecimals::format) ?: "No grades yet"

/** 87.5 of 100 → "87.5 / 100"; null → "Not graded yet". */
fun score(value: Double?, max: Double): String =
    value?.let { "${twoDecimals.format(it)} / ${twoDecimals.format(max)}" } ?: "Not graded yet"

/** An API UTC time in Manila, e.g. "Nov 10, 9:00 AM". */
fun manilaDateTime(utc: String): String = manila.format(parseUtc(utc))
