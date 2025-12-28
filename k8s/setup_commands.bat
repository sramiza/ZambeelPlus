@echo off
echo ========================================================
echo   ZAMBEEL+ HIGH AVAILABILITY CLUSTER SETUP
echo ========================================================

echo [Step 1] Creating SQL Secret...
kubectl apply -f mssql-secret.yaml

echo [Step 2] configuring Persistent Storage...
kubectl apply -f mssql-storage.yaml

echo [Step 3] Deploying SQL Server 2022 HA Cluster...
kubectl apply -f mssql-deployment.yaml

echo.
echo ========================================================
echo   CLUSTER STARTING...
echo   Please wait for the pod to show status 'Running'.
echo ========================================================
kubectl get pods -w