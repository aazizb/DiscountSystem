# Discount Codes System

Solution Overview
We'll create a solution named DiscountSystem consisting of six  projects and one test project:



<img width="729" height="203" alt="image" src="https://github.com/user-attachments/assets/e5dac2cb-a548-4991-87ca-b93f10b8e068" />




# Run and test everything :

Make sure to clone the solution. Make sure you have access to MS Sql Server and update the connection string, if necessary. Alternatively, the simplest and most common setup for local development is to run the Database in Docker and keep  the server and client running on your host machine. Make sure you to update the connection string again.

# "Server=sqlserver,1433;Database=DiscountDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"


To run and test the entire process, please follow these steps:
# Run the server (gRPC over HTTPS/HTTP3)
cd ../DiscountSystem.Server
dotnet run

# In a new terminal, run the client
cd ../DiscountSystem.Client
dotnet run

# Run tests
cd ../DiscountSystem.Tests
dotnet test
