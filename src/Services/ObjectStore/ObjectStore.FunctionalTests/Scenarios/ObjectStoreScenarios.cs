﻿using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using DrifterApps.Holefeeder.Framework.SeedWork.Infrastructure;
using DrifterApps.Holefeeder.ObjectStore.API;
using DrifterApps.Holefeeder.ObjectStore.Application.Commands;
using DrifterApps.Holefeeder.ObjectStore.Application.Models;
using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace ObjectStore.FunctionalTests.Scenarios
{
    public class ObjectStoreScenarios : IClassFixture<ObjectStoreWebApplicationFactory>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public ObjectStoreScenarios(ObjectStoreWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GivenGetStoreItems_WhenNoFilterApplied_ThenReturnAllItems()
        {
            // Arrange
            var client = _factory.CreateDefaultClient();
            const string request = "/api/v2/StoreItems/";

            // Act
            var response = await client.GetAsync(request);

            // Assert
            response.StatusCodeShouldBeSuccess();

            var result = await response.Content.ReadFromJsonAsync<StoreItemViewModel[]>(_jsonSerializerOptions);

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GivenGetStoreItems_WhenFilterCode2Applied_ThenReturnItem()
        {
            // Arrange
            var client = _factory.CreateDefaultClient();
            const string request = "/api/v2/StoreItems?filter=code=Code2";

            // Act
            var response = await client.GetAsync(request);

            // Assert
            response.StatusCodeShouldBeSuccess();

            var result = await response.Content.ReadFromJsonAsync<StoreItemViewModel[]>(_jsonSerializerOptions);

            result.Should().BeEquivalentTo(new StoreItemViewModel(StoreItemContextSeed.Guid2, "Code2", "Data2"));
        }

        [Fact]
        public async Task GivenGetStoreItems_WhenQueryParamsApplied_ThenReturnItemsInProperOrder()
        {
            // Arrange
            var client = _factory.CreateDefaultClient();
            const string request = "/api/v2/StoreItems?sort=-code&offset=1&limit=2";

            // Act
            var response = await client.GetAsync(request);

            // Assert
            response.StatusCodeShouldBeSuccess();

            var result = await response.Content.ReadFromJsonAsync<StoreItemViewModel[]>(_jsonSerializerOptions);

            result.Should()
                .BeEquivalentTo(new StoreItemViewModel(StoreItemContextSeed.Guid2, "Code2", "Data2"),
                    new StoreItemViewModel(StoreItemContextSeed.Guid1, "Code1", "Data1"));
        }

        [Fact]
        public async Task GivenGetStoreItem_WhenValidId_ThenReturnItem()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = $"/api/v2/StoreItems/{StoreItemContextSeed.Guid1.ToString()}";

            // Act
            var response = await client.GetAsync(request);

            // Assert
            response.StatusCodeShouldBeSuccess();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StoreItemViewModel>(json, _jsonSerializerOptions);

            result.Should().BeEquivalentTo(new StoreItemViewModel(StoreItemContextSeed.Guid1, "Code1", "Data1"));
        }

        [Fact]
        public async Task GivenGetStoreItem_WhenInvalidId_ThenReturnBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = $"/api/v2/StoreItems/{Guid.Empty}";

            // Act
            var response = await client.GetAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task GivenGetStoreItem_WhenIdDoesntExist_ThenReturnNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = $"/api/v2/StoreItems/{Guid.NewGuid()}";

            // Act
            var response = await client.GetAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task GivenCreateObjectCommand_WhenCommandValid_ThenObjectCreated()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.TEST_USER_ID_HEADER, Guid.NewGuid().ToString());
            var request = $"/api/v2/StoreItems/create-store-item";

            // Act
            var command = new CreateStoreItemCommand("New Code", "New Data");
            var response = await client.PostAsJsonAsync(request, command);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CommandResult<Guid>>(_jsonSerializerOptions);

            result.Should().NotBeNull();
            response.Headers.Location?.AbsolutePath.Should().BeEquivalentTo($"/api/v2/StoreItems/create-store-item");
            result.Status.Should().Be(CommandStatus.Created);
            result.Result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GivenCreateObjectCommand_WhenCommandInvalid_ThenObjectCreated()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = $"/api/v2/StoreItems/create-store-item";

            // Act
            var response = await client.PostAsJsonAsync(request, new {});

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }
    }
}
