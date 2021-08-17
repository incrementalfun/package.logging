﻿using System.Reflection;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Formatting.Compact;
using Serilog.Sinks.AwsCloudWatch;
using Serilog.Sinks.AwsCloudWatch.LogStreamNameProvider;

namespace Incremental.Common.Logging
{
    /// <summary>
    ///     Common Logger extensions.
    /// </summary>
    public static class WebHostBuilderExtensions
    {
        /// <summary>
        ///     Configures common logging.
        /// </summary>
        /// <param name="webHostBuilder">
        ///     <see cref="IWebHostBuilder" />
        /// </param>
        public static void AddCommonLogging(this IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.UseSerilog((context, configuration) =>
            {
                configuration
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                        .WithDefaultDestructurers()
                        .WithDestructurers(new[] {new DbUpdateExceptionDestructurer()}));

                if (context.HostingEnvironment.IsDevelopment())
                    configuration
                        .WriteTo.Console();

                configuration
                    .WriteTo.AmazonCloudWatch(new CloudWatchSinkOptions
                    {
                        LogGroupName = $"{context.Configuration["LOG_GROUP_NAME"]}#{context.HostingEnvironment.EnvironmentName}",
                        LogStreamNameProvider =
                            new ConfigurableLogStreamNameProvider($"{Assembly.GetEntryAssembly()?.GetName().Name}", false, false),
                        CreateLogGroup = true,
                        TextFormatter = new CompactJsonFormatter(),
                        MinimumLogEventLevel = context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information
                    }, new AmazonCloudWatchLogsClient(context.Configuration.GetAWSCredentials(), context.Configuration.GetAWSRegion()));
            });
        }

        private static BasicAWSCredentials GetAWSCredentials(this IConfiguration configuration)
        {
            return new(configuration["AWS_ACCESS_KEY"], configuration["AWS_SECRET_KEY"]);
        }

        private static RegionEndpoint GetAWSRegion(this IConfiguration configuration)
        {
            return RegionEndpoint.GetBySystemName(configuration["AWS_REGION"] ?? "eu-west-1");
        }
    }
}