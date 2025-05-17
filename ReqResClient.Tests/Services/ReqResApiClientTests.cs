using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ReqResClient.Configuration;
using ReqResClient.Exceptions;
using ReqResClient.Models;
using ReqResClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReqResClient.Tests.Services
{
    public class ReqResApiClientTests
    {
        private readonly Mock<IOptions<ReqResApiOptions>> _optionsMock;
        private readonly Mock<ILogger<ReqResApiClient>> _loggerMock;
        private readonly ReqResApiOptions _options;
        public ReqResApiClientTests()
        {
            _options = new ReqResApiOptions()
            {
                BaseUrl = "https://reqres.in/api",
                TimeOutSeconds = 30
            };

            _optionsMock = new Mock<IOptions<ReqResApiOptions>>();  
            _optionsMock.Setup(o => o.Value).Returns(_options);

            _loggerMock = new Mock<ILogger<ReqResApiClient>>(); 
        }
        [Fact]
        public async Task GetAsync_SuccessfulResponse_ReturnsDeserializedObject()
        {
            //Arrange data
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Avatar = "https://example.com/avatar.jpg"
            };

            var response = new UserResponse
            {
                Data = user
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                 )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(response))
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(_options.BaseUrl)
            };

            var apiClient = new ReqResApiClient(httpClient, _optionsMock.Object, _loggerMock.Object);

            //Act result
            var result = await apiClient.GetAsync<UserResponse>("users/1");

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.Id);
            Assert.Equal("test@example.com", result.Data.Email);
            Assert.Equal("John", result.Data.FirstName);
            Assert.Equal("Doe", result.Data.LastName);
        }
        [Fact]
        public async Task GetAsync_NotFound_ThrowsNotFoundException()
        {
            //Arrange data
            var httpMessageHandleMock = new Mock<HttpMessageHandler>();
            httpMessageHandleMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                 )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            var httpClient = new HttpClient(httpMessageHandleMock.Object)
            {
                BaseAddress = new Uri(_options.BaseUrl)
            };

            var apiClient = new ReqResApiClient(httpClient, _optionsMock.Object, _loggerMock.Object);

            //Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => apiClient.GetAsync<UserResponse>("users/999"));
        }
        [Fact]
        public async Task GetAsync_ServerError_ThrowsApiException()
        {
            //Arrange
            var httpMessageHandleMock = new Mock<HttpMessageHandler> ();
            httpMessageHandleMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                 )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            var httpClient = new HttpClient(httpMessageHandleMock.Object)
            {
                BaseAddress = new Uri(_options.BaseUrl)
            };

            var apiClient = new ReqResApiClient(httpClient, _optionsMock.Object, _loggerMock.Object);

            //Act & Assert
            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                apiClient.GetAsync<UserResponse>("users/1"));

            Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        }
        [Fact]
        public async Task GetAsync_InvalidJson_ThrowsDeserializationException()
        {
            //Arrange
            var httpMessageHandlerMock = new Mock<HttpMessageHandler> ();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Invalid JSON")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(_options.BaseUrl)
            };

            var apiClient = new ReqResApiClient(httpClient, _optionsMock.Object, _loggerMock.Object);

            //Act and Assert
            await Assert.ThrowsAsync<DeserializationException>(() => apiClient.GetAsync<UserResponse>("users/1"));
        }
    }
}
