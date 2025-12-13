using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tests.ControllerTests;
using Xunit;

namespace Tests.ControllerTest.Admin
{
    [Collection("IntegrationTests")]
    public class StationControllerTest : IAsyncLifetime
    {
        private HttpClient _client = null!;
        private CustomAppFact _factory = null!;

        public async Task InitializeAsync()
        {
            _factory = new CustomAppFact();
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-admin-token");
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_200OK()
        {
            //Arrange
            var url = "/api/admin/station/list?PageNumber=1&PageSize=10&Search=test&SortBy=brandname";

            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.GetProperty("items").ValueKind == JsonValueKind.Array);
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var url = "/api/admin/station/list?PageNumber=1&PageSize=10&Search=test&SortBy=brandname";

            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var url = "/api/admin/station/list?PageNumber=1&PageSize=10&Search=test&SortBy=brandname";

            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetStationInfoForEditAsyncTest_200OK()
        {
            //Arrange
            var url = "/api/admin/station/edit/info?BrandName=Orlen&Street=test&HouseNumber=1&City=test";

            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(content));
            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.True(root.GetProperty("data").ValueKind == JsonValueKind.Object);
        }

        [Fact]
        public async Task GetStationInfoForEditAsyncTest_400()
        {
            //Arrange
            var url = "/api/admin/station/edit/info?BrandName=&Street=&HouseNumber=1&City=test";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetStationInfoForEditAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var url = "/api/admin/station/edit/info?BrandName=Orlen&Street=test&HouseNumber=1&City=test";

            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetStationInfoForEditAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var url = "/api/admin/station/edit/info?BrandName=Orlen&Street=test&HouseNumber=1&City=test";

            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetStationInfoForEditAsyncTest_404()
        {
            //Arrange
            var url = "/api/admin/station/edit/info?BrandName=BAD&Street=test&HouseNumber=1&City=test";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task EditStationAsyncTest_200OK()
        {
            //Arrange
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "Orlen",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                NewCity = "UpdatedCityForTest"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/admin/station/edit", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("success", responseContent);
        }

        [Fact]
        public async Task EditStationAsyncTest_400()
        {
            //Arrange
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "",
                    Street = "",
                    HouseNumber = "",
                    City = ""
                },
                NewCity = ""
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/admin/station/edit", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task EditStationAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "Orlen",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                NewCity = "UpdatedCityForTest"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PatchAsync("/api/admin/station/edit", content);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task EditStationAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "Orlen",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                NewCity = "UpdatedCityForTest"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PatchAsync("/api/admin/station/edit", content);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddNewStationAsyncTest_201()
        {
            //Arrange
            var request = new AddStationRequest
            {
                BrandName = "Orlen",
                Street = "NewStreetTest",
                HouseNumber = "99",
                City = "NewCityTest",
                PostalCode = "11-111",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    new AddFuelTypeAndPriceRequest
                    {
                        Name = "Diesel",
                        Code = "ON",
                        Price = 6.5m,
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/admin/station/add", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Contains("success", responseContent, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AddNewStationAsyncTest_400()
        {
            //Arrange
            var request = new AddStationRequest
            {
                BrandName = "",
                Street = "",
                HouseNumber = "",
                City = "",
                PostalCode = "",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    new AddFuelTypeAndPriceRequest
                    {
                        Name = "Diesel",
                        Code = "ON",
                        Price = 6.5m,
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/admin/station/add", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddNewStationAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var request = new AddStationRequest
            {
                BrandName = "Orlen",
                Street = "NewStreetTest",
                HouseNumber = "99",
                City = "NewCityTest",
                PostalCode = "11-111",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    new AddFuelTypeAndPriceRequest
                    {
                        Name = "Diesel",
                        Code = "ON",
                        Price = 6.5m,
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await unauthClient.PostAsync("/api/admin/station/add", content);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddNewStationAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-user-token");
            var request = new AddStationRequest
            {
                BrandName = "Orlen",
                Street = "NewStreetTest",
                HouseNumber = "99",
                City = "NewCityTest",
                PostalCode = "11-111",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    new AddFuelTypeAndPriceRequest
                    {
                        Name = "Diesel",
                        Code = "ON",
                        Price = 6.5m,
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await unauthClient.PostAsync("/api/admin/station/add", content);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteStationAsyncTest_200OK()
        {
            // Arrange
            var deleteRequest = new FindStationRequest
            {
                BrandName = "Orlen",
                Street = "test",
                HouseNumber = "1",
                City = "test"
            };
            var content = new StringContent(JsonSerializer.Serialize(deleteRequest), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/station/delete")
            {
                Content = content
            };

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeleteStationAsyncTest_400()
        {
            // Arrange
            var deleteRequest = new FindStationRequest
            {
                BrandName = "",
                Street = "test",
                HouseNumber = "1",
                City = "test"
            };
            var content = new StringContent(JsonSerializer.Serialize(deleteRequest), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/station/delete")
            {
                Content = content
            };

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteStationAsyncTest_401()
        {
            // Arrange
            var unauthClient = _factory.CreateClient();
            var deleteRequest = new FindStationRequest
            {
                BrandName = "Orlen",
                Street = "test",
                HouseNumber = "1",
                City = "test"
            };
            var content = new StringContent(JsonSerializer.Serialize(deleteRequest), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/station/delete")
            {
                Content = content
            };

            // Act
            var response = await unauthClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteStationAsyncTest_403()
        {
            // Arrange
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-user-token");
            var deleteRequest = new FindStationRequest
            {
                BrandName = "Orlen",
                Street = "test",
                HouseNumber = "1",
                City = "test"
            };
            var content = new StringContent(JsonSerializer.Serialize(deleteRequest), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/station/delete")
            {
                Content = content
            };

            // Act
            var response = await unauthClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteStationAsyncTest_500()
        {
            // Arrange
            var deleteRequest = new FindStationRequest
            {
                BrandName = "BadBrand",
                Street = "test",
                HouseNumber = "1",
                City = "test"
            };
            var content = new StringContent(JsonSerializer.Serialize(deleteRequest), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/station/delete")
            {
                Content = content
            };

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task AssignStationToFuelTypeAsyncTest_200OK()
        {
            //Arrange
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var addRequest = new AddStationRequest
            {
                BrandName = "Orlen",
                Street = "TempAssignTestStreet" + uniqueSuffix,
                HouseNumber = "99" + uniqueSuffix,
                City = "TempCity" + uniqueSuffix,
                PostalCode = "12-345",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
        {
            new AddFuelTypeAndPriceRequest
            {
                Name = "Diesel",
                Code = "ON",
                Price = 6.5m,
            }
        }
            };
            var addContent = new StringContent(JsonSerializer.Serialize(addRequest), Encoding.UTF8, "application/json");
            var addResponse = await _client.PostAsync("/api/admin/station/add", addContent);
            Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
            var assignRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = addRequest.BrandName,
                    Street = addRequest.Street,
                    HouseNumber = addRequest.HouseNumber,
                    City = addRequest.City
                },
                Code = "X",
                Price = 4.44m
            };
            var assignContent = new StringContent(JsonSerializer.Serialize(assignRequest), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/admin/station/fuel-type/assign", assignContent);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AssignStationToFuelTypeAsyncTest_400()
        {
            //Arrange
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var addRequest = new AddStationRequest
            {
                BrandName = "Orlen",
                Street = "TempAssignTestStreet" + uniqueSuffix,
                HouseNumber = "99" + uniqueSuffix,
                City = "TempCity" + uniqueSuffix,
                PostalCode = "12-345",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
        {
            new AddFuelTypeAndPriceRequest
            {
                Name = "Diesel",
                Code = "ON",
                Price = 6.5m,
            }
        }
            };
            var addContent = new StringContent(JsonSerializer.Serialize(addRequest), Encoding.UTF8, "application/json");
            var addResponse = await _client.PostAsync("/api/admin/station/add", addContent);
            Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
            var assignRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = addRequest.BrandName,
                    Street = addRequest.Street,
                    HouseNumber = addRequest.HouseNumber,
                    City = addRequest.City
                },
                Code = "ON",
                Price = 4.44m
            };
            var assignContent = new StringContent(JsonSerializer.Serialize(assignRequest), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/admin/station/fuel-type/assign", assignContent);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [Fact]
        public async Task AssignStationToFuelTypeAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var addRequest = new AddStationRequest
            {
                BrandName = "Orlen",
                Street = "TempAssignTestStreet" + uniqueSuffix,
                HouseNumber = "99" + uniqueSuffix,
                City = "TempCity" + uniqueSuffix,
                PostalCode = "12-345",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
        {
            new AddFuelTypeAndPriceRequest
            {
                Name = "Diesel",
                Code = "ON",
                Price = 6.5m,
            }
        }
            };
            var addContent = new StringContent(JsonSerializer.Serialize(addRequest), Encoding.UTF8, "application/json");
            var addResponse = await _client.PostAsync("/api/admin/station/add", addContent);
            Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
            var assignRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = addRequest.BrandName,
                    Street = addRequest.Street,
                    HouseNumber = addRequest.HouseNumber,
                    City = addRequest.City
                },
                Code = "X",
                Price = 4.44m
            };
            var assignContent = new StringContent(JsonSerializer.Serialize(assignRequest), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PostAsync("/api/admin/station/fuel-type/assign", assignContent);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AssignStationToFuelTypeAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var addRequest = new AddStationRequest
            {
                BrandName = "Orlen",
                Street = "TempAssignTestStreet" + uniqueSuffix,
                HouseNumber = "99" + uniqueSuffix,
                City = "TempCity" + uniqueSuffix,
                PostalCode = "12-345",
                Latitude = 10.0,
                Longitude = 10.0,
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
        {
            new AddFuelTypeAndPriceRequest
            {
                Name = "Diesel",
                Code = "ON",
                Price = 6.5m,
            }
        }
            };
            var addContent = new StringContent(JsonSerializer.Serialize(addRequest), Encoding.UTF8, "application/json");
            var addResponse = await _client.PostAsync("/api/admin/station/add", addContent);
            Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
            var assignRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = addRequest.BrandName,
                    Street = addRequest.Street,
                    HouseNumber = addRequest.HouseNumber,
                    City = addRequest.City
                },
                Code = "X",
                Price = 4.44m
            };
            var assignContent = new StringContent(JsonSerializer.Serialize(assignRequest), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PostAsync("/api/admin/station/fuel-type/assign", assignContent);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetFuelPriceForStationAsyncTest_200OK()
        {
            //Arrange
            var url = "/api/admin/station/fuel-type?BrandName=Orlen&Street=test&HouseNumber=1&City=test";

            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(content));
            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.True(root.GetProperty("data").ValueKind == JsonValueKind.Array);
        }

        [Fact]
        public async Task GetFuelPriceForStationAsyncTest_400()
        {
            //Arrange
            var url = "/api/admin/station/fuel-type?BrandName=&Street=&HouseNumber=1&City=test";
            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetFuelPriceForStationAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var url = "/api/admin/station/fuel-type?BrandName=Orlen&Street=test&HouseNumber=1&City=test";
            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFuelPriceForStationAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var url = "/api/admin/station/fuel-type?BrandName=Orlen&Street=test&HouseNumber=1&City=test";
            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetFuelPriceForStationAsyncTest_404()
        {
            //Arrange
            var url = "/api/admin/station/fuel-type?BrandName=Shell&Street=test&HouseNumber=1&City=test";
            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeFuelPriceAsyncTest_200OK()
        {
            //Arrange
            var assignRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = "Orlen",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                Code = "ON",
                Price = 4.44m
            };
            var assignContent = new StringContent(JsonSerializer.Serialize(assignRequest), Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/admin/station/fuel-type/assign", assignContent);
            var changeRequest = new ManageStationFuelPriceRequest
            {
                Station = assignRequest.Station,
                Code = "ON",
                Price = 4.99m
            };
            var changeContent = new StringContent(JsonSerializer.Serialize(changeRequest), Encoding.UTF8, "application/json");

            //Act
            var changeResponse = await _client.PatchAsync("/api/admin/station/fuel-type/change-price", changeContent);
            var changeRespContent = await changeResponse.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);
            Assert.Contains("success", changeRespContent, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ChangeFuelPriceAsyncTest_400()
        {
            //Arrange
            var changeRequest = new ManageStationFuelPriceRequest
            {
                Station= new FindStationRequest
                {
                    BrandName = "Orlen",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                Code = "",
                Price = 3.0m
            };
            var changeContent = new StringContent(JsonSerializer.Serialize(changeRequest), Encoding.UTF8, "application/json");

            //Act
            var changeResponse = await _client.PatchAsync("/api/admin/station/fuel-type/change-price", changeContent);
            var changeRespContent = await changeResponse.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, changeResponse.StatusCode);
        }

        [Fact]
        public async Task ChangeFuelPriceAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var changeRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = "Orlen",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                Code = "ON",
                Price = 3.0m
            };
            var changeContent = new StringContent(JsonSerializer.Serialize(changeRequest), Encoding.UTF8, "application/json");

            //Act
            var changeResponse = await unauthClient.PatchAsync("/api/admin/station/fuel-type/change-price", changeContent);
            var changeRespContent = await changeResponse.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, changeResponse.StatusCode);
        }

        [Fact]
        public async Task ChangeFuelPriceAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var changeRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = "Orlen",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                Code = "ON",
                Price = 3.0m
            };
            var changeContent = new StringContent(JsonSerializer.Serialize(changeRequest), Encoding.UTF8, "application/json");

            //Act
            var changeResponse = await unauthClient.PatchAsync("/api/admin/station/fuel-type/change-price", changeContent);
            var changeRespContent = await changeResponse.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, changeResponse.StatusCode);
        }

        [Fact]
        public async Task ChangeFuelPriceAsyncTest_404()
        {
            //Arrange
            var changeRequest = new ManageStationFuelPriceRequest
            {
                Station = new FindStationRequest
                {
                    BrandName = "BadBrand",
                    Street = "test",
                    HouseNumber = "1",
                    City = "test"
                },
                Code = "ON",
                Price = 3.0m
            };
            var changeContent = new StringContent(JsonSerializer.Serialize(changeRequest), Encoding.UTF8, "application/json");

            //Act
            var changeResponse = await _client.PatchAsync("/api/admin/station/fuel-type/change-price", changeContent);
            var changeRespContent = await changeResponse.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, changeResponse.StatusCode);
        }
    }
}
