output "pagerduty_sns_topic_arn" {
  value       = aws_sns_topic.pagerduty.arn
  description = "ARN of the SNS topic for PagerDuty alerts"
}

output "email_sns_topic_arn" {
  value       = aws_sns_topic.email.arn
  description = "ARN of the SNS topic for email alerts"
}

output "s3_access_logs_log_group_name" {
  value       = aws_cloudwatch_log_group.s3_access_logs.name
  description = "Name of the CloudWatch Log Group for S3 access logs"
}
