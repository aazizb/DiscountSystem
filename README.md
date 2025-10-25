# Discount Codes System

Solution Overview:

We created a solution named DiscountSystem consisting of six projects and one test project:



<img width="729" height="203" alt="image" src="https://github.com/user-attachments/assets/e5dac2cb-a548-4991-87ca-b93f10b8e068" />




## Run and test everything :

Make sure to clone the solution. Make sure you have access to MS Sql Server and update the connection string, if necessary. Alternatively, the simplest and most common setup for local development is to run the Database in Docker and keep  the server and client running on your host machine. Make sure you to update the connection string again.

#### "Server=sqlserver,1433;Database=DiscountDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"


To run and test the entire process, please follow these steps:
## Run the server (gRPC over HTTPS/HTTP3)
cd ../DiscountSystem.Server<br>
dotnet run

## In a new terminal, run the client
cd ../DiscountSystem.Client<br>
dotnet run

## Run tests
cd ../DiscountSystem.Tests
dotnet test

## Key Features Implemented
1. gRPC Communication: HTTP/3 compatible gRPC services
2. Data Persistence: SQL Server with EF Core, including unique constraints on discount codes
3. Concurrent Handling: Use async/await everywhere (repositories/services/gRPC),  which lets Kestrel handle many simultaneous connections, with thread-safe operations. Use optimistic concurrency token RowVersion ([Timestamp]) in the entity; EF Core will throw DbUpdateConcurrencyException on conflicting updates. Catch it and return meaningful gRPC status codes so client can handle re-try or notify user.
4. Unique Code Generation: The random code generation with uniqueness guarantee. The DB unique index on Code is the source of truth. We attempt to insert values and, upon a unique violation, regenerate the conflicting values. This pattern is robust in concurrent environments.<br>

        public static class DiscountCodeConstants
        {
            public static readonly int MaxNumCodes = 2000;
            public const int MinCodeLength = 7;
            public const int MaxCodeLength = 8;
            public static readonly int[] AllowedCodeLengths = { MinCodeLength, MaxCodeLength };
        }

5. Error Handling: Comprehensive error handling and result codes.
6. Race to UseCode: Two clients could try to use the same code at the same time. We prevent this by reloading the entity inside a transaction just before marking it as used; the DB transaction and SaveChanges ensure that only one attempt will succeed. For very high throughput, we considered issuing an

       await _context.DiscountCodes
                .Where(o => o.Code == code && !o.IsUsed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.IsUsed, true)
                    .SetProperty(c => c.UsedAt, DateTime.UtcNow),
                    cancellationToken);
7. Testing: xUnit tests with in-memory database for service layer.

## Generate Development Certificates
Before running Docker, generate the SSL certificates needed for HTTPS/HTTP3:

mkdir -p ~/.aspnet/https

dotnet dev-certs https -ep ~/.aspnet/https/aspnetapp.pfx -p YourCertPassword123! --trust
