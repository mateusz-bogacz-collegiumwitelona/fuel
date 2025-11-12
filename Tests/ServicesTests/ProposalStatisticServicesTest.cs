using Data.Context;
using Data.Interfaces;
using Data.Models;
using Data.Reopsitories;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class ProposalStatisticServicesTest
    {
        private readonly Mock<IProposalStatisticRepository> _repository;
        private readonly Mock<ILogger<ProposalStatisticServices>> _loggerMock;
        private readonly ProposalStatisticServices _service;
        private readonly ITestOutputHelper _output;

        public ProposalStatisticServicesTest(ITestOutputHelper output)
        {
            // test output setup
            _output = output;

            //repo, logger, service
            _repository = new Mock<IProposalStatisticRepository>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<ProposalStatisticServices>>();
            //_service = new ProposalStatisticServices(_repository.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetUserProposalStatisticResponseTest_NullEmail_SuccessWhenReturnsBad()
        {
            // Arrange
            //

            // Act
            //we try to get a response for an empty email
            var result = await _service.GetUserProposalStatisticResponse(string.Empty);

            // Assert
            // GetUserProposalStatisticResponse() should return Bad response (!IsSuccess, code 400)
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            Assert.Contains("Email is required", result.Errors);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Email is required to fetch user proposal statistics.")),
                It.Is<Exception>(ex => ex == null),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
            _output.WriteLine("Test passed: GetUserProposalStatisticResponse() doesn't let a nonexistent email address pass and returns 400");
        }

        //[Fact]
        //public async Task GetUserProposalStatisticResponseTest_ExistingUserNoData_SuccessWhenReturnsBad()
        //{
        //    //Arrange
        //    //set repo up to return null response for this email
        //    _repository.Setup(r => r.GetUserProposalStatisticAsync("user123@example.com"))
        //    .ReturnsAsync((GetProposalStatisticResponse?)null);

        //    //Act
        //    var result = await _service.GetUserProposalStatisticResponse("user123@example.com");

        //    // Assert
        //    Assert.False(result.IsSuccess);
        //    Assert.Equal(404, result.StatusCode);
        //    Assert.Contains("User proposal statistic not found", result.Errors);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No proposal statistics found for user with email")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: GetUserProposalStatisticResponse() finds the user, but not the stats and retunrs 404");
        //}

        //[Fact]
        //public async Task GetUserProposalStatisticResponseTest_EverythingGood_SuccessWhenReturnsData()
        //{
        //    // Arrange\
        //    //set repo up to respond with a statistic for test email
        //    var response = new GetProposalStatisticResponse { TotalProposals = 1 }; 
        //    _repository.Setup(r => r.GetUserProposalStatisticAsync("user123@example.com"))
        //             .ReturnsAsync(response);

        //    // Act
        //    var result = await _service.GetUserProposalStatisticResponse("user123@example.com");

        //    // Assert
        //    Assert.True(result.IsSuccess);
        //    Assert.Equal(200, result.StatusCode);
        //    Assert.Equal(response, result.Data);
        //    _loggerMock.Verify(x => x.Log(
        //        It.Is<LogLevel>(lvl => lvl == LogLevel.Warning || lvl == LogLevel.Error),
        //        It.IsAny<EventId>(),
        //        It.IsAny<It.IsAnyType>(),
        //        It.IsAny<Exception>(),
        //        It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        //    _output.WriteLine("Test passed: GetUserProposalStatisticResponse() gets the data correctly and returns 200");
        //}
        
        //[Fact]
        //public async Task GetUserProposalStatisticResponseTest_SomethingWentWrongEx_SuccessWhenExceptionThrown()
        //{
        //    // Arrange
        //    // set repo up to throw an exception for our email
        //    _repository.Setup(r => r.GetUserProposalStatisticAsync("user123@example.com")).
        //        ThrowsAsync(new Exception("Test error"));

        //    // Act
        //    var result = await _service.GetUserProposalStatisticResponse("user123@example.com");

        //    // Assert
        //    Assert.False(result.IsSuccess);
        //    Assert.Equal(500, result.StatusCode);
        //    Assert.Contains("Test error", result.Errors);
        //    _loggerMock.Verify(
        //        x => x.Log(
        //            LogLevel.Error,
        //            It.IsAny<EventId>(),
        //            It.Is<It.IsAnyType>((v, _) =>
        //                v.ToString()!.Contains("An error occurred while fetching proposal statistics for email:")),
        //            It.Is<Exception>(ex => ex.Message == "Test error"),
        //            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        //        Times.Once);
        //    _output.WriteLine("Test passed: GetUserProposalStatisticResponse() catches exception and throws 500");
        //}

        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_WrongEmail_SuccessWhenReturnsBad()
        //{
        //    // Arrange
        //    //

        //    // Act
        //    //we try to get a response for an empty email
        //    var result = await _service.UpdateTotalProposalsAsync(false, string.Empty);

        //    // Assert
        //    // GetUserProposalStatisticResponse() should return Bad response (!IsSuccess, code 400)
        //    Assert.False(result.IsSuccess);
        //    Assert.Equal(400, result.StatusCode);
        //    Assert.Contains("Email is required", result.Errors);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Email is required to update user proposal statistics.")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() doesn't let a nonexistent email address pass and returns 400");
        //}

        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_FailedToUpdate_SuccessWhenReturnsBad()
        //{
        //    // Arrange
        //    // set repo up to respond with failure
        //    _repository.Setup(r => r.UpdateTotalProposalsAsync(true, "user123@example.com"))
        //     .ReturnsAsync(false);

        //    // Act
        //    var result = await _service.UpdateTotalProposalsAsync(true, "user123@example.com");

        //    // Assert
        //    Assert.False(result.IsSuccess);
        //    Assert.Equal(500, result.StatusCode);
        //    Assert.Contains("Failed to update user proposal statistic", result.Errors);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to update proposal statistics for user with email")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() returns 404");
        //}
        
        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_EverythingGood_SuccessWhenUpdatesCorrectly()
        //{
        //    // Arrange
        //    _repository.Setup(r => r.UpdateTotalProposalsAsync(true, "user123@example.com"))
        //     .ReturnsAsync(true);

        //    // Act
        //    var result = await _service.UpdateTotalProposalsAsync(true, "user123@example.com");

        //    // Assert
        //    Assert.True(result.IsSuccess);
        //    Assert.Equal(200, result.StatusCode);
        //    Assert.True(result.Data);
        //    _loggerMock.Verify(x => x.Log(
        //        It.Is<LogLevel>(lvl => lvl == LogLevel.Warning || lvl == LogLevel.Error),
        //        It.IsAny<EventId>(),
        //        It.IsAny<It.IsAnyType>(),
        //        It.IsAny<Exception>(),
        //        It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() returned 200 and threw no errors");
        //}

        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_SomethingWentWrongEx_SuccessWhenExceptionThrown()
        //{
        //    // Arrange
        //    _repository.Setup(r => r.UpdateTotalProposalsAsync(true, "user123@example.com")).
        //             ThrowsAsync(new Exception("Test error"));

        //    // Act
        //    var result = await _service.UpdateTotalProposalsAsync(true, "user123@example.com");

        //    // Assert
        //    Assert.False(result.IsSuccess);
        //    Assert.Equal(500, result.StatusCode);
        //    Assert.Contains("Test error", result.Errors);
        //    _loggerMock.Verify(
        //        x => x.Log(
        //            LogLevel.Error,
        //            It.IsAny<EventId>(),
        //            It.Is<It.IsAnyType>((v, _) =>
        //                v.ToString()!.Contains("An error occurred while updating proposal statistics for email:")),
        //            It.Is<Exception>(ex => ex.Message == "Test error"),
        //            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        //        Times.Once);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() catches exception and throws 500");
        //}
    }
}
