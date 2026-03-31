namespace CaloriesTracker.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string email { get; set; } = string.Empty;
        public string passwordHash { get; set; } = string.Empty;
        public int DailyCaloriesGoal { get; set; } = 2000;

    }
}
