using Castle.Core.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ReqResClient.Configuration;
using ReqResClient.Exceptions;
using ReqResClient.Interfaces;
using ReqResClient.Models;
using ReqResClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqResClient.Tests.Services
{
    public class ExternalUserServiceTests
    {
        private readonly Mock<IReqResApiClient> _apiClientMock;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<IOptions<ReqResApiOptions>> _optionsMock;
        private readonly Mock<ILogger<ExternalUserService>> _loggerMock;
        private readonly ReqResApiOptions _options;
        public ExternalUserServiceTests()
        {
            _apiClientMock = new Mock<IReqResApiClient>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _options = new ReqResApiOptions
            {
                EnableCaching = true,
                CacheTimeoutMinutes = 5,
            };

            _optionsMock = new Mock<IOptions<ReqResApiOptions>>();
            _optionsMock.Setup(o => o.Value).Returns(_options);

            _loggerMock = new Mock<ILogger<ExternalUserService>>(); 
        }
        [Fact]
        public async Task GetUserByIdAsync_UserExists_ReturnsUser()
        {
            //Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe"
            };

            var response = new UserResponse
            {
                Data = user
            };

            _apiClientMock
                .Setup(c => c.GetAsync<UserResponse>("users/1",It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var service = new ExternalUserService(
                _apiClientMock.Object,
                _memoryCache,
                _optionsMock.Object,
                _loggerMock.Object
                );

            //Act
            var result = await service.GetUserByIdAsync( 1 );

            //Asert
            Assert.NotNull( result );
            Assert.Equal(1, result.Id );
            Assert.Equal("test@example.com",result.Email);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
        }
        [Fact]
        public async Task GetUserByIdAsync_UserDoesNotExists_ThrowsNotFoundException()
        {
            //Arrange
            var response = new UserResponse
            {
                Data = null
            };

            _apiClientMock
                .Setup(c => c.GetAsync<UserResponse>("users/999", It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var service = new ExternalUserService(
                _apiClientMock.Object,
                _memoryCache,
                _optionsMock.Object,
                _loggerMock.Object
                );

            //Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetUserByIdAsync(999));
        }
        [Fact]
        public async Task GetUserByIdAsync_CachedUser_ReturnsCachedUser()
        {
            //Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe"
            };

            _memoryCache.Set("user_1", user, TimeSpan.FromMinutes(5));

            var services = new ExternalUserService(
                _apiClientMock.Object,
                _memoryCache,
                _optionsMock.Object,
                _loggerMock.Object
             );

            //Act
            var result = await services.GetUserByIdAsync(1);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);

            //Verify that the API client was not called
            _apiClientMock.Verify(
                 c=> c.GetAsync<UserResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        }
        [Fact]
        public async Task GetAllUsersAsync_ReturnAllUsers()
        {
            //Arrange
            var page1Response = new PaginatedResponse<User>
            {
                Page = 1,
                PerPage = 3,
                Total = 6,
                TotalPages = 2,
                Data = new List<User>
                {
                    new User { Id = 1, Email = "user1@example.com", FirstName = "User", LastName = "One" },
                    new User { Id = 2, Email = "user2@example.com", FirstName = "User", LastName = "Two" },
                    new User { Id = 3, Email = "user3@example.com", FirstName = "User", LastName = "Three" }
                }
            };

            var page2Response = new PaginatedResponse<User>
            {
                Page = 2,
                PerPage = 3,
                Total = 6,
                TotalPages = 2,
                Data = new List<User>
                {
                    new User { Id = 4, Email = "user4@example.com", FirstName = "User", LastName = "Four" },
                    new User { Id = 5, Email = "user5@example.com", FirstName = "User", LastName = "Five" },
                    new User { Id = 6, Email = "user6@example.com", FirstName = "User", LastName = "Six" }
                }
            };

            _apiClientMock
                .Setup(c => c.GetAsync<PaginatedResponse<User>>("users?page=1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(page1Response);

            _apiClientMock
                .Setup(c => c.GetAsync<PaginatedResponse<User>>("users?page=2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(page2Response);

            var service = new ExternalUserService(
                _apiClientMock.Object,
                _memoryCache,
                _optionsMock.Object,
                _loggerMock.Object
            );

            // Act
            var result = await service.GetAllUsersAsync();

            // Assert
            var users = result.ToList();
            Assert.Equal(6, users.Count);
            Assert.Equal(1, users[0].Id);
            Assert.Equal(6, users[5].Id);
        }
        [Fact]
        public async Task GetAllUsersAsync_CachedUsers_ReturnsCachedUsers()
        {
            // Arrange
            var cachedUsers = new List<User>
            {
                new User { Id = 1, Email = "user1@example.com", FirstName = "User", LastName = "One" },
                new User { Id = 2, Email = "user2@example.com", FirstName = "User", LastName = "Two" }
            };

            _memoryCache.Set("all_users", cachedUsers, TimeSpan.FromMinutes(5));

            var service = new ExternalUserService(
                _apiClientMock.Object,
                _memoryCache,
                _optionsMock.Object,
                _loggerMock.Object
            );

            // Act
            var result = await service.GetAllUsersAsync();

            // Assert
            var users = result.ToList();
            Assert.Equal(2, users.Count);

            // Verify that the API client was not called
            _apiClientMock.Verify(
                c => c.GetAsync<PaginatedResponse<User>>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }
    }
}
