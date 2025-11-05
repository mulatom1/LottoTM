# LottoTM - Lottery Ticket Management System

A modern web application designed to simplify lottery ticket management and automated win verification for LOTTO players.

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [Getting Started](#getting-started)
- [Available Scripts](#available-scripts)
- [Project Scope](#project-scope)
- [Project Status](#project-status)
- [License](#license)

## Overview

LottoTM solves a common problem for lottery players: manually checking multiple ticket sets against draw results is time-consuming, monotonous, and prone to human error. This system automates the verification process, enabling users to check up to 100 ticket sets against official LOTTO draw results in less than 2 seconds.

### Key Benefits

- **Fast Verification**: Check 100 ticket sets in under 2 seconds
- **Error Prevention**: Automated matching eliminates human error
- **Organized Management**: Store and manage up to 100 ticket sets per user
- **Flexible Checking**: Verify wins across custom date ranges
- **Smart Generators**: Create random tickets or system-generated sets covering all numbers 1-49

## Tech Stack

### Backend
- **.NET 8** (C#)
- **ASP.NET Core Web API** with Minimal APIs
- **Entity Framework Core** for data access
- **SQL Server 2022** database
- **Vertical Slice Architecture** for code organization
- **Serilog** for structured logging
- **FluentValidation** for input validation
- **JWT** (JSON Web Tokens) for authentication
- **MediatR** for CORS support
- Global exception handling middleware

### Frontend
- **React 19.1.1** with TypeScript
- **React Router 7.9.5** for navigation
- **Tailwind CSS 4.1.16** for styling
- **Vite 7.1.7** as build tool
- **ESLint** for code quality

## Features

### MVP (Version 1.0.0)

#### User Management
- User registration with email and password
- JWT-based authentication (login/logout)
- Data isolation between users
- Support for up to 100 ticket sets per user

#### Draw Management
- Manual entry of official LOTTO and LOTTO PLUS results (6 numbers from 1-49)
- Historical draw results storage
- Input validation with real-time feedback
- Date-based filtering and sorting

#### Ticket Set Management
- View all saved ticket sets
- Add tickets manually with validation
- Edit and delete existing tickets
- Random ticket generator
- System generator: creates 9 tickets covering all numbers 1-49
- Duplicate prevention system

#### Win Verification
- Check tickets against draw results within a custom date range
- Visual highlighting of winning numbers (bold formatting)
- Win labels displaying match count ("3 matches", "4 matches", etc.)
- High-performance verification engine

## Getting Started

### Prerequisites

- **Node.js** (v18 or higher recommended)
- **.NET 8 SDK**
- **SQL Server 2022** (Express edition or higher)
- **Git**

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/mulatom1/LottoTM.git
   cd LottoTM
   ```

2. **Setup Backend**
   ```bash
   cd src/server/LottoTM.Server.Api
   
   # Restore dependencies
   dotnet restore
   
   # Update database connection string in appsettings.json
   # Configure your SQL Server connection details
   
   # Apply database migrations
   dotnet ef database update
   
   # Run the API
   dotnet run
   ```
   
   The API will start at `https://localhost:7000`, `http://localhost:5000` (or configured port)

3. **Setup Frontend**
   ```bash
   cd src/client/app01
   
   # Install dependencies
   npm install
   
   # Start development server
   npm run dev
   ```
   
   The application will open at `http://localhost:5173`

### Configuration

#### Backend Configuration
Edit `appsettings.json` or `appsettings.Development.json`:
- Configure database connection string
- Set JWT secret and expiration
- Configure CORS allowed origins
- Adjust logging levels (Serilog)

#### Frontend Configuration
The frontend uses Vite environment modes:
- Development mode: `npm run dev` (uses `--mode dev`)
- Production mode: `npm run prod` (uses `--mode prod`)

Configure API endpoints in environment-specific configuration files.

## Available Scripts

### Frontend (src/client/app01)

```bash
# Start development server with hot reload
npm run dev

# Start production mode
npm run prod

# Build for production
npm run build

# Run ESLint
npm run lint

# Preview production build
npm run preview
```

### Backend (src/server/LottoTM.Server.Api)

```bash
# Run the application
dotnet run

# Build the project
dotnet build

# Run tests
dotnet test

# Apply database migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add <MigrationName>
```

## Project Scope

### Included in MVP

✅ User registration and JWT authentication  
✅ Manual draw result entry and management  
✅ Ticket set CRUD operations (up to 100 per user)  
✅ Random and system ticket generators  
✅ Win verification with date range filtering  
✅ Visual win indication and match counting  
✅ Real-time input validation  
✅ Duplicate ticket prevention  

### Future Enhancements (Out of Scope for MVP)

❌ Support for other lottery games (MINI LOTTO, EuroJackpot, etc.)  
❌ Email verification during registration  
❌ Automatic draw result fetching from external APIs  
❌ Import/Export tickets (CSV, PDF)  
❌ Email or push notifications  
❌ Social features (sharing tickets)  
❌ Native mobile applications  
❌ Prize amount information  
❌ AI/ML analysis and recommendations  
❌ Payment system integration  
❌ Group betting features  

## Project Status

**Current Version**: MVP 1.3 (In Development)

The project is actively under development with focus on core MVP features. The system supports LOTTO and LOTTO PLUS games (6 numbers from 1-49) with automated win verification functionality.

### Performance Goals
- ✅ Verify 100 ticket sets against 1 draw result in ≤ 2 seconds

## License

This project is licensed under the terms specified in the [LICENSE](LICENSE) file.

---

**Author**: Tomasz Mularczyk  
**Last Updated**: November 5, 2025
