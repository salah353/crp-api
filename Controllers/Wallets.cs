using alpha.Models;
using alpha.Services;
using Microsoft.AspNetCore.Mvc;

namespace alpha.Controllers
{
    [Route("api/wallets")]
    [ApiController]
    public class Wallets : ControllerBase
    {
        private readonly IWalletService _walletService;

        public Wallets(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Get balance of a specific symbol on a chain.
        /// GET /api/wallets/balance?chain=Bsc&address=0x...&symbol=USDT&network=mainnet
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance(
            [FromQuery] string chain,
            [FromQuery] string address,
            [FromQuery] string symbol,
            [FromQuery] string network = "mainnet")
        {
            try
            {
                var response = await _walletService.GetBalanceAsync(chain, address, symbol);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all supported assets balance for an address on a chain.
        /// GET /api/wallets/assets?chain=Bsc&address=0x...&network=mainnet
        /// </summary>
        [HttpGet("assets")]
        public async Task<IActionResult> GetAssets(
            [FromQuery] string chain,
            [FromQuery] string address,
            [FromQuery] string network = "mainnet")
        {
            try
            {
                var response = await _walletService.GetAssetsAsync(chain, address);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
