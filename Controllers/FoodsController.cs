using CaloriesTracker.DB;
using CaloriesTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;
using CaloriesTracker.Services;
using System.Text.RegularExpressions;
using System.Globalization;
using CaloriesTracker.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace CaloriesTracker.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FoodsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IFoodApiService _foodApiService;

        public FoodsController(AppDbContext context, IFoodApiService foodApiService)
        {
            _context = context;
            _foodApiService = foodApiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            return Ok(await _context.Foods.AsNoTracking().ToArrayAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            var food = await _context.Foods.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (food == null)
            {
                return NotFound();
            }
            return Ok(food);
        }

        [HttpGet("search/{name}")]
        public async Task<IActionResult> GetItemByName(string name)
        {
            var food = await _context.Foods.AsNoTracking().FirstOrDefaultAsync(i => i.name == name);
            if (food == null)
            {
                return NotFound();
            }
            return Ok(food);
        }

        [HttpGet("external/search/{name}")]
        public async Task<IActionResult> SearchExternalFood(string name)
        {
            var result = await _foodApiService.SearchFoodAsync(name);

            if (result == null || result.Count == 0)
            {
                return NotFound("Not found in external database");
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddNewItem([FromBody] Foods newItem)
        {
            await _context.Foods.AddAsync(newItem);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetItemById), new { id = newItem.Id }, newItem);
        }
        private double ParseNutritionValue(string description,string key)
        {
            var match = Regex.Match(description, $@"{key}:\s*([\d\.]+)", RegexOptions.IgnoreCase);

            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }
            return 0;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportExternalFood([FromBody] ExternalFoodDto dto)
        {
            var existingFood = await _context.Foods.FirstOrDefaultAsync(f => f.name.ToLower() == dto.Name.ToLower());

            if (existingFood != null)
            {
                return Conflict(new { message = "This product is already exist in out database" });
            }

            double cal = ParseNutritionValue(dto.Description, "Calories");
            double p = ParseNutritionValue(dto.Description, "Protein");
            double f = ParseNutritionValue(dto.Description, "Fat");
            double c = ParseNutritionValue(dto.Description, "Carbs");

            var newLocalFood = new Foods
            {
                name = dto.Name,
                calories = Convert.ToInt32(cal),
                protein = Convert.ToInt32(p),
                fats = Convert.ToInt32(f),
                carbs = Convert.ToInt32(c)

            };

            await _context.Foods.AddAsync(newLocalFood);
            await _context.SaveChangesAsync();

            return Ok(newLocalFood);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] Foods updatedItem)
        {
            var food = await _context.Foods.FirstOrDefaultAsync(i => i.Id == id);
            if (food == null)
            {
                return NotFound();
            }
            food.name = updatedItem.name;
            food.calories = updatedItem.calories;
            food.protein = updatedItem.protein;
            food.fats = updatedItem.fats;
            food.carbs = updatedItem.carbs;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchItem(int id,[FromBody]JsonPatchDocument<Foods> patchedItem)
        {
            var food = await _context.Foods.FirstOrDefaultAsync(i => i.Id == id);
            if(food == null)
            {
                return NotFound();
            }

            patchedItem.ApplyTo(food, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var food = await _context.Foods.FirstOrDefaultAsync(i => i.Id == id);
            if (food == null)
            {
                return NotFound();
            }
            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
