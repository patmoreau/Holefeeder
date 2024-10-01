using DrifterApps.Seeds.Domain;
using DrifterApps.Seeds.Testing;

using Holefeeder.Domain.Enumerations;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.Features.Users;
using Holefeeder.Domain.ValueObjects;
using Holefeeder.Tests.Common.Builders;

using Xunit.Abstractions;

using static Holefeeder.Tests.Common.Builders.Transactions.CashflowBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.TransactionBuilder;

namespace Holefeeder.UnitTests.Domain.Features.Transactions;

[UnitTest, Category("Domain")]
public class CashflowTests(ITestOutputHelper testOutputHelper)
{
    private readonly Driver _driver = new();

    [Theory, ClassData(typeof(CreateValidationData))]
    public void GivenCreate_WhenInvalidData_ThenReturnFailure(
        DateOnly effectiveDate,
        int frequency,
        int recurrence,
        AccountId accountId,
        CategoryId categoryId,
        UserId userId,
        ResultError error)
    {
        // arrange
        var driver = _driver
            .WithEffectiveDate(effectiveDate)
            .WithFrequency(frequency)
            .WithRecurrence(recurrence)
            .WithAccount(accountId)
            .WithCategory(categoryId)
            .WithUser(userId);

        // act
        var result = driver.Build();

        // assert
        result.Should().BeFailure()
            .WithError(ResultAggregateError.CreateValidationError([error]));
    }

    [Theory, ClassData(typeof(ImportValidationData))]
    public void GivenImport_WhenInvalidData_ThenReturnFailure(
        CashflowId id,
        DateOnly effectiveDate,
        int frequency,
        int recurrence,
        AccountId accountId,
        CategoryId categoryId,
        UserId userId,
        ResultError error)
    {
        // arrange
        var driver = _driver
            .WithId(id)
            .WithEffectiveDate(effectiveDate)
            .WithFrequency(frequency)
            .WithRecurrence(recurrence)
            .WithAccount(accountId)
            .WithCategory(categoryId)
            .WithUser(userId);

        // act
        var result = driver.BuildWithImport();

        // assert
        result.Should().BeFailure()
            .WithError(ResultAggregateError.CreateValidationError([error]));
    }

    [Fact]
    public void GivenCancel_WhenCashflowActive_ThenCashflowIsInactive()
    {
        // arrange
        var cashflow = _driver.IsActive().Build().Value;

        // act
        var result = cashflow.Cancel();

        // assert
        using var scope = new AssertionScope();
        result.Should().BeSuccessful();
        result.Value.Inactive.Should().BeTrue();
    }

    [Fact]
    public void GivenSetTags_WhenEmptyList_ThenTagListIsEmpty()
    {
        // arrange
        var cashflow = _driver.Build().Value;

        // act
        var result = cashflow.SetTags();

        // assert
        result.Should().BeSuccessful();
        result.Value.Tags.Should().BeEmpty();
    }

    [Fact]
    public void GivenSetTags_WhenAddingTags_ThenTagListIsSet()
    {
        // arrange
        var cashflow = _driver.Build().Value;
        string[]? newTags = Fakerizer.Random.WordsArray(RandomCollectionCount());

        // act
        var result = cashflow.SetTags(newTags);

        // assert
        result.Should().BeSuccessful();
        result.Value.Tags.Should().Contain(newTags.Select(x => x.ToLowerInvariant()));
    }

    [Fact]
    public void GivenLastPaidDate_WhenNoTransactions_ThenReturnNull()
    {
        // arrange
        var cashflow = GivenAnActiveCashflow().Build();

        // act
        var result = cashflow.LastPaidDate;

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenLastPaidDate_WhenTransactions_ThenReturnMaxDate()
    {
        // arrange
        var cashflow = GivenAnActiveCashflow().WithTransactions().Build();

        // act
        var result = cashflow.LastPaidDate;

        // assert
        result.Should().Be(cashflow.Transactions.OrderByDescending(x => x.Date).First().Date);
    }

    [Fact]
    public void GivenLastCashflowDate_WhenNoTransactions_ThenReturnNull()
    {
        // arrange
        var cashflow = _driver.Build().Value;

        // act
        var result = cashflow.LastCashflowDate;

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenLastCashflowDate_WhenTransactions_ThenReturnMaxCashflowDate()
    {
        // arrange
        var cashflow = GivenAnActiveCashflow()
            .WithTransactions()
            .Build();

        // act
        var result = cashflow.LastCashflowDate;

        // assert
        result.Should().Be(cashflow.Transactions.OrderByDescending(x => x.Date).First().CashflowDate);
    }

    [Fact]
    public void GivenGetUpcoming_WhenInactiveCashflow_ThenReturnEmptyList()
    {
        // arrange
        var cashflow = _driver.Build().Value;
        cashflow = cashflow.Cancel().Value;

        // act
        var result = cashflow.GetUpcoming(Fakerizer.Date.FutureDateOnly());

        // assert
        result.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(GetUpcomingWithNonePaidTestCases))]
    public void GivenGetUpcoming_WhenNoCashflowsPaid_ThenReturnExpectedDates(
        string testName,
        (DateIntervalType IntervalType, int Frequency, DateOnly EffectiveDate) info,
        DateOnly to,
        IReadOnlyCollection<DateOnly> expected)
    {
        testOutputHelper.WriteLine(testName);

        ArgumentNullException.ThrowIfNull(expected);

        // arrange
        var cashflow = GivenAnActiveCashflow()
            .OfFrequency(info.IntervalType, info.Frequency)
            .OnEffectiveDate(info.EffectiveDate)
            .Build();

        // act
        var result = cashflow.GetUpcoming(to);

        // assert
        result.Should().HaveCount(expected.Count).And.Equal(expected);
    }

    [Theory]
    [MemberData(nameof(GetUpcomingWithPaidTestCases))]
    public void GivenGetUpcoming_WhenSomeCashflowsPaid_ThenReturnExpectedDates(
        string testName,
        (DateIntervalType IntervalType, int Frequency, DateOnly EffectiveDate) info,
        DateOnly to,
        DateOnly lastPaidDate,
        IReadOnlyCollection<DateOnly> expected)
    {
        testOutputHelper.WriteLine(testName);

        ArgumentNullException.ThrowIfNull(expected);

        // arrange
        var transaction = GivenATransaction()
            .OnDate(lastPaidDate.AddDays(Fakerizer.Random.Int(0, 10)))
            .ForCashflowDate(lastPaidDate)
            .Build();
        var cashflow = GivenAnActiveCashflow()
            .OfFrequency(info.IntervalType, info.Frequency)
            .OnEffectiveDate(info.EffectiveDate)
            .WithTransactions(transaction)
            .Build();

        // act
        var result = cashflow.GetUpcoming(to);

        // assert
        result.Should().HaveCount(expected.Count).And.Equal(expected);
    }

    private sealed class Driver : IDriverOf<Result<Cashflow>>
    {
        private static readonly Faker Faker = new();
        private CashflowId _id = CashflowId.New;
        private DateOnly _effectiveDate = Faker.Date.RecentDateOnly();
        private readonly DateIntervalType _intervalType = Fakerizer.PickRandom<DateIntervalType>(DateIntervalType.List);
        private int _frequency = Faker.Random.Int(1, 10);
        private int _recurrence = Faker.Random.Int(1, 10);
        private readonly Money _amount = MoneyBuilder.Create().Build();
        private readonly string _description = Faker.Lorem.Sentence();
        private AccountId _accountId = (AccountId)Faker.Random.Guid();
        private CategoryId _categoryId = (CategoryId)Faker.Random.Guid();
        private bool _inactive;
        private UserId _userId = (UserId)Faker.Random.Guid();

        public Result<Cashflow> Build() =>
            Cashflow.Create(
                _effectiveDate,
                _intervalType,
                _frequency,
                _recurrence,
                _amount,
                _description,
                _categoryId,
                _accountId,
                _userId);

        public Result<Cashflow> BuildWithImport() =>
            Cashflow.Import(_id,
                _effectiveDate,
                _intervalType,
                _frequency,
                _recurrence,
                _amount,
                _description,
                _categoryId,
                _accountId,
                _inactive,
                _userId);

        public Driver WithId(CashflowId id)
        {
            _id = id;
            return this;
        }

        public Driver WithEffectiveDate(DateOnly effectiveDate)
        {
            _effectiveDate = effectiveDate;
            return this;
        }

        public Driver WithFrequency(int frequency)
        {
            _frequency = frequency;
            return this;
        }

        public Driver WithRecurrence(int recurrence)
        {
            _recurrence = recurrence;
            return this;
        }

        public Driver WithAccount(AccountId accountId)
        {
            _accountId = accountId;
            return this;
        }

        public Driver WithCategory(CategoryId category)
        {
            _categoryId = category;
            return this;
        }

        public Driver IsActive()
        {
            _inactive = false;
            return this;
        }

        public Driver IsInactive()
        {
            _inactive = true;
            return this;
        }

        public Driver WithUser(UserId userId)
        {
            _userId = userId;
            return this;
        }

        public void ShouldBeValid(Cashflow value)
        {
            using var scope = new AssertionScope();
            value.Id.Should().Be(_id);
            value.EffectiveDate.Should().Be(_effectiveDate);
            value.Amount.Should().Be(_amount);
            value.Description.Should().Be(_description);
            value.AccountId.Should().Be(_accountId);
            value.CategoryId.Should().Be(_categoryId);
            value.Frequency.Should().Be(_frequency);
            value.Recurrence.Should().Be(_recurrence);
            value.UserId.Should().Be(_userId);
        }
    }

    internal sealed class CreateValidationData :
        TheoryData<DateOnly, int, int, AccountId, CategoryId, UserId, ResultError>
    {
        public CreateValidationData()
        {
            Add(default,
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.EffectiveDateRequired);
            Add(Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(max: 0),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.FrequencyInvalid);
            Add(Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(max: -1),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.RecurrenceInvalid);
            Add(Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                AccountId.Empty,
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.AccountIdRequired);
            Add(Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                CategoryId.Empty,
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.CategoryIdRequired);
            Add(Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                UserId.Empty,
                CashflowErrors.UserIdRequired);
        }
    }

    internal sealed class ImportValidationData :
        TheoryData<CashflowId, DateOnly, int, int, AccountId, CategoryId, UserId, ResultError>
    {
        public ImportValidationData()
        {
            Add(CashflowId.Empty,
                Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.IdRequired);
            Add((CashflowId)Fakerizer.Random.Guid(),
                default,
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.EffectiveDateRequired);
            Add((CashflowId)Fakerizer.Random.Guid(),
                Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(max: 0),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.FrequencyInvalid);
            Add((CashflowId)Fakerizer.Random.Guid(),
                Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(max: -1),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.RecurrenceInvalid);
            Add((CashflowId)Fakerizer.Random.Guid(),
                Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                AccountId.Empty,
                (CategoryId)Fakerizer.Random.Guid(),
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.AccountIdRequired);
            Add((CashflowId)Fakerizer.Random.Guid(),
                Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                CategoryId.Empty,
                (UserId)Fakerizer.Random.Guid(),
                CashflowErrors.CategoryIdRequired);
            Add((CashflowId)Fakerizer.Random.Guid(),
                Fakerizer.Date.RecentDateOnly(),
                Fakerizer.Random.Int(1),
                Fakerizer.Random.Int(0),
                (AccountId)Fakerizer.Random.Guid(),
                (CategoryId)Fakerizer.Random.Guid(),
                UserId.Empty,
                CashflowErrors.UserIdRequired);
        }
    }

    public static IEnumerable<object[]> GetUpcomingWithNonePaidTestCases
    {
        get
        {
            yield return new object[]
            {
                "OneTime Cashflow effective after date returns nothing",
                (IntervalType: DateIntervalType.OneTime, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2022, 12, 31), Array.Empty<DateOnly>()
            };
            yield return new object[]
            {
                "OneTime Cashflow effective before date returns single date",
                (IntervalType: DateIntervalType.OneTime, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2023, 12, 31), new[] {new DateOnly(2023, 1, 1)}
            };
            yield return new object[]
            {
                "Weekly Cashflow effective after date returns nothing",
                (IntervalType: DateIntervalType.Weekly, Frequency: 2, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2022, 12, 31), Array.Empty<DateOnly>()
            };
            yield return new object[]
            {
                "Weekly Cashflow effective before date returns 27 iterations",
                (IntervalType: DateIntervalType.Weekly, Frequency: 2, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2023, 12, 31),
                new[]
                {
                    new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 15), new DateOnly(2023, 1, 29),
                    new DateOnly(2023, 2, 12), new DateOnly(2023, 2, 26), new DateOnly(2023, 3, 12),
                    new DateOnly(2023, 3, 26), new DateOnly(2023, 4, 9), new DateOnly(2023, 4, 23),
                    new DateOnly(2023, 5, 7), new DateOnly(2023, 5, 21), new DateOnly(2023, 6, 4),
                    new DateOnly(2023, 6, 18), new DateOnly(2023, 7, 2), new DateOnly(2023, 7, 16),
                    new DateOnly(2023, 7, 30), new DateOnly(2023, 8, 13), new DateOnly(2023, 8, 27),
                    new DateOnly(2023, 9, 10), new DateOnly(2023, 9, 24), new DateOnly(2023, 10, 8),
                    new DateOnly(2023, 10, 22), new DateOnly(2023, 11, 5), new DateOnly(2023, 11, 19),
                    new DateOnly(2023, 12, 3), new DateOnly(2023, 12, 17), new DateOnly(2023, 12, 31)
                }
            };
            yield return new object[]
            {
                "Monthly Cashflow effective after date returns nothing",
                (IntervalType: DateIntervalType.Monthly, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2022, 12, 31), Array.Empty<DateOnly>()
            };
            yield return new object[]
            {
                "Monthly Cashflow effective before date returns 12 iterations",
                (IntervalType: DateIntervalType.Monthly, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2023, 12, 31),
                new[]
                {
                    new DateOnly(2023, 1, 1), new DateOnly(2023, 2, 1), new DateOnly(2023, 3, 1),
                    new DateOnly(2023, 4, 1), new DateOnly(2023, 5, 1), new DateOnly(2023, 6, 1),
                    new DateOnly(2023, 7, 1), new DateOnly(2023, 8, 1), new DateOnly(2023, 9, 1),
                    new DateOnly(2023, 10, 1), new DateOnly(2023, 11, 1), new DateOnly(2023, 12, 1)
                }
            };
            yield return new object[]
            {
                "Yearly Cashflow effective after date returns nothing",
                (IntervalType: DateIntervalType.Yearly, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2022, 12, 31), Array.Empty<DateOnly>()
            };
            yield return new object[]
            {
                "Yearly Cashflow effective before date returns 2 iterations",
                (IntervalType: DateIntervalType.Yearly, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2024, 12, 31), new[] {new DateOnly(2023, 1, 1), new DateOnly(2024, 1, 1)}
            };
        }
    }

    public static IEnumerable<object[]> GetUpcomingWithPaidTestCases
    {
        get
        {
            yield return new object[]
            {
                "OneTime Cashflow already paid returns nothing",
                (IntervalType: DateIntervalType.OneTime, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2023, 12, 31), new DateOnly(2023, 1, 1), Array.Empty<DateOnly>()
            };
            yield return new object[]
            {
                "Weekly Cashflow effective before date returns 2 iterations",
                (IntervalType: DateIntervalType.Weekly, Frequency: 2, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2023, 12, 31), new DateOnly(2023, 12, 3),
                new[] {new DateOnly(2023, 12, 17), new DateOnly(2023, 12, 31)}
            };
            yield return new object[]
            {
                "Monthly Cashflow effective before date returns 12 iterations",
                (IntervalType: DateIntervalType.Monthly, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2023, 12, 31), new DateOnly(2023, 7, 1),
                new[]
                {
                    new DateOnly(2023, 8, 1), new DateOnly(2023, 9, 1), new DateOnly(2023, 10, 1),
                    new DateOnly(2023, 11, 1), new DateOnly(2023, 12, 1)
                }
            };
            yield return new object[]
            {
                "Yearly Cashflow effective before date returns 2 iterations",
                (IntervalType: DateIntervalType.Yearly, Frequency: 1, EffectiveDate: new DateOnly(2023, 1, 1)),
                new DateOnly(2024, 12, 31), new DateOnly(2023, 1, 1), new[] {new DateOnly(2024, 1, 1)}
            };
        }
    }
}
