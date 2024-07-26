using Holefeeder.Ui.Common.Models;

using Refit;

namespace Holefeeder.Ui.Common.Services;

public interface IHolefeederApiService
{
    [Get("/api/v2/enumerations/get-account-types")]
    Task<ApiResponse<IList<string>>> GetAccountTypes();

    [Get("/api/v2/store-items/?filter=code:eq:{code}")]
    Task<ApiResponse<IList<StoreItem>>> GetStoreItem(string code);

    [Get("/api/v2/cashflows/get-upcoming?from={fromDate}&to={toDate}&sort=-date")]
    Task<ApiResponse<IReadOnlyList<Upcoming>>> GetUpcomingCashflows(DateOnly fromDate, DateOnly toDate);
}
