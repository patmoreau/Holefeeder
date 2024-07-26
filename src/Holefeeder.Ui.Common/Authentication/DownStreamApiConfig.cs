// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Holefeeder.Ui.Common.Authentication;

public sealed class DownStreamApiConfig
{
    /// <summary>
    /// Gets or sets the base uri of the api.
    /// </summary>
    /// <value>
    /// The api base uri.
    /// </value>
    public required Uri BaseUrl { get; init; }

    /// <summary>
    /// Gets or sets the scopes for MS graph call.
    /// </summary>
    /// <value>
    /// The scopes.
    /// </value>
    public required string Scopes { get; init; }

    /// <summary>
    /// Gets the scopes in a format as expected by the various MSAL SDK methods.
    /// </summary>
    /// <value>
    /// The scopes.
    /// </value>
    public IReadOnlyList<string> ScopesArray => Scopes.Split(' ');
}
