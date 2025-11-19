# Service Booking System

## Overview

This project is the final project for the university course:  
**[CSCB766 Programming with ASP .NET](https://ecatalog.nbu.bg/default.asp?V_Year=2021&YSem=6&Spec_ID=&Mod_ID=&PageShow=coursepresent&P_Menu=courses_part2&Fac_ID=3&M_PHD=0&P_ID=832&TabIndex=1&K_ID=13013&K_TypeID=10&l=1)** at [New Bulgarian University](https://www.nbu.bg/en).

Lecturer: Asst. Prof. Lachezar Tomov, PhD

## Project Introduction

The **Service Booking System** is a web application built with ASP.NET Core. It provides a platform where providers can offer services (e.g., guitar lessons) and customers can book available time slots. The system is designed with a clean, layered architecture and follows modern .NET development best practices.

## Table of Contents

- [Project Goal](#project-goal)
- [Architecture & Technologies](#architecture--technologies)
- [User Roles](#user-roles)
- [Database Schema](#database-schema)
- [Setup & Progress](#setup--progress)

## Project Goal

The project's main goal is to create a robust and scalable platform for booking services. This includes managing provider and customer roles, defining services and their availability, and handling bookings. The application will feature a traditional multi-page web application (MVC) and a separate RESTful API for programmatic access.

## Architecture & Technologies

The application is built using a layered architecture to ensure a clean separation of concerns.

-   **Backend**: **ASP.NET Core 9.0**
-   **Architecture**: Layered (Web, Application, Data)
-   **Database**: **Entity Framework Core** with **Microsoft SQL Server**
-   **Logging**: **Serilog**
-   **Authentication**: **ASP.NET Core Identity**

## User Roles

-   **Administrator**: The System Owner/Manager. Has global control over the entire application.
-   **Provider**: The Service Seller/Offeror. Registers themselves and provides the services.
-   **Customer**: The Service Buyer/Booker. Registers themselves to book available services.

### Use Case Diagram

The following diagram provides a high-level overview of the system's functionality and the roles of its different users.

![Use Case Diagram](./docs/diagrams/02-Use-Case-Diagram.png)

### Domain Model Diagram

The database schema is designed to support the core features of the application. It utilizes a flexible base entity hierarchy and a robust soft-delete pattern to ensure data integrity and history.

![Domain Model Diagram](./docs/diagrams/01-Domain-Model-Schema.png)

*(The project's `/docs/diagrams` folder contains the detailed PlantUML source files for these diagrams.)*

## Setup & Progress

### Completed Steps:

1.  **Project Setup**: Solution created with three projects: `.Web` (MVC UI), `.Data` (Data Access Layer), and a placeholder for the `.Application` layer.
2.  **Logging**: Serilog is configured for structured logging to the console and rolling files.
3.  **Database Configuration**: Entity Framework Core is configured to connect to a Microsoft SQL Server database.
4.  **Identity Setup**:
    -   ASP.NET Core Identity is configured using custom `ApplicationUser` and `ApplicationRole` entities.
    -   The Identity database schema has been created via an initial EF Core migration.
5.  **Core Domain Entities**:
    -   Created core business entities: `Category`, `Service`, `OperatingHour`, `ServiceImage`, and `Review`.
    -   Established relationships between entities (e.g., a Service has a Category, a Provider, and multiple Reviews).
6.  **Data Persistence Patterns**:
    -   Implemented a flexible base entity hierarchy (`BaseEntity`, `AuditableEntity`, `DeletableEntity`) to support both hard and soft deletes.
    -   Implemented a robust **Soft-Delete** pattern using EF Core's Global Query Filters to ensure data is never permanently lost.
    -   Resolved database cascade-delete cycles to ensure schema integrity.
7.  **Documentation**:
    -   Created initial `README.md` to track project progress.
    -   Created high-level UML diagrams for the Domain Model and System Use Cases.

---
