# 🍏 CaloriesTracker API

A backend for a fitness application designed to track daily food intake, automatically calculate macronutrients (macros), and maintain a detailed meal diary. 

This project demonstrates the use of relational databases, integration with third-party APIs (FatSecret), and secure authentication using JWTs via HTTP-only Cookies.

## 🚀 Tech Stack
* **Backend:** C#, ASP.NET Core (Web API)
* **Database:** SQLite
* **ORM:** Entity Framework Core
* **Authentication:** JWT (JSON Web Tokens) + HTTP-only Cookies
* **External Integrations:** [FatSecret API](https://platform.fatsecret.com/api/)

## ✨ Key Features
1. **🔐 Security & Authentication:**
   * User registration and login.
   * Storing tokens safely in `HttpOnly` Cookies (protecting against XSS).
   * Role-Based Access Control (RBAC): separation of regular users and administrators.

2. **🍎 Smart Food Database (API Caching):**
   * Search for foods by name.
   * If a product is not found in the local database, the backend automatically fetches it from the FatSecret API, caches it in the local SQLite database, and returns the result to the user.

3. **📊 Meal Log (Diary):**
   * Add food items to the diary categorized by: `Breakfast`, `Lunch`, `Dinner`, `Snack`.
   * **Dynamic Macro Calculation:** the user inputs the weight in grams, and the server automatically recalculates calories, protein, fat, and carbs based on the base (100g) nutritional value.

4. **📈 Daily Statistics:**
   * Generate a detailed daily report: total macronutrient intake broken down by each meal, along with a total daily summary.

## 🛠️ How to Run Locally

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/your-username/CaloriesTracker.git](https://github.com/your-username/CaloriesTracker.git)

2. **Configure "appsettings.json":**
```json
{
  &quot;Jwt&quot;: {
    &quot;Key&quot;: &quot;your_super_secret_and_long_key_here&quot;,
    &quot;Issuer&quot;: &quot;CaloriesTrackerServer&quot;,
    &quot;Audience&quot;: &quot;CaloriesTrackerUsers&quot;
  },
  &quot;FatSecret&quot;: {
    &quot;ClientId&quot;: &quot;your_fatsecret_client_id&quot;,
    &quot;ClientSecret&quot;: &quot;your_fatsecret_client_secret&quot;
  }
}
```
4. **Apply Migrations (Create Database)**
     `` dotnet ef database update``
5. **Run the project:**
   ``dotnet run``

   * Once running, the Swagger UI will be available at http://localhost:5230/swagger.

## 🗺️ Roadmap

* [ ] Implement full CRUD for the Meal Log (ability to delete and update the weight of existing records).

* [ ] Add a "Goals" system: personalized daily calorie and macro limits for each user.

* [ ] Integrate Redis for high-performance caching of frequent search queries.

* [ ] Expand the frontend client functionality.
