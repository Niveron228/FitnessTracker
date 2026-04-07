using CaloriesTracker.DB;
using CaloriesTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaloriesTracker.Services.FoodsServices
{
    public class FoodService : IFoodService
    {
        private readonly AppDbContext _context;

        public FoodService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Foods>> GetAllItemsAsync()
        {
            return await _context.Foods.AsNoTracking().ToListAsync();
        }

        public async Task<Foods?> GetItemByIdAsync(int id)
        {
            return await _context.Foods.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Foods>> GetItemByNameAsync(string name)
        {
            return await _context.Foods.AsNoTracking()
                .Where(f => f.name.ToLower().Contains(name.ToLower()))
                .ToListAsync();
        }

        public async Task<Foods> AddNewItemAsync(Foods newItem)
        {
            _context.Foods.Add(newItem);
            await _context.SaveChangesAsync();
            return newItem; 
        }

        public async Task<Foods?> UpdateItemAsync(int id, Foods updatedItem)
        {
            var existingFood = await _context.Foods.FirstOrDefaultAsync(i => i.Id == id);

            if (existingFood == null) return null; 
            existingFood.name = updatedItem.name;
            existingFood.calories = updatedItem.calories;
            existingFood.protein = updatedItem.protein;
            existingFood.fats = updatedItem.fats;
            existingFood.carbs = updatedItem.carbs;

            await _context.SaveChangesAsync();
            return existingFood;
        }

        public async Task<Foods?> PatchItemAsync(int id, JsonPatchDocument<Foods> patchDoc)
        {
            var existingFood = await _context.Foods.FirstOrDefaultAsync(i => i.Id == id);
            if (existingFood == null) return null;

            patchDoc.ApplyTo(existingFood);
            await _context.SaveChangesAsync();

            return existingFood;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var food = await _context.Foods.FirstOrDefaultAsync(i => i.Id == id);
            if (food == null) return false;

            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
