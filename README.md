# LottoTM

A modern lottery ticket management system that automates win verification and simplifies ticket management for LOTTO players.

## Table of Contents

- [Project Description](#project-description)
- [Tech Stack](#tech-stack)
- [Getting Started Locally](#getting-started-locally)
- [Getting Started with Docker](#getting-started-with-docker)
- [Available Scripts](#available-scripts)
- [Project Scope](#project-scope)
- [Project Status](#project-status)
- [License](#license)

## Project Description

LottoTM is a full-stack web application designed to solve a common problem for lottery players: manually checking multiple ticket sets against draw results is time-consuming, monotonous, and prone to human error. This MVP system automates the entire verification process, enabling users to check up to 100 ticket sets against official LOTTO draw results in less than 2 seconds.

### Key Features

- **Fast Verification**: Automated checking of 100 ticket sets in under 2 seconds
- **Error Prevention**: Eliminates human error through automated number matching
- **Organized Management**: Store and manage up to 100 ticket sets per user with data isolation
- **Smart Generators**: Random ticket generator and system generator (covering all numbers 1-49)
- **Flexible Checking**: Verify wins across custom date ranges with visual highlighting
- **Real-time Validation**: Input validation with instant feedback for all user entries

### Supported Games

Currently supports LOTTO and LOTTO PLUS (6 numbers from 1-49)

## Tech Stack

### Backend

- **.NET 8** (C#) - Latest .NET framework
- **ASP.NET Core Minimal APIs** - Lightweight API endpoints
- **Entity Framework Core** - ORM for database operations
- **SQL Server 2022** - Relational database
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Request validation
- **Serilog** - Structured logging
- **JWT** - JSON Web Token authentication
- **Google Gemini API** - AI-powered data extraction from XLotto website
- **xUnit** - Unit and integration testing
- **WebApplicationFactory** - Integration test framework
- **Moq** - Mocking framework for unit tests

### Frontend

- **React 19.1.1** - UI library with latest features
- **TypeScript 5.9.3** - Type-safe JavaScript
- **React Router 7.9.5** - Client-side routing
- **Tailwind CSS 4.1.16** - Utility-first CSS framework
- **Vite 7.1.7** - Next-generation build tool
- **ESLint 9.36.0** - Code quality and consistency

### Architecture

- **Vertical Slice Architecture** - Feature-based code organization
- **Context API** - State management for React frontend
- **Repository Pattern** - Data access abstraction
- **CQRS Pattern** - Command/Query separation with MediatR

## Getting Started Locally

### Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js** (v18 or higher recommended)
- **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **SQL Server 2022** (Express edition or higher)
- **Git**

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/mulatom1/LottoTM.git
cd LottoTM
```

#### 2. Backend Setup

```bash
# Navigate to backend directory
cd src/server/LottoTM.Server.Api

# Restore dependencies
dotnet restore

# Update database connection string in appsettings.json
# Configure your SQL Server connection details in:
# - appsettings.json (base configuration)
# - appsettings.Development.json (development overrides)

# Apply database migrations
dotnet ef database update

# Run the API server
dotnet run
```

The API will start at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`

#### 3. Frontend Setup

```bash
# Navigate to frontend directory
cd src/client/app01

# Install dependencies
npm install

# Start development server
npm run dev
```

The application will open at `http://localhost:5173`

## Getting Started with Docker

Docker provides a simplified way to run the entire application (frontend + backend) in a containerized environment without installing Node.js or .NET SDK locally.

### Prerequisites

- **Docker Desktop** ([Download](https://www.docker.com/products/docker-desktop))
- **SQL Server 2022** (running locally or accessible remotely)

### Running with Docker

#### 1. Build the Docker Image

From the project root directory:

```bash
docker build -t lottotm:latest .
```

This creates a multi-stage Docker image that:
- Builds the React frontend using Node.js 20
- Builds the .NET 8 backend
- Combines both into a single runtime image with ASP.NET Core serving the frontend

#### 2. Run the Container

```bash
docker run -d -p 8080:8080 --name lottotm-app lottotm:latest
```

The application will be available at `http://localhost:8080`

#### 3. Environment Configuration

For production deployments, you'll need to configure environment variables for database connection and JWT settings:

```bash
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=YOUR_SQL_SERVER;Database=LottoTM;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True" \
  -e Jwt__Key="YOUR_SECRET_KEY_MINIMUM_32_CHARACTERS" \
  -e Jwt__Issuer="LottoTM" \
  -e Jwt__Audience="LottoTM" \
  -e Jwt__ExpiryMinutes="60" \
  --name lottotm-app \
  lottotm:latest
```

#### 4. Useful Docker Commands

```bash
# View running containers
docker ps

# Stop the container
docker stop lottotm-app

# Start the container
docker start lottotm-app

# Remove the container
docker rm lottotm-app

# View container logs
docker logs lottotm-app

# Follow container logs in real-time
docker logs -f lottotm-app

# Execute commands inside the container
docker exec -it lottotm-app /bin/bash
```

### Docker Notes

- The Dockerfile uses multi-stage builds to optimize image size
- Frontend static files are served by the ASP.NET Core backend from `wwwroot`
- The container exposes port **8080** (default for .NET 8 in containers)
- Database migrations must be applied separately before running the container
- For local development, SQL Server connection should use `host.docker.internal` instead of `localhost`

### Configuration

#### Backend Configuration

Edit `appsettings.json` or `appsettings.Development.json` in `src/server/LottoTM.Server.Api/`:

**Required sections:**
- `ConnectionStrings:DefaultConnection` - SQL Server connection string
- `Jwt:Key` - JWT secret key (minimum 32 characters)
- `Jwt:Issuer` - Token issuer
- `Jwt:Audience` - Token audience
- `Jwt:ExpiryMinutes` - Token expiration time
- `GoogleGemini:ApiKey` - Google Gemini API key (base64 encoded)
- `GoogleGemini:Model` - Gemini model to use (default: "gemini-2.0-flash")
- `GoogleGemini:Enable` - **Feature Flag** to enable/disable XLotto functionality (default: false)
- `LottoWorker` - Background worker configuration (**Feature Flag**)
  - `Enable` - Enable/disable worker (default: false)
  - `StartTime` - Worker start time (default: "22:15:00")
  - `EndTime` - Worker end time (default: "23:00:00")
  - `IntervalMinutes` - Check interval in minutes (default: 5)
- `ApiUrl` - Application API URL for worker keep-alive pings
- `Serilog` - Logging configuration (console + file)
- `Swagger:Enabled` - **Feature Flag** to enable/disable Swagger UI (default: false)

#### Frontend Configuration

The frontend uses Vite environment modes with `.env` files in `src/client/app01/`:

- `.env` - Base configuration
- `.env.dev` - Development mode overrides (used by `npm run dev`)
- `.env.prod` - Production mode overrides (used by `npm run prod`)

**Required variables:**
- `VITE_API_URL` - Backend API endpoint
- `VITE_APP_TOKEN` - Application authentication token

## Available Scripts

### Frontend Scripts

Run these commands from `src/client/app01/`:

```bash
# Start development server with hot reload (dev mode)
npm run dev

# Start development server (production mode)
npm run prod

# Build for production
npm run build

# Run ESLint code linter
npm run lint

# Preview production build locally
npm run preview
```

### Backend Scripts

Run these commands from `src/server/LottoTM.Server.Api/` or project root:

```bash
# Run the API server
dotnet run --project src/server/LottoTM.Server.Api/LottoTM.Server.Api.csproj

# Build the entire solution
dotnet build LottoTM.sln

# Run all tests
dotnet test LottoTM.sln

# Run specific test project
dotnet test tests/server/LottoTM.Server.Api.Tests/LottoTM.Server.Api.Tests.csproj

# Create a new migration
dotnet ef migrations add <MigrationName> --project src/server/LottoTM.Server.Api

# Apply migrations to database
dotnet ef database update --project src/server/LottoTM.Server.Api
```

## Project Scope

### Included in MVP

**User Management**
- ✅ User registration with email and password
- ✅ JWT-based authentication (login/logout)
- ✅ Data isolation between users
- ✅ Support for up to 100 ticket sets per user

**Draw Management**
- ✅ Manual entry of official LOTTO and LOTTO PLUS results
- ✅ **Background Worker for Automatic Draw Fetching**
  - Automated background service (`LottoWorker`) that runs during configured time window (22:15-23:00)
  - **Feature Flag controlled** (`LottoWorker:Enable` in appsettings.json)
  - Disabled by default in production, can be enabled in development
  - Automatically fetches LOTTO and LOTTO PLUS results from XLotto.pl API
  - Runs every 5 minutes (configurable) during active window
  - Validates and saves results to database without user intervention
  - Prevents duplicates and handles errors gracefully
  - Keep-alive ping to maintain API uptime
  - Detailed logging for monitoring and troubleshooting
- ✅ Automated draw fetching from XLotto.pl via Google Gemini API (On-Demand)
  - **Feature Flag controlled** (`GoogleGemini:Enable` in appsettings.json)
  - Disabled by default in production, enabled in development
  - Frontend dynamically shows/hides "Pobierz z XLotto" button based on backend configuration
  - Manual trigger by admin users through UI
- ✅ AI-powered HTML content extraction and JSON conversion
- ✅ Support for multiple lottery types (LOTTO, LOTTO PLUS)
- ✅ Historical draw results storage
- ✅ Input validation with real-time feedback
- ✅ Date-based filtering and sorting

**Ticket Set Management**
- ✅ View all saved ticket sets
- ✅ Add tickets manually with validation
- ✅ Edit and delete existing tickets
- ✅ Random ticket generator
- ✅ System generator (creates 9 tickets covering all numbers 1-49)
- ✅ Duplicate prevention system

**Win Verification**
- ✅ Check tickets against draw results within custom date range
- ✅ Visual highlighting of winning numbers (bold formatting)
- ✅ Win labels displaying match count ("3 matches", "4 matches", etc.)
- ✅ High-performance verification engine (100 tickets in ≤2s)

### Future Enhancements (Out of Scope for MVP)

- ❌ Support for other lottery games (MINI LOTTO, EuroJackpot, etc.)
- ❌ Email verification during registration
- ❌ Automatic draw result fetching from external APIs
- ❌ Import/Export tickets (CSV, PDF)
- ❌ Email or push notifications for wins
- ❌ Social features (sharing tickets)
- ❌ Native mobile applications
- ❌ Prize amount calculation and information
- ❌ AI/ML analysis and number recommendations
- ❌ Payment system integration
- ❌ Group betting features

## Project Status

**Current Version**: MVP 1.3 (In Development)

**Live Demo**: The MVP is deployed and accessible at **https://tomsoft1.pl/lottotm**

The project is actively under development with a focus on delivering core MVP functionality. The system currently supports LOTTO and LOTTO PLUS games (6 numbers from 1-49) with fully automated win verification.

### Architecture Status

- ✅ Vertical Slice Architecture implemented
- ✅ MediatR CQRS pattern in use
- ✅ FluentValidation for all inputs
- ✅ Global exception handling middleware
- ✅ Structured logging with Serilog
- ✅ Comprehensive test coverage (Unit, Integration, Service tests)
- ✅ Integration tests with WebApplicationFactory
- ✅ XLotto integration with Google Gemini API (29 tests passing)

### Performance Metrics

- ✅ **Target Met**: Verify 100 ticket sets against 1 draw result in ≤ 2 seconds

### Recent Updates

- **LottoWorker - Background Service**: Implemented automatic lottery result fetching
  - Background service that runs from 22:15 to 23:00 (configurable)
  - **Feature Flag**: `LottoWorker:Enable` controls worker activation (default: false)
  - Automatically fetches LOTTO and LOTTO PLUS results every 5 minutes
  - Validates and saves results without user intervention
  - Includes keep-alive ping to maintain API availability
  - Comprehensive error handling and logging
  - Configuration via `appsettings.json` (LottoWorker section)
  - Dedicated documentation in `.ai/worker-plan.md`
- **Feature Flag for XLotto**: Added configurable `GoogleGemini:Enable` flag to control XLotto functionality visibility
  - New endpoint `GET /api/xlotto/is-enabled` for frontend to check feature availability
  - When disabled, API returns empty data and UI hides "Pobierz z XLotto" button
  - Default: disabled in production, enabled in development
- **Docker Support**: Added multi-stage Dockerfile for containerized deployments (frontend + backend in single image)
- Implemented GitHub Actions CI/CD pipeline
- Enhanced ticket and draw management with `groupName` and `lottoType` fields
- Refactored database context with updated Entity Framework Core fluent API

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

**Copyright © 2025 Tomasz Mularczyk**

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software.

---

**Author**: Tomasz Mularczyk
**GitHub**: [github.com/mulatom1/LottoTM](https://github.com/mulatom1/LottoTM)
**Live Demo**: [tomsoft1.pl/lottotm](https://tomsoft1.pl/lottotm)
**Last Updated**: November 11, 2025
