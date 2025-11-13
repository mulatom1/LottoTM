# LottoTM

A modern lottery ticket management system that automates win verification and simplifies ticket management for LOTTO players.

## Table of Contents

- [Project Description](#project-description)
- [Tech Stack](#tech-stack)
- [Getting Started Locally](#getting-started-locally)
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
- **xUnit** - Unit and integration testing
- **WebApplicationFactory** - Integration test framework

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

### Configuration

#### Backend Configuration

Edit `appsettings.json` or `appsettings.Development.json` in `src/server/LottoTM.Server.Api/`:

**Required sections:**
- `ConnectionStrings:DefaultConnection` - SQL Server connection string
- `Jwt:Key` - JWT secret key (minimum 32 characters)
- `Jwt:Issuer` - Token issuer
- `Jwt:Audience` - Token audience
- `Jwt:ExpiryMinutes` - Token expiration time
- `Serilog` - Logging configuration (console + file)
- `Swagger:Enabled` - Enable/disable Swagger UI (default: false)

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
- ✅ Manual entry of official LOTTO and LOTTO PLUS results (including download from XLotto with LLM extraction)
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

The project is actively under development with a focus on delivering core MVP functionality. The system currently supports LOTTO and LOTTO PLUS games (6 numbers from 1-49) with fully automated win verification.

### Architecture Status

- ✅ Vertical Slice Architecture implemented
- ✅ MediatR CQRS pattern in use
- ✅ FluentValidation for all inputs
- ✅ Global exception handling middleware
- ✅ Structured logging with Serilog
- ✅ Integration tests with WebApplicationFactory

### Performance Metrics

- ✅ **Target Met**: Verify 100 ticket sets against 1 draw result in ≤ 2 seconds

### Recent Updates

- Added Dockerfile for containerization
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
**Last Updated**: November 11, 2025
