resource "aws_sqs_queue" "processing" {
  name                      = "processing"
  delay_seconds             = 10
  visibility_timeout_seconds = 180
  message_retention_seconds = 86400
  receive_wait_time_seconds = 10
}

resource "aws_sqs_queue" "output" {
  name                      = "output"
  delay_seconds             = 10
  message_retention_seconds = 86400
  receive_wait_time_seconds = 10
}