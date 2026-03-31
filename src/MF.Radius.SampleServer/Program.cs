using MF.Radius.Core.Extensions;
using MF.Radius.Core.Interfaces;
using MF.Radius.Core.Options;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Nas.Commands;
using MF.Radius.SampleServer.Application.Features.Nas.Events;
using MF.Radius.SampleServer.Application.Features.Nas.Handlers;
using MF.Radius.SampleServer.Application.Features.Nas.Interfaces;
using MF.Radius.SampleServer.Application.Features.Nas.Models;
using MF.Radius.SampleServer.Application.Features.Sessions.Events;
using MF.Radius.SampleServer.Application.Features.Sessions.Interfaces;
using MF.Radius.SampleServer.Application.Features.Sessions.Queries;
using MF.Radius.SampleServer.Application.Features.Subscribers.Events;
using MF.Radius.SampleServer.Application.Features.Subscribers.Interfaces;
using MF.Radius.SampleServer.Application.Features.Subscribers.Queries;
using MF.Radius.SampleServer.Application.Interfaces.Packets;
using MF.Radius.SampleServer.Application.Options;
using MF.Radius.SampleServer.Infrastructure.Data;
using MF.Radius.SampleServer.Infrastructure.Events;
using MF.Radius.SampleServer.Infrastructure.Messaging;
using MF.Radius.SampleServer.Infrastructure.Metrics;
using MF.Radius.SampleServer.Infrastructure.Metrics.Events;
using MF.Radius.SampleServer.Infrastructure.Network;
using MF.Radius.SampleServer.Infrastructure.Radius;
using MF.Radius.SampleServer.Infrastructure.Radius.Packets;
using MF.Radius.SampleServer.Infrastructure.Storage;
using MF.Radius.SampleServer.Presentation.WebApi.Endpoints;
using MF.Radius.SampleServer.Shared.DTOs;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI is useful for demo users and third-party integrators.
builder.Services.AddOpenApi();

// Bind options for sender and ISP-specific RADIUS reply attributes.
builder.Services.Configure<RadiusListenerOptions>(builder.Configuration.GetSection("RadiusListener"));
builder.Services.Configure<RadiusSenderOptions>(builder.Configuration.GetSection("RadiusSender"));
builder.Services.Configure<RadiusIspOptions>(builder.Configuration.GetSection("RadiusIsp"));

// Register core RADIUS server runtime (listener + processor + sender).
builder.Services.AddRadiusServer<IspRadiusProcessor>();

// Register packet factories and NAS command gateway (CoA / Disconnect).
builder.Services.AddSingleton<IAuthResponsePacketFactory, AuthResponsePacketFactory>();
builder.Services.AddSingleton<IAcctResponsePacketFactory, AcctResponsePacketFactory>();
builder.Services.AddSingleton<INasCommandGateway, RadiusNasCommandGateway>();
builder.Services.AddSingleton<ICoaRequestPacketFactory, CoaRequestCiscoPacketFactory>();
builder.Services.AddSingleton<IDisconnectRequestPacketFactory, DisconnectRequestPacketFactory>();

// Register lightweight CQRS dispatchers.
builder.Services.AddSingleton<IQueryDispatcher, InProcessQueryDispatcher>();
builder.Services.AddSingleton<ICommandDispatcher, InProcessCommandDispatcher>();
builder.Services.AddSingleton<IApplicationEventPublisher, InProcessApplicationEventPublisher>();

// Register command handlers.
builder.Services.AddSingleton<ICommandHandler<RestrictSessionCommand, NasCommandResult>, RestrictSessionCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<DisconnectSessionCommand, NasCommandResult>, DisconnectSessionCommandHandler>();

// Register query handlers.
builder.Services.AddSingleton<IQueryHandler<GetSubscriberQuery, SubscriberDto?>, GetSubscriberQueryHandler>();
builder.Services.AddSingleton<IQueryHandler<GetSessionsByUserNameQuery, IReadOnlyList<SessionDto>>, GetSessionsByUserNameQueryHandler>();
builder.Services.AddSingleton<IQueryHandler<GetSessionByNasAndIdQuery, SessionDto?>, GetSessionByNasAndIdQueryHandler>();
builder.Services.AddSingleton<IQueryHandler<GetSessionsAllQuery, IReadOnlyList<SessionDto>>, GetSessionsAllHandler>();

// Register event handlers.
builder.Services.AddSingleton<IApplicationEventHandler<NasCommandCompletedEvent>, NasCommandCompletedLoggingHandler>();

// Register demo in-memory storages.
// Replace these with persistent implementations (PostgreSQL/Redis/etc.) in production.
builder.Services.AddSingleton<ISubscriberRepository, InMemorySubscriberRepository>();
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();

// Demo shared secret resolver (single static secret for all NAS clients).
// This is intentionally simple for the sample host.
var sharedSecret = builder.Configuration["DemoSecurity:SharedSecret"] ?? "testing123";
builder.Services.AddSingleton<IRadiusSharedSecretResolver>(_ => new StaticSharedSecretResolver(sharedSecret));

// Metrics
builder.Services.AddSingleton<RadiusMetrics>();
builder.Services.AddSingleton<IApplicationEventHandler<SubscriberAuthenticatedEvent>, SubscriberAuthenticatedMetricsHandler>();
builder.Services.AddSingleton<IApplicationEventHandler<SubscriberAuthRejectedEvent>, SubscriberAuthRejectedMetricsHandler>();
builder.Services.AddSingleton<IApplicationEventHandler<AcctPacketProcessedEvent>, AcctPacketProcessedMetricsHandler>();
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(MetricsConstants.RadiusMeterName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Public API surface grouped under /api.
var api = app.MapGroup("/api");
api.MapSessionEndpoints();
api.MapSubscriberEndpoints();
api.MapNasCommandEndpoints();
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();
