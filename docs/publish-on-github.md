# Publishing the demonstration to GitHub

The code archive is a **starter project**, not a verified build. Verify it locally before publishing as a finished portfolio piece.

## 1. Prerequisites

- Windows PowerShell and Git installed
- .NET 10 SDK installed (`dotnet --version`)
- Personal GitHub account

## 2. Unzip the archive

Extract the zip into a directory named `trading-order-management-api` under your personal development workspace.

```powershell
cd C:\Users\<your-username>\source\repos\trading-order-management-api

dotnet restore .\tests\TradingOrders.Tests\TradingOrders.Tests.csproj
dotnet build .\src\TradingOrders.Api\TradingOrders.Api.csproj -c Release
dotnet test .\tests\TradingOrders.Tests\TradingOrders.Tests.csproj -c Release
```

Do not publish until those commands pass and you understand the implementation.

## 3. Create an empty GitHub repository

Create a **Public** repository named `trading-order-management-api`. Do not add a README on GitHub because the archive already includes one. Choose a software license separately if you want to allow reuse.

```powershell
git init
git branch -M main
git add .
git commit -m "Initial portfolio project: simulated trading orders API"
git remote add origin https://github.com/YOUR_USERNAME/trading-order-management-api.git
git push -u origin main
```

Substitute your own GitHub username. GitHub authentication may open a browser or request you to authenticate via Git Credential Manager.

## 4. Verify GitHub Actions

Open the repository's **Actions** tab and confirm the workflow succeeds before using this project in a job proposal. If it fails, fix the issue and push the correction. A red workflow is not a portfolio achievement.

## 5. Make an authentic pull request

Use a branch (`feature/...`) to implement a small change; open a real PR; describe the change, testing performed, trade-offs, and review findings in English. Avoid synthetic PRs that misrepresent collaboration.

## 6. Portfolio metadata

**Title:** Simulated Trading Orders API — C#/.NET, Clean Architecture & Automated Testing

**Summary:** Independently developed sample demonstrating an idempotent simulated-order API, domain validation, SQLite persistence, unit and integration tests, and CI/CD discipline. No real-money trading or brokerage connections.

**Skills:** C#, .NET, ASP.NET Core, REST APIs, Entity Framework Core, xUnit, Clean Architecture, Software Testing.
