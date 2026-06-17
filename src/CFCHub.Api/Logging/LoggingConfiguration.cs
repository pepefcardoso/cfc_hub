using System;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.AwsCloudWatch;
using Amazon.CloudWatchLogs;

namespace CFCHub.Api.Logging;

public static class LoggingConfiguration
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        var env = Environment.GetEnvironmentVariable("CFCHUB_ENVIRONMENT") ?? "dev";
        var isDev = env.Equals("dev", StringComparison.OrdinalIgnoreCase);

        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("environment", env)
            .Destructure.With<SensitiveDataDestructuringPolicy>();

        if (isDev)
        {
            loggerConfig
                .MinimumLevel.Debug()
                .WriteTo.Seq("http://seq:5341");
        }
        else
        {
            loggerConfig
                .MinimumLevel.Information()
                .Filter.ByExcluding(e => e.Level == LogEventLevel.Debug)
                .WriteTo.AmazonCloudWatch(new CloudWatchSinkOptions
                {
                    LogGroupName = $"/cfchub/{env}/api",
                    MinimumLogEventLevel = LogEventLevel.Information
                }, new AmazonCloudWatchLogsClient());
        }

        builder.Host.UseSerilog(loggerConfig.CreateLogger());
    }
}
