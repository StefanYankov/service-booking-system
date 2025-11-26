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

## Project Goal

The project's main goal is to build a functional service booking platform by applying professional, modern ASP.NET Core development practices. The focus is on creating a maintainable application with a clean, layered architecture that separates concerns effectively.

## System Architecture & Design

The application is built using a layered architecture to ensure a clean separation of concerns.

-   **Backend**: **ASP.NET Core 9.0**
-   **Architecture**: Layered (`Core`, `Data`, `Application`, `Infrastructure`, `Web`)
-   **Database**: **Entity Framework Core** with **Microsoft SQL Server**
-   **Authentication**: **ASP.NET Core Identity**

### Use Case Diagram

The following diagram provides a high-level overview of the system's functionality and the roles of its different users.

!Use Case Diagram

### Domain Model Diagram

The database schema is designed to support the core features of the application. It utilizes a flexible base entity hierarchy and a robust soft-delete pattern to ensure data integrity and history.

!Domain Model Diagram

*(The project's `/docs/diagrams` folder contains the detailed PlantUML source files for these diagrams.)*

## User Roles

-   **Administrator**: The System Owner/Manager. Has global control over the entire application.
-   **Provider**: The Service Seller/Offeror. Registers themselves and provides the services.
-   **Customer**: The Service Buyer/Booker. Registers themselves to book available services.

## Project Status & Key Features

The project has a solid architectural foundation, with the following key patterns and features implemented:

1.  **Layered Architecture**: Solution structured into five layers: `.Core`, `.Data`, `.Application`, `.Infrastructure`, and `.Web`.
2.  **Identity & Authentication**: ASP.NET Core Identity is configured with custom `ApplicationUser` and `ApplicationRole` entities.
3.  **Data Persistence Patterns**:
    -   A complete domain model with a flexible base entity hierarchy (`BaseEntity`, `DeletableEntity`).
    -   A **Soft-Delete** pattern implemented using EF Core's Global Query Filters.
4.  **Core Business Services**:
    -   A fully-featured `UserService` for administrative user management (CRUD, role management, disabling users).
    -   A `CategoryService` for managing service categories.
5.  **Infrastructure Layer & External Services**:
    -   A dedicated `.Infrastructure` project for decoupled external service implementations.
    -   An **Email Notification Service** with `SendGrid` (production) and `NullEmailService` (development) implementations.
    -   An `ITemplateService` that renders HTML email templates from embedded resources.
6.  **Database Seeding**: A decoupled, composite seeder pattern for essential data (`Roles`, `Administrator`).
7.  **Comprehensive Unit Testing**:
    -   A dedicated **Unit Test** project using xUnit and Moq.
    -   High test coverage for the `UserService`, including happy paths, non-happy paths, and edge cases.
    -   Tests are organized into **partial classes** (e.g., `UsersServiceTests.Get.cs`) for better maintainability.
8.  **Logging**: Configured Serilog for structured logging to both the console and rolling files.

## Technology Stack

-   **Backend:** .NET 9, ASP.NET Core
-   **Data Access:** Entity Framework Core 9
-   **Authentication:** ASP.NET Core Identity
-   **Testing:** xUnit, Moq
-   **Logging:** Serilog
-   **Email:** SendGrid

## Getting Started

### Prerequisites

-   .NET 9 SDK
-   A code editor (e.g., JetBrains Rider, Visual Studio)
-   (Optional) A SendGrid account and API key for testing real email sending.

### Configuration

1.  Clone the repository.
2.  The application uses `appsettings.Development.json` for local development configuration. Ensure the `ConnectionStrings` section is configured for your local database.
3.  **API Keys (Important!):** It is strongly recommended to store the `SendGridApiKey` using the .NET Secret Manager. To set the secret, navigate to the `ServiceBookingSystem.Web` project directory in your terminal and run:
    