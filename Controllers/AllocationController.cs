using Microsoft.AspNetCore.Mvc;
using WisVestAPI.Models.DTOs;
using WisVestAPI.Services.Interfaces;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace WisVestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AllocationController : ControllerBase
    {
        private readonly IAllocationService _allocationService;

        public AllocationController(IAllocationService allocationService)
        {
            _allocationService = allocationService;
        }

        // POST: api/Allocation/compute
        [HttpPost("compute")]
        public async Task<ActionResult<AllocationResultDTO>> GetAllocation([FromBody] UserInputDTO input)
        {
            if (input == null)
            {
                return BadRequest("User input cannot be null.");
            }

            var fullAllocationResult = await _allocationService.CalculateFinalAllocation(input);

            // Validate allocation
            if (fullAllocationResult == null || !fullAllocationResult.ContainsKey("assets"))
            {
                return BadRequest("Allocation could not be computed or formatted correctly.");
            }

            var assetsData = fullAllocationResult["assets"] as Dictionary<string, object>;
            if (assetsData == null)
            {
                return StatusCode(500, "Error: Final allocation data format is incorrect.");
            }

            var result = new AllocationResultDTO { Assets = new Dictionary<string, AssetAllocation>() };

            foreach (var assetPair in assetsData)
            {
                var assetName = assetPair.Key;
                if (assetPair.Value is Dictionary<string, object> assetDetails)
                {
                    var assetAllocation = ParseAssetDetails(assetDetails);
                    if (assetAllocation != null)
                    {
                        result.Assets[assetName] = assetAllocation;
                    }
                }
            }

            return Ok(result);
        }

        private AssetAllocation? ParseAssetDetails(Dictionary<string, object> assetDetails)
        {
            if (assetDetails.TryGetValue("percentage", out var percentageObj) &&
                assetDetails.TryGetValue("subAssets", out var subAssetsObj) &&
                percentageObj is double percentage &&
                subAssetsObj is Dictionary<string, double> subAssets)
            {
                return new AssetAllocation
                {
                    Percentage = percentage,
                    SubAssets = subAssets
                };
            }
            return null;
        }
    }
}
