variable "environment" {
  type        = string
  description = "Environment name"
}

variable "api_log_group_name" {
  type        = string
  description = "Name of the API ECS CloudWatch log group"
}

variable "workers_log_group_name" {
  type        = string
  description = "Name of the Workers ECS CloudWatch log group"
}

variable "alb_arn_suffix" {
  type        = string
  description = "ARN suffix of the ALB for metrics"
}

variable "target_group_arn_suffix" {
  type        = string
  description = "ARN suffix of the ALB Target Group for metrics"
}

variable "pagerduty_endpoint" {
  type        = string
  description = "HTTPS endpoint for PagerDuty integration"
}

variable "alert_email" {
  type        = string
  description = "Email address for non-critical alerts"
}

variable "medical_bucket_name" {
  type        = string
  description = "Name of the medical S3 bucket"
}
