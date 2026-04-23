# ⚡ Calories & Workout Tracker 🍎🏋️‍♂️

A modern Fullstack application for tracking nutrition and workouts. Built with **.NET 8** and **React**, featuring a sleek **Glassmorphism** design and smooth animations.

This project demonstrates the use of relational databases, integration with third-party APIs (FatSecret), and secure authentication using JWTs via HTTP-only Cookies.

## ✨ Features
* **🔐 Full Authentication:** Secure Registration and Login with password hashing (BCrypt).
* **🍎 Calorie Tracker:** Search products, add meals, and automatic Macro (P/F/C) calculation.
* **💪 Workout Module:** Add exercises, sets, and weights with date-based history.
* **🎨 Modern UI:** Animated backgrounds, Glassmorphism effects, and custom Toast notifications.
* **🐳 Dockerized:** Entire stack (Frontend + Backend + DB) runs with a single command.

## 🚀 Tech Stack
* **Backend:** C#, ASP.NET Core (Web API)
* **Database:** SQLite
* **ORM:** Entity Framework Core
* **Authentication:** JWT (JSON Web Tokens) + HTTP-only Cookies
* **External Integrations:** [FatSecret API](https://platform.fatsecret.com/api/)

## 📸 Screenshots

### 🔐 Login & Registration
<div align="center">
  <img src="https://github.com/user-attachments/assets/357cf0a5-e296-4c76-8d12-b9d09a6bff23" width="450" alt="Login Screen" />
  <img src="https://github.com/user-attachments/assets/e27127a5-5b99-409a-8332-caaabefddc0e" width="415" alt="Registration Screen" />
</div>

### 🏠 Home Page
<div align="center">
  <img width="1848" height="915" alt="image" src="https://github.com/user-attachments/assets/7674c399-5dad-4ade-85df-b5db3b76d9ac" />
</div>

### 🍎 Dashboard & Tracking
<div align="center">
  <p><b>Main Dashboard View</b></p>
  <img src="https://github.com/user-attachments/assets/6d7a4a1e-6b3f-4c13-8531-3360063ad1e1" width="900" alt="Dashboard Main" />
  
  <p><b>Workout Tracking & History</b></p>
  <img src="https://github.com/user-attachments/assets/67d89653-a155-4268-9f3d-921ec5b853f5" width="400" alt="Workout Mobile View" />
  <img src="https://github.com/user-attachments/assets/330a9ab8-b68d-49c8-bcca-c2b120475dc1" width="900" alt="Dashboard Statistics" />
</div>

## 🛠️ How to Run Locally

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Niveron228/CaloriesTracker.git

2. **Configure "appsettings.json":**
```json
{
  "Jwt": {
    "Key": "your_super_secret_and_long_key_here",
    "Issuer": "CaloriesTrackerServer",
    "Audience": "CaloriesTrackerUsers"
  },
  "FatSecret": {
    "ClientId": "your_fatsecret_client_id",
    "ClientSecret": "your_fatsecret_client_secret"
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
