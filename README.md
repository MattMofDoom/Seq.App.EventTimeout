# Seq.Apps.EventTimeout - Event Timeout for Seq

This app provides an event timeout function for [Seq](https://datalust.co/seq). It will read an input stream and monitor for a matching text string during a configured start and end time. 

It includes a timeout and suppression interval, which allows you to fine tune the way the timeout works.

When a timeout occurs, it will output the configured text and description back to the stream.
