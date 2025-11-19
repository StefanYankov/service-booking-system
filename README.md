# Service Booking System

## Overview

This project is the final project for the university course:  
**[CSCB766 Programming with ASP .NET](https://ecatalog.nbu.bg/default.asp?V_Year=2021&YSem=6&Spec_ID=&Mod_ID=&PageShow=coursepresent&P_Menu=courses_part2&Fac_ID=3&M_PHD=0&P_ID=832&TabIndex=1&K_ID=13013&K_TypeID=10&l=1)** at [New Bulgarian University](https://www.nbu.bg/en).

Lecturer: Asst. Prof. Lachezar Tomov, PhD

## Project Introduction

The **Service Booking System** is a web application built with ASP.NET Core. It provides a platform where providers can offer services (e.g., guitar lessons) and customers can book available time slots. The system is designed with a clean, layered architecture and follows .NET development best practices.

## Table of Contents

- [Project Goal](#project-goal)
- [System Architecture & Design](#system-architecture--design)
- [User Roles](#user-roles)
- [Project Status & Key Features](#project-status--key-features)

## Project Goal

The project's main goal is to build a functional service booking platform by applying professional, modern ASP.NET Core development practices. The focus is on creating a maintainable application with a clean, layered architecture that separates concerns effectively.

## System Architecture & Design

The application is built using a layered architecture to ensure a clean separation of concerns.

-   **Backend**: **ASP.NET Core 9.0**
-   **Architecture**: Layered (`Core`, `Data`, `Application`, `Web`)
-   **Database**: **Entity Framework Core** with **Microsoft SQL Server**
-   **Logging**: **Serilog**
-   **Authentication**: **ASP.NET Core Identity**
-   **Testing**: **xUnit**, **Moq**, and **FluentAssertions**

### Use Case Diagram

The following diagram provides a high-level overview of the system's functionality and the roles of its different users.

![Use Case Diagram](./docs/diagrams/02-Use-Case-Diagram.png)

### Domain Model Diagram

The database schema is designed to support the core features of the application. It utilizes a flexible base entity hierarchy and a robust soft-delete pattern to ensure data integrity and history.

![Domain Model Diagram](./docs/diagrams/01-Domain-Model-Schema.png)

*(The project's `/docs/diagrams` folder contains the detailed PlantUML source files for these diagrams.)*

## User Roles

-   **Administrator**: The System Owner/Manager. Has global control over the entire application.
-   **Provider**: The Service Seller/Offeror. Registers themselves and provides the services.
-   **Customer**: The Service Buyer/Booker. Registers themselves to book available services.

## Project Status & Key Features

The project is currently in the foundational stage, with the following key architectural patterns and features implemented:

1.  **Layered Architecture**: Solution structured into four layers: `.Core` (shared contracts), `.Data` (data access), `.Application` (business logic), and `.Web` (presentation).
2.  **Logging Setup**: Configured Serilog for centralized logging to both the console and rolling files, establishing a foundation for structured logging.
3.  **Identity & Authentication**: ASP.NET Core Identity is configured with custom `ApplicationUser` and `ApplicationRole` entities.
4.  **Database & Entities**:
    -   A complete domain model has been defined using Entity Framework Core.
    -   Relationships and database constraints (e.g., cascade-delete behavior) have been configured.
5.  **Data Persistence Patterns**:
    -   Implemented a flexible base entity hierarchy (`BaseEntity`, `AuditableEntity`, `DeletableEntity`).
    -   Implemented a **Soft-Delete** pattern using EF Core's Global Query Filters.
6.  **Database Seeding**:
    -   Implemented a decoupled, composite seeder pattern using an `ISeeder` contract.
    -   Created seeders for essential data (`Roles`, `Administrator` user).
7.  **Automated Testing Strategy**:
    -   Set up dedicated **Unit Test** and **Integration Test** projects using xUnit.
    -   Implemented integration tests for the database seeding process against an in-memory database.
    -   Implemented isolated unit tests for individual seeder classes using mocks.
8.  **Documentation**:
    -   Created a professional `README.md` to track project architecture and progress.
    -   Created high-level UML diagrams for the Domain Model and System Use Cases.

---
