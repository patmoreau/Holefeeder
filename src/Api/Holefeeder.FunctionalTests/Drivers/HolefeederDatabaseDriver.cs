using System.Data.Common;

using Holefeeder.Infrastructure.Context;
using Holefeeder.Infrastructure.SeedWork;

using Microsoft.Extensions.DependencyInjection;

using Respawn;
using Respawn.Graph;

namespace Holefeeder.FunctionalTests.Drivers;

public class HolefeederDatabaseDriver : DatabaseDriver
{
    protected override MySqlDbContext DbContext { get; }

    public HolefeederDatabaseDriver(ApiApplicationDriver apiApplicationDriver)
    {
        if (apiApplicationDriver == null)
        {
            throw new ArgumentNullException(nameof(apiApplicationDriver));
        }

        var settings = apiApplicationDriver.Services.GetRequiredService<HolefeederDatabaseSettings>();
        var context = new HolefeederContext(settings);

        DbContext = context;
    }

    protected override async Task<Respawner> CreateStateAsync(DbConnection connection)
    {
        return await Respawner.CreateAsync(connection,
            new RespawnerOptions
            {
                SchemasToInclude = new[] {"budgeting_functional_tests"},
                DbAdapter = DbAdapter.MySql,
                TablesToInclude = new Table[] {"accounts", "cashflows", "categories", "transactions"},
                TablesToIgnore = new Table[] {"schema_versions"}
            });
    }
}
