using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Contlollers.Controllers.Client
{
    [Route("api/proposal-statistic")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class ProposalStatisticController : ControllerBase
    {
        private readonly IProposalStatisticServices _proposalStatistic;

        public ProposalStatisticController(IProposalStatisticServices proposalStatistic)
        {
            _proposalStatistic = proposalStatistic;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserProposalStatisticResponse(string email)
        {
            var result = await _proposalStatistic.GetUserProposalStatisticResponse(email);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

    }
}
