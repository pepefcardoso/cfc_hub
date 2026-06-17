resource "aws_sns_topic" "pagerduty" {
  name = "cfchub-pagerduty-${var.environment}"
}

resource "aws_sns_topic_subscription" "pagerduty_https" {
  topic_arn = aws_sns_topic.pagerduty.arn
  protocol  = "https"
  endpoint  = var.pagerduty_endpoint
}

resource "aws_sns_topic" "email" {
  name = "cfchub-alerts-${var.environment}"
}

resource "aws_sns_topic_subscription" "email" {
  topic_arn = aws_sns_topic.email.arn
  protocol  = "email"
  endpoint  = var.alert_email
}

# S3 Access Logs Group (for MedicalFileDirectAccess)
resource "aws_cloudwatch_log_group" "s3_access_logs" {
  name              = "/s3/access-logs/cfchub-${var.environment}-medical"
  retention_in_days = 30
}

# Pre-created CloudWatch Log Insights queries
resource "aws_cloudwatch_query_definition" "slow_handlers" {
  name = "CFCHub/${var.environment}/Slow Handlers"

  log_group_names = [var.api_log_group_name]

  query_string = <<EOF
fields @timestamp, @message, @duration
| filter @message like /Handled request/
| sort @duration desc
| limit 20
EOF
}

resource "aws_cloudwatch_query_definition" "failed_outbox" {
  name = "CFCHub/${var.environment}/Failed Outbox Messages"

  log_group_names = [var.workers_log_group_name]

  query_string = <<EOF
fields @timestamp, @message, type, error
| filter status = "Failed" or @message like /Outbox worker failed/
| sort @timestamp desc
| limit 20
EOF
}

resource "aws_cloudwatch_query_definition" "lgpd_erasure" {
  name = "CFCHub/${var.environment}/LGPD Erasure Completions"

  log_group_names = [var.workers_log_group_name]

  query_string = <<EOF
fields @timestamp, @message, studentId
| filter type = "DataErasureCompleteNotified" or @message like /Data erasure completed/
| sort @timestamp desc
| limit 20
EOF
}
