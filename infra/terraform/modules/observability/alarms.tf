# 1. OutboxFailureRate
resource "aws_cloudwatch_log_metric_filter" "outbox_failure" {
  name           = "OutboxFailureFilter-${var.environment}"
  pattern        = "{ $.level = \"Critical\" || $.status = \"Failed\" || $.message = \"*Outbox*failed*\" }"
  log_group_name = var.workers_log_group_name

  metric_transformation {
    name      = "OutboxFailureCount"
    namespace = "CFCHub/${var.environment}"
    value     = "1"
  }
}

resource "aws_cloudwatch_metric_alarm" "outbox_failure" {
  alarm_name          = "CFCHub-${var.environment}-OutboxFailureRate"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = aws_cloudwatch_log_metric_filter.outbox_failure.metric_transformation[0].name
  namespace           = aws_cloudwatch_log_metric_filter.outbox_failure.metric_transformation[0].namespace
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  alarm_description   = "Alarm when outbox messages fail (count > 0 in 5min)"
  alarm_actions       = [aws_sns_topic.pagerduty.arn]
}

# 2. ApiErrorRate
resource "aws_cloudwatch_metric_alarm" "api_error_rate" {
  alarm_name          = "CFCHub-${var.environment}-ApiErrorRate"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  threshold           = 1 # > 1%
  alarm_description   = "Alarm when API HTTP 5xx > 1% of requests in 5min"
  alarm_actions       = [aws_sns_topic.pagerduty.arn]

  metric_query {
    id          = "e1"
    expression  = "m1 / m2 * 100"
    label       = "Error Rate"
    return_data = true
  }

  metric_query {
    id = "m1"
    metric {
      metric_name = "HTTPCode_Target_5XX_Count"
      namespace   = "AWS/ApplicationELB"
      period      = 300
      stat        = "Sum"
      dimensions = {
        LoadBalancer = var.alb_arn_suffix
      }
    }
  }

  metric_query {
    id = "m2"
    metric {
      metric_name = "RequestCount"
      namespace   = "AWS/ApplicationELB"
      period      = 300
      stat        = "Sum"
      dimensions = {
        LoadBalancer = var.alb_arn_suffix
      }
    }
  }
}

# 3. SchedulingConflictSpike
resource "aws_cloudwatch_log_metric_filter" "scheduling_conflict" {
  name           = "SchedulingConflictFilter-${var.environment}"
  pattern        = "{ ($.statusCode = 409) && ($.message = \"*ConflictException*\") }"
  log_group_name = var.api_log_group_name

  metric_transformation {
    name      = "SchedulingConflictCount"
    namespace = "CFCHub/${var.environment}"
    value     = "1"
  }
}

resource "aws_cloudwatch_metric_alarm" "scheduling_conflict_spike" {
  alarm_name          = "CFCHub-${var.environment}-SchedulingConflictSpike"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = aws_cloudwatch_log_metric_filter.scheduling_conflict.metric_transformation[0].name
  namespace           = aws_cloudwatch_log_metric_filter.scheduling_conflict.metric_transformation[0].namespace
  period              = 60 # 1 min
  statistic           = "Sum"
  threshold           = 10
  alarm_description   = "Alarm when HTTP 409 on scheduling > 10/min"
  alarm_actions       = [aws_sns_topic.email.arn]
}

# 4. MedicalFileDirectAccess
resource "aws_cloudwatch_log_metric_filter" "medical_direct_access" {
  name           = "MedicalDirectAccessFilter-${var.environment}"
  # Looking for GetObject on the medical bucket where X-Amz-Signature (used in presigned URLs) is absent
  pattern        = "\"REST.GET.OBJECT\" -\"X-Amz-Signature=\""
  log_group_name = aws_cloudwatch_log_group.s3_access_logs.name

  metric_transformation {
    name      = "MedicalDirectAccessCount"
    namespace = "CFCHub/${var.environment}"
    value     = "1"
  }
}

resource "aws_cloudwatch_metric_alarm" "medical_direct_access" {
  alarm_name          = "CFCHub-${var.environment}-Security-MedicalFileDirectAccess"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = aws_cloudwatch_log_metric_filter.medical_direct_access.metric_transformation[0].name
  namespace           = aws_cloudwatch_log_metric_filter.medical_direct_access.metric_transformation[0].namespace
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  alarm_description   = "SECURITY: Alarm when medical bucket is accessed directly (not via pre-signed URL)"
  alarm_actions       = [aws_sns_topic.pagerduty.arn]
}

# 5. ALB latency p99 > 2s
resource "aws_cloudwatch_metric_alarm" "alb_latency" {
  alarm_name          = "CFCHub-${var.environment}-AlbLatencyP99"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "TargetResponseTime"
  namespace           = "AWS/ApplicationELB"
  period              = 300
  extended_statistic  = "p99"
  threshold           = 2
  alarm_description   = "Alarm when ALB latency p99 > 2s"
  alarm_actions       = [aws_sns_topic.email.arn]

  dimensions = {
    TargetGroup  = var.target_group_arn_suffix
    LoadBalancer = var.alb_arn_suffix
  }
}
