# Project Setup Guide

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server (LocalDB or full instance)

## Backend Setup

1.  Navigate to `src/backend`:
    ```bash
    cd src/backend
    ```
2.  Restore dependencies:
    ```bash
    dotnet restore
    ```
3.  Update database (apply migrations):
    ```bash
    dotnet ef database update
    ```
4.  Run the application:
    ```bash
    dotnet run
    ```
    The backend will start on `http://localhost:5000` (http) and `https://localhost:5001` (https).

## Frontend Setup

1.  Navigate to `src/frontend`:
    ```bash
    cd src/frontend
    ```
2.  Install dependencies:
    ```bash
    npm install
    ```
3.  Run the development server:
    ```bash
    npm run dev
    ```
    The frontend will start on `http://localhost:5173` (or similar).

## Testing

### Backend Tests
```bash
dotnet test tests/backend/ReactCore.Backend.Tests/ReactCore.Backend.Tests.csproj
```

### Frontend Tests
```bash
cd src/frontend
npm test
```
