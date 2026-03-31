namespace CaloriesTracker.DTOs
{
    public class UserRegisterDto
    {
        public string email { get; set; }   
        public string password { get; set; }    
    }

    public class UserLoginDto
    {
        public string email { get; set; }
        public string password { get; set; }
    }

}
