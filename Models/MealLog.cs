namespace CaloriesTracker.Models
{
    public class MealLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public Users User   { get; set; }

        public int FoodId { get; set; }
        public Foods Food { get; set; }

        public double Grams { get; set; }
        public DateTime LogDate { get; set; }
        public enum MealType
        {
            Breakfast,
            Lunch,
            Dinner,
            Snack
        }
        public MealType Type { get; set; }
    }
}
