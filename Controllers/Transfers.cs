using alpha.Models;
using alpha.Services;
using Microsoft.AspNetCore.Mvc;

namespace alpha.Controllers
{
    [Route("api/transfer")]
    [ApiController]
    public class Transfers : ControllerBase
    {
        private readonly ITransferService _transferService;

        public Transfers(ITransferService transferService)
        {
            _transferService = transferService;
        }

        [HttpPost("prepare")]
        public async Task<IActionResult> Prepare([FromBody] PrepareTransferRequest request)
        {
            try
            {
                var response = await _transferService.PrepareAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastTransferRequest request)
        {
            try
            {
                var response = await _transferService.BroadcastAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("status")]
        public async Task<IActionResult> GetStatus([FromBody] GetStatusRequest request)
        {
            try
            {
                var response = await _transferService.GetStatusAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("gas-quote")]
        public async Task<IActionResult> GasQuote([FromBody] GasQuoteRequest request)
        {
            try
            {
                var response = await _transferService.GetGasQuoteAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
