==============================================================================
PROJECT: Zambeel+ (High Availability SQL Cluster)
GROUP:   47
==============================================================================

[1] REQUIRED SOFTWARE
------------------------------------------------------------------------------
To execute this project, the host machine requires:
1. Docker Desktop (Windows) with Kubernetes Enabled.
   - Required Image: mcr.microsoft.com/mssql/server:2022-latest
2. .NET 8.0 SDK (or compatible .NET Core runtime).
3. Browser (Chrome/Edge) for the application interface.

[2] SUBMISSION CONTENTS
------------------------------------------------------------------------------
/k8s
  |-- setup_commands.bat        (Double-click to deploy the entire cluster)
  |-- data_loading_commands.txt (Commands to populate Schema + 840k records)
  |-- mssql-deployment.yaml     (HA Deployment with NodePort 31433)
  |-- mssql-storage.yaml        (Persistent Volume Config)
  |-- mssql-secret.yaml         (SA Password Configuration)

/SQL
  |-- Phase2.sql                (Combined Schema creation and Data insertion)

/ZambeelApp
  |-- (Source Code)             (C# ASP.NET Core Application)

[3] EXECUTION INSTRUCTIONS (VIVA DEMO)
------------------------------------------------------------------------------

STEP 1: CONFIGURE STORAGE
   Open 'k8s/mssql-storage.yaml' and ensure the 'hostPath' matches the 
   local machine's folder structure (e.g., C:\zambeel_data).

STEP 2: DEPLOY CLUSTER
   Run 'k8s/setup_commands.bat'.
   Wait for the pod status to change to 'Running'.
   Copy the Pod Name (e.g., mssql-deployment-xyz).

STEP 3: LOAD DATA
   Open 'k8s/data_loading_commands.txt'.
   Replace [POD_NAME] with the copied name.
   Run the commands to:
     a) Copy 'Phase2.sql' into the container.
     b) Execute the SQL script to create tables and insert data.

STEP 4: RUN APPLICATION
   Navigate to the 'ZambeelApp' folder.
   Run command: 'dotnet watch'
   The application will launch at http://localhost:5xxx connecting to Port 1433.

[4] EXTRA CREDIT: HIGH AVAILABILITY TEST
------------------------------------------------------------------------------
To verify disaster recovery:
1. Delete the running pod: `kubectl delete pod [POD_NAME]`
2. Watch Kubernetes automatically provision a replacement.
3. Once the new pod is running, the application data will persist without loss.