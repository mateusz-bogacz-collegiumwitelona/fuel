using Data.Context;
using Data.Models;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class RefreshTokenRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly RefreshTokenRepository _repository;
        private readonly ITestOutputHelper _output;
        public RefreshTokenRepositoryTest(ITestOutputHelper output)
        {
            _output = output;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _repository = new RefreshTokenRepository(_context);
        }
        [Fact]
        public async Task GetByTokenAsyncTest_SuccessWhenUserTokenReturned()
        {
            //Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
            };
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "testToken",
                User = user
            };
            _context.Users.Add(user);
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.GetByTokenAsync("testToken");

            //Assert
            Assert.NotNull(result);
            Assert.Equal(user, result.User);
            Assert.Equal("testToken", result.Token);
            _output.WriteLine("Success, GetByTokenAsync returns the correct user token.");
        }

        [Fact]
        public async Task GetByUserIdAsyncTest_SuccessWhenUserTokenListReturned()
        {
            //Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid()
            };
            var token1 = new RefreshToken {
                Id = Guid.NewGuid(), UserId = user.Id
            };
            var token2 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id
            };
            _context.RefreshTokens.AddRange(token1, token2);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.GetByUserIdAsync(user.Id);

            //Assert
            var tokenList = result.ToList();
            Assert.NotNull(result);
            Assert.Equal(2, tokenList.Count());
            Assert.Contains(tokenList, t => t.Id == token1.Id);
            Assert.Contains(tokenList, t => t.Id == token2.Id);
            _output.WriteLine("Success, GetByUserIdAsync returns the list of tokens for the userId");
        }

        [Fact]
        public async Task AddAsyncTest_SuccessIfTokenAdded()
        {
            //Arrange
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "test"
            };

            //Act
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();

            //Assert
            Assert.Equal(1, _context.RefreshTokens.Count());
            Assert.Equal(token.Id, _context.RefreshTokens.ToList().First().Id);
            _output.WriteLine("Succes, AddAsync correctly adds a token.");
        }

        [Fact]
        public async Task UpdateTokenTest_SuccessIfTokenUpdated()
        {
            //Arrange
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "testOld"
            };
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
            token.Token = "testNew";

            //Act
            await _repository.UpdateAsync(token);
            await _repository.SaveChangesAsync();

            //Assert
            Assert.Equal("testNew", _context.RefreshTokens.ToList().First().Token);
            _output.WriteLine("Success, UpdateToken succesfuly updates the token.");
        }

        [Fact]
        public async Task DeleteTokenTest_SuccessIfTokenDeleted()
        {
            //Arrange
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "test"
            };
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();

            //Act
            await _repository.DeleteAsync(token.Id);
            await _repository.SaveChangesAsync();

            //Assert
            Assert.Empty(_context.RefreshTokens);
            _output.WriteLine("Success, DeleteToken deletes the token");
        }
    }
}
