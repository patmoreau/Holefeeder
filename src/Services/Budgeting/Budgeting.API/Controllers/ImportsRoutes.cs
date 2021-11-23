﻿using System;
using System.Threading.Tasks;

using DrifterApps.Holefeeder.Budgeting.Application.Imports.Commands;
using DrifterApps.Holefeeder.Budgeting.Application.Imports.Queries;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DrifterApps.Holefeeder.Budgeting.API.Controllers;

public static class ImportsRoutes
{
    public static WebApplication AddImportsRoutes(this WebApplication app)
    {
        const string routePrefix = "api/v2/imports";

        app.MapPost($"{routePrefix}/import-data", ImportData)
            .WithName(nameof(ImportData))
            .AddOptions();

        app.MapGet($"{routePrefix}/{{id}}", ImportDataStatus)
            .WithName(nameof(ImportDataStatus))
            .AddOptions();

        return app;
    }

    private static async Task<IResult> ImportData(ImportData.Request command, IMediator mediator)
    {
        var requestResult = await mediator.Send(command);
        return requestResult.Match(
            result => Results.AcceptedAtRoute(nameof(ImportDataStatus), new { Id = result }, new { Id = result }),
            result => Results.ValidationProblem(
                result.Errors,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "https://httpstatuses.com/422")
        );
    }

    private static async Task<IResult> ImportDataStatus(Guid id, IMediator mediator)
    {
        var requestResult = await mediator.Send(new ImportDataStatus.Request(id));
        return requestResult.Match(
            result => Results.Ok(result),
            _ => Results.NotFound()
        );
    }

    private static RouteHandlerBuilder AddOptions(this RouteHandlerBuilder builder) =>
        builder
            .WithTags("Imports")
            .RequireAuthorization()
            .WithGroupName("v2");
}
