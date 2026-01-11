# Service Booking System

## Overview

This project is the final project for the university course:  
**[CSCB766 Programming with ASP .NET](https://ecatalog.nbu.bg/default.asp?V_Year=2021&YSem=6&Spec_ID=&Mod_ID=&PageShow=coursepresent&P_Menu=courses_part2&Fac_ID=3&M_PHD=0&P_ID=832&TabIndex=1&K_ID=13013&K_TypeID=10&l=1)** at [New Bulgarian University](https://www.nbu.bg/en).

Lecturer: Asst. Prof. Lachezar Tomov, PhD

## Project Introduction

The **Service Booking System** is a web application built with ASP.NET Core. It provides a platform where providers can offer services and customers can book available time slots. The system is designed with a clean, layered architecture and follows modern .NET development best practices.

## Table of Contents

- [Project Goal](#project-goal)
- [System Architecture & Design](#system-architecture--design)
- [User Roles](#user-roles)
- [Project Status & Key Features](#project-status--key-features)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
- [Testing](#testing)

## Project Goal

The project's main goal is to build a functional service booking platform by applying professional, modern ASP.NET Core development practices. The focus is on creating a maintainable application with a clean, layered architecture that separates concerns effectively.

## System Architecture & Design

The application is built using a layered architecture to ensure a clean separation of concerns.

-   **Backend**: **ASP.NET Core 9.0**
-   **Architecture**: Layered (`Core`, `Data`, `Application`, `Infrastructure`, `Web`)
-   **Database**: **Entity Framework Core** with **Microsoft SQL Server**
-   **Authentication**: **ASP.NET Core Identity** (Cookies for MVC, JWT for API)

### Use Case Diagram

The following diagram provides a high-level overview of the system's functionality and the roles of its different users.

![Use Case Diagram](./docs/diagrams/02-Use-Case-Diagram.png)

### Domain Model Diagram

The database schema is designed to support the core features of the application. It utilizes a flexible base entity hierarchy and a robust soft-delete pattern to ensure data integrity and history.

![Domain Model Diagram](./docs/diagrams/01-Domain-Model-Diagram.png)

*(The project's `/docs/diagrams` folder contains the detailed PlantUML source files for these diagrams.)*

## User Roles

-   **Administrator**: The System Owner/Manager. Has global control over the entire application.
-   **Provider**: The Service Seller/Offeror. Registers themselves and provides the services.
-   **Customer**: The Service Buyer/Booker. Registers themselves to book available services.

## Project Status & Key Features

The project has a solid architectural foundation, with the following key patterns and features implemented:

1.  **Layered Architecture**: Solution structured into five layers: `.Core`, `.Data`, `.Application`, `.Infrastructure`, and `.Web`.
2.  **Identity & Authentication**: 
    -   ASP.NET Core Identity configured with custom `ApplicationUser` and `ApplicationRole` entities.
    -   **Hybrid Auth**: Supports Cookies for MVC views and **JWT (JSON Web Tokens)** for API endpoints.
3.  **Data Persistence Patterns**:
    -   A complete domain model with a flexible base entity hierarchy (`BaseEntity`, `DeletableEntity`).
    -   A **Soft-Delete** pattern implemented using EF Core's Global Query Filters.
4.  **Core Business Services**:
    -   A fully-featured `UserService` for administrative user management (CRUD, role management, disabling users).
    -   A `ServiceService` for full CRUD management of services, including paging, sorting, and soft-delete support.
    -   A `CategoryService` for managing service categories.
    -   A `BookingService` for managing the entire booking lifecycle (Create, Read, Update, Cancel, Confirm, Decline, Complete).
    -   A `ReviewService` for managing customer reviews and ratings.
    -   An `AvailabilityService` that handles complex scheduling logic, including operating hours, split shifts, and booking overlaps.
5.  **API Layer**:
    -   A comprehensive REST API exposing all core functionalities (Auth, Users, Services, Bookings, Availability, Reviews).
    -   **Image Management**: Endpoints for uploading and managing service images.
    -   **Validation**: Robust input validation using Data Annotations and custom logic.
6.  **Infrastructure Layer & External Services**:
    -   A dedicated `.Infrastructure` project for decoupled external service implementations.
    -   **Email Notification Service**: Automated emails for booking events (Created, Confirmed, Cancelled) using `SendGrid` (production) or `NullEmailService` (development).
    -   **Image Storage**: Integration with **Cloudinary** for scalable image hosting and transformation.
    -   An `ITemplateService` that renders HTML email templates from embedded resources.
7.  **Database Seeding**: A decoupled, composite seeder pattern for essential data (`Roles`, `Administrator`).
8.  **Testing**:
    -   **Unit Tests**: High coverage using xUnit and Moq for business logic.
    -   **Integration Tests**: Tests using **Testcontainers (SQL Server)** and **Respawn** to verify the full stack against a real database.
9.  **Logging**: Configured Serilog for structured logging to both the console and rolling files.

## Technology Stack

-   **Backend:** .NET 9, ASP.NET Core
-   **Data Access:** Entity Framework Core 9
-   **Authentication:** ASP.NET Core Identity, JWT Bearer
-   **Testing:** xUnit, Moq, Testcontainers, Respawn
-   **Logging:** Serilog
-   **Email:** SendGrid
-   **Storage:** Cloudinary

## Getting Started

### Prerequisites

-   .NET 9 SDK
-   A code editor (e.g., JetBrains Rider, Visual Studio)
-   **Docker Desktop** (Required for Integration Tests)
-   (Optional) A SendGrid account and API key for testing real email sending.
-   (Optional) A Cloudinary account for image uploads.

### Configuration

1.  Clone the repository.
2.  The application uses `appsettings.Development.json` for local development configuration. Ensure the `ConnectionStrings` section is configured for your local database.
3.  **API Keys (Important!):** It is strongly recommended to store sensitive keys using the .NET Secret Manager. To set the secrets, navigate to the `ServiceBookingSystem.Web` project directory in your terminal and run:
    ```bash
    dotnet user-secrets init
    dotnet user-secrets set "Jwt:Key" "YOUR_SUPER_SECRET_KEY_MIN_32_CHARS"
    dotnet user-secrets set "EmailSettings:SendGridApiKey" "YOUR_SENDGRID_API_KEY"
    dotnet user-secrets set "Cloudinary:CloudName" "YOUR_CLOUD_NAME"
    dotnet user-secrets set "Cloudinary:ApiKey" "YOUR_CLOUDINARY_API_KEY"
    dotnet user-secrets set "Cloudinary:ApiSecret" "YOUR_CLOUDINARY_API_SECRET"
    ```

## Testing

The project employs the following testing strategy.

### Unit Tests
Located in `ServiceBookingSystem.UnitTests`.
-   **Focus:** Business logic in the Application layer.
-   **Tools:** xUnit, Moq, EF Core In-Memory (for simple repository mocking).
-   **Run:** `dotnet test ServiceBookingSystem.UnitTests`

### Integration Tests
Located in `ServiceBookingSystem.IntegrationTests`.
-   **Focus:** API Endpoints, Database Constraints, Middleware, and Full Request Lifecycle.
-   **Infrastructure:** Uses **Testcontainers** to spin up a real SQL Server Docker container for the test suite. Uses **Respawn** to wipe the database clean between every test method.
-   **Logging:** Application logs are piped to the xUnit output window using `MartinCostello.Logging.XUnit`.
-   **Requirement:** Docker must be running.
-   **Run:** `dotnet test ServiceBookingSystem.IntegrationTests`
