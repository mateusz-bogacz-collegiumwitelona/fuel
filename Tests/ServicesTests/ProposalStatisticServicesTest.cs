using Data.Interfaces;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;   
using Xunit;

namespace Tests.ServicesTests
{
    public class ProposalStatisticServicesTest
    {
        private readonly IProposalStatisticRepository _proposalStatisticRepository;
        private readonly ILogger<ProposalStatisticServices> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CacheService _cache;
        public ProposalStatisticServicesTest(
            IProposalStatisticRepository proposalStatisticRepository,
            ILogger<ProposalStatisticServices> logger,
            UserManager<ApplicationUser> userManager,
            CacheService cache
            )
        {
            _proposalStatisticRepository = proposalStatisticRepository;
            _logger = logger;
            _userManager = userManager;
            _cache = cache;
        }
        [Fact]
        public async Task GetUserProposalStatisticResponse_SuccessIfResponseIsValid()
        {
            //Arrange
            var service = new ProposalStatisticServices(
                _proposalStatisticRepository,
                _logger,
                _userManager,
                _cache
                );
            var email = "";
            //Act
            var result = await service.GetUserProposalStatisticResponse(email);
            //Assert
            Assert.True(result.IsSuccess);
        }
        [Fact] 
        public async Task GetUserProposalStatisticResponse_FailIfEmailIsNullOrEmpty()
        {
            //Arrange
            var service = new ProposalStatisticServices(
                _proposalStatisticRepository,
                _logger,
                _userManager,
                _cache
                );
            string email = null;
            //Act
            var result = await service.GetUserProposalStatisticResponse(email);
            //Assert
            Assert.False(result.IsSuccess);
        }
    } 
}
