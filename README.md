# ZambeelPlus - University Management System

A robust, containerized University Management System built with **.NET 8**, **SQL Server**, and **Kubernetes**. Designed to handle complex academic workflows including student enrollment, residency management, and finance tracking.

## Architecture: Hybrid CQRS

This project implements a **Command Query Responsibility Segregation (CQRS)** inspired pattern to optimize for both data integrity and projection flexibility:

* **Writes (Commands):** Critical state changes (e.g., `EnrollStudent`, `RegisterStudent`) are handled via **Stored Procedures** within strict SQL Transactions to ensure ACID compliance and prevent race conditions.
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

---

## Local Deployment Guide

Follow these steps to deploy the entire cluster on a local machine using Docker Desktop.

### [1] REQUIRED SOFTWARE
To execute this project, the host machine requires:
1. **Docker Desktop** (Windows) with Kubernetes Enabled.
   * Required Image: `mcr.microsoft.com/mssql/server:2022-latest`
2. **.NET 8.0 SDK** (or compatible .NET Core runtime).
3. **Browser** (Chrome/Edge/Safari) for the application interface.

### [2] REPO CONTENTS
* `k8s/setup_commands.bat`: Double-click to deploy the entire cluster.
* `k8s/data_loading_commands.txt`: Commands to populate Schema + 840k records.
* `k8s/mssql-deployment.yaml`: HA Deployment with NodePort 1433.
* `k8s/mssql-storage.yaml`: Persistent Volume Config.
* `k8s/mssql-secret.yaml`: SA Password Configuration. (redacted from repo)
* `SQL/Phase2.sql`: Combined Schema creation and Data insertion.
* `ZambeelApp/`: C# ASP.NET Core Application Source Code.

### [3] EXECUTION INSTRUCTIONS

**STEP 1: CONFIGURE STORAGE**
Open `k8s/mssql-storage.yaml` and ensure the `hostPath` matches the local machine's folder structure (e.g., `C:\zambeel_data`).

**STEP 2: DEPLOY CLUSTER**
1. Run `k8s/setup_commands.bat`.
2. Wait for the pod status to change to 'Running'.
3. Copy the Pod Name (e.g., `mssql-deployment-xyz`) using `kubectl get pods`.

**STEP 3: LOAD DATA**
1. Open `k8s/data_loading_commands.txt`.
2. Replace `[POD_NAME]` with the copied name.
3. Run the commands to:
   * Copy `Phase2.sql` into the container.
   * Execute the SQL script to create tables and insert data.

**STEP 4: RUN APPLICATION**
1. Navigate to the `ZambeelApp` folder.
2. Run command: `dotnet watch`
3. The application will launch at `http://localhost:5xxx` connecting to Port 1433.

### [4] HIGH AVAILABILITY TEST
To verify disaster recovery:
1. Delete the running pod: `kubectl delete pod [POD_NAME]`
2. Watch Kubernetes automatically provision a replacement.
3. Once the new pod is running, the application data will persist without loss.

## Contributors

* Syed Ramiz Abbas
