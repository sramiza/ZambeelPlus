# ZambeelPlus - University Management System

A robust, containerized University Management System built with **.NET 8**, **SQL Server**, and **Kubernetes**. Designed to handle complex academic workflows including student enrollment, residency management, and finance tracking.

## Architecture: Hybrid CQRS

This project implements a **Command Query Responsibility Segregation (CQRS)** inspired pattern to optimize for both data integrity and projection flexibility:

* **Writes (Commands):** Critical state changes (e.g., `EnrollStudent`, `RegisterStudent`) are handled via **Stored Procedures** within strict SQL Transactions to ensure ACID compliance and prevent race conditions (e.g., preventing over-enrollment).
* **Reads (Queries):** Complex data projections (e.g., Student Dashboards, Rosters) are handled via **LINQ** and Entity Framework Core, allowing for type-safe, dynamic object mapping of hierarchical data.

## Tech Stack

* **Backend:** ASP.NET Core (.NET 8)
* **Database:** Microsoft SQL Server 2022 (Partitioned Tables, Stored Procedures, CTEs)
* **Orchestration:** Kubernetes (Docker Desktop / Kind)
* **Deployment:** Multi-container setup with Persistent Volume Claims (PVC) mapped to local host storage.

## Key Features

* **Partitioning Strategy:** Database tables partitioned by Student ID batch and Department ID for performance scaling.
* **Resiliency:** Kubernetes `StatefulSet` deployment for SQL Server with persistent storage configurations for Windows/Mac environments.
* **Data Integrity:** Custom SQL triggers for automatic finance record generation and GPA calculations.

## Contributors

* Syed Ramiz Abbas
