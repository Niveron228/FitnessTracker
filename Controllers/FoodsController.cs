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

        [Authorize(Roles = "Admin")]
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
            var searchTerm = name.ToLower();

            var exactMatch = await _context.Foods.AsNoTracking().FirstOrDefaultAsync(f => f.name.ToLower() == searchTerm);

            if(exactMatch == null)
            {
                var externalList = await _foodApiService.SearchFoodAsync(name);

                if (externalList != null && externalList.Any())
                {
                    await SaveExternalListToDb(externalList);
                }
            }

            var allMatches = await _context.Foods
                .AsNoTracking()
                .Where(i => i.name.ToLower()
                .Contains(searchTerm))
                .ToListAsync();

            return allMatches.Any() ? Ok(allMatches) : NotFound("Not found in local and external databases");
        }

        [HttpGet("daily-stats/{date}")]
        public async Task<IActionResult> GetDailyStats(DateTime date)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim.Value);

            var logs = await _context.mealLogs
                .Include(m => m.Food)
                .Where(m => m.UserId == currentUserId
                && m.LogDate >= date.Date
                && m.LogDate < date.Date.AddDays(1))
                .ToListAsync();

            var report = Enum.GetValues(typeof(MealLog.MealType))
                .Cast<MealLog.MealType>()
                .Select(type =>
                {
                    var mealItems = logs.Where(l => l.Type == type).ToList();

                    return new
                    {
                        MealName = type.ToString(),
                        TotalCalories = Math.Round(mealItems.Sum(i => (i.Food.calories * i.Grams) / 100), 1),
                        TotalProtein = Math.Round(mealItems.Sum(i => (i.Food.protein * i.Grams) / 100), 1),
                        TotalFat = Math.Round(mealItems.Sum(i => (i.Food.fats * i.Grams) / 100), 1),
                        TotalCarbs = Math.Round(mealItems.Sum(i => (i.Food.carbs * i.Grams) / 100), 1),
                        ItemsCount = mealItems.Count,

                        Items = mealItems.Select(item => new
                        {
                            Id = item.Id,
                            Name = item.Food.name,
                            Grams = item.Grams,
                            Calories = Math.Round((item.Food.calories * item.Grams) / 100, 1),
                            Protein = Math.Round((item.Food.protein * item.Grams) / 100, 1),
                            Fat = Math.Round((item.Food.fats * item.Grams) / 100, 1),
                            Carbs = Math.Round((item.Food.carbs * item.Grams) / 100, 1)
                        }).ToList()
                    };
                });

            var dailySummary = new
            {
                Date = date.ToString("yyyy-MM-dd"),
                Meals = report,
                DayTotal = new
                {
                    Calories = Math.Round(logs.Sum(i => (i.Food.calories * i.Grams) / 100), 1),
                    Protein = Math.Round(logs.Sum(i => (i.Food.protein * i.Grams) / 100), 1),
                    Fat = Math.Round(logs.Sum(i => (i.Food.fats * i.Grams) / 100), 1),
                    Carbs = Math.Round(logs.Sum(i => (i.Food.carbs * i.Grams) / 100), 1)
                }
            };

                return Ok(dailySummary);
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [HttpPost("add-meal")]
        public async Task<IActionResult> AddMealToLog([FromBody] AddMealDto request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if(userIdClaim == null)
            {
                return Unauthorized("Unauthorized!");
            }
            int currentUserId = int.Parse(userIdClaim.Value);

            var searchTerm = request.FoodName.ToLower();

            var food = await _context.Foods.FirstOrDefaultAsync(f => f.name.ToLower() == searchTerm);

            if (food == null)
            {
                var externalList = await _foodApiService.SearchFoodAsync(request.FoodName);
                if (externalList == null || externalList.Count == 0)
                {
                    return NotFound();
                }

                await SaveExternalListToDb(externalList);

                food = await _context.Foods.FirstOrDefaultAsync(f => f.name.ToLower() == searchTerm);

                if (food == null)
                {
                    food = await _context.Foods.FirstOrDefaultAsync(f => f.name.ToLower().Contains(searchTerm));
                }
            }

            var newMealLog = new MealLog
            {
                UserId = currentUserId,
                FoodId = food.Id,
                Grams = request.Grams,
                Type = request.MealType,
                LogDate = request.LogDate
            };

            _context.mealLogs.Add(newMealLog);
            await _context.SaveChangesAsync();

            double actualCalories = (food.calories * request.Grams) / 100;
            double actualProtein = (food.protein * request.Grams) / 100;
            double actualFat = (food.fats * request.Grams) / 100;
            double actualCarbs = (food.carbs * request.Grams) / 100;

            return Ok(new
            {
                Message = "Meal added!",
                Date = request.LogDate.ToString("yyyy-MM-dd"),
                Meal = request.MealType.ToString(),
                FoodLogged = food.name,
                Amount = $"{request.Grams}g",
                CalculatedMacros = new
                {
                    Calories = Math.Round(actualCalories, 1),
                    Protein = Math.Round(actualProtein, 1),
                    Fat = Math.Round(actualFat, 1),
                    Carbs = Math.Round(actualCarbs, 1)
                }
            });
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [HttpDelete("delete-meal/{logId}")]
        public async Task<IActionResult> DeleteMealLog(int logId)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim.Value);

            var mealLog = await _context.mealLogs
                .FirstOrDefaultAsync(m => m.Id == logId && m.UserId == currentUserId);

            if (mealLog == null)
            {
                return NotFound(new { Message = "Not found!" });
            }

            _context.mealLogs.Remove(mealLog);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product removed!" });
        }

        // custom method to get and save list of objects from external api 
        private async Task SaveExternalListToDb(List<ExternalFoodDto> externalList)
        {
            foreach (var externalItem in externalList)
            {
                await ImportExternalFood(externalItem);
            }
        }

    }
}
