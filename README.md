# 🧪 Lab Assistant OOP – Backend API (.NET 8)

**Lab Assistant OOP (LAO)** is a backend system designed to support Object-Oriented Programming lab activities for students and instructors. It is built using **ASP.NET Core Web API 8.0** and follows the classic **3-Layer Architecture** (Presentation – Business Logic – Data Access) to ensure clean separation of concerns, scalability, and maintainability.

---

## 🏗️ Project Structure

```plaintext
LabAssistantOPP_LAO.WebApi          // Presentation Layer
 └── Controllers/
 └── Program.cs

LabAssistantOPP_LAO.Services             // Business Logic Layer
 └── Interfaces/
 └── Services/

LabAssistantOPP_LAO.DTO           // Data Access Layer
 └── DTOs/

LabAssistantOPP_LAO.Models           // Data Access Layer
 └── Models/

---

## 🛠️ Core Technologies Used

| Component      | Technology              |
| -------------- | ----------------------- |
| Framework      | ASP.NET Core API 8.0    |
| ORM            | Entity Framework Core 8 |
| Validation     | FluentValidation        |
| Object Mapping | AutoMapper              |
| Logging        | Serilog                 |
| API Docs       | Swagger (Swashbuckle)   |
| Testing        | xUnit + Moq             |

---

## 📦 Required NuGet Packages

```bash
# Web API & Authentication
Microsoft.AspNetCore.Authentication.JwtBearer
Swashbuckle.AspNetCore

# Entity Framework Core
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools

# Business Logic & Helpers
FluentValidation
AutoMapper
AutoMapper.Extensions.Microsoft.DependencyInjection

# Logging
Serilog
Serilog.AspNetCore

# Testing
xUnit
Moq
FluentAssertions
```

---

## 🌟 Key Features

* ✅ Clear separation of layers: Web API, BLL, DAL
* ✅ DTO pattern for safe and clean data transfer
* ✅ Centralized Dependency Injection configuration
* ✅ Supports EF Core Migrations for database updates
* ✅ Swagger UI with JWT bearer authentication
* ✅ Clean, testable Service and Repository pattern structure

---

## ▶️ Quick Start

1. **Clone the repository**:

   ```bash
   git clone https://github.com/your-org/api_laopp.git
   ```


2. **Run the API**:

   ```bash
   dotnet run --project api_laopp.WebApi
   ```

3. **Access Swagger UI**:

   ```
   https://localhost:{port}/swagger
   ```

---

## 🧩 Contributing

Feel free to submit Pull Requests! Make sure to follow the 3-layer architecture, include relevant unit tests, and maintain consistent naming and formatting across the solution.
