using CaloriesTracker.Models;

namespace CaloriesTracker.DTOs
{
    public class AddMealDto
    {
        public string FoodName { get; set; } = string.Empty;
        public double Grams { get; set; }
        public MealLog.MealType MealType { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now;
    }
}
