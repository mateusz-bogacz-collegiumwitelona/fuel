using Data.Context;
using Data.Interfaces;
using Data.Models;
using Data.Reopsitories;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Services.Helpers;
using Services.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class BrandServicesTest
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<BrandServices> _logger;
        private readonly CacheService _cache;

        public BrandServicesTest(
            IBrandRepository brandRepository,
            ILogger<BrandServices> logger,
            CacheService cache
            )
        {
            _brandRepository = brandRepository;
            _logger = logger;
            _cache = cache;
        }
    }
}

