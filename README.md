# LottoTM - Lotto Ticket Management System

A modern web application for managing and verifying Polish LOTTO tickets. LottoTM helps players efficiently track multiple number sets and automatically identifies winning combinations, eliminating manual verification errors and saving time.

## Table of Contents

- [Project Description](#project-description)
- [Tech Stack](#tech-stack)
- [Getting Started Locally](#getting-started-locally)
- [Available Scripts](#available-scripts)
- [Project Scope](#project-scope)
- [Project Status](#project-status)
- [License](#license)

---

## Project Description

LottoTM addresses a common challenge faced by LOTTO players: managing and verifying multiple number sets after each draw. Manual verification is time-consuming, error-prone, and can lead to missed winnings.

### Key Features

- **User Account Management**: Secure registration and authentication with JWT
- **Ticket Management**: Store up to 100 number sets per user with full CRUD operations
- **Draw Management**: Manual entry of official LOTTO draw results
- **Smart Verification**: Check all tickets against draws in a specified date range
- **Number Generators**:
  - Random generator for single sets
  - System generator creating 9 sets covering all numbers 1-49
- **Visual Indicators**: Bold highlighting of winning numbers and clear win labels (3, 4, 5, or 6 matches)

### Performance

- Verifies 100 ticket sets against 1 draw in under 2 seconds
- Supports up to 100 concurrent users in MVP
- Handles 1M+ database records

---

## Tech Stack

### Backend

- **.NET 8** (C#) - Modern, high-performance framework
- **ASP.NET Core Web API** - RESTful API with Minimal APIs pattern
- **Entity Framework Core 8** - ORM for database operations
- **Vertical Slice Architecture** - Feature-based code organization
- **JWT Authentication** - Stateless, secure authentication
- **SQL Server 2022** - Production database (PostgreSQL recommended for MVP)

### Frontend

- **React 19** - Modern UI library with latest features
- **TypeScript** - Type-safe JavaScript for better code quality
- **Tailwind CSS** - Utility-first CSS framework for rapid styling
- **React Router** - Client-side routing
- **React Hook Form** - Efficient form management
- **Vite** - Fast build tool and development server

### Security

- **bcrypt** - Password hashing (min. 10 rounds)
- **JWT tokens** - 24-hour expiration time
- **HTTPS** - Required for production
- **CORS policies** - Configured for secure cross-origin requests
- **Rate limiting** - Protection against brute force attacks

### Deployment

- **Cloud Hosting**: Webio.pl (configured)
- **CI/CD**: GitHub Actions for automated testing and deployment
- **Database**: SQL Server Express (MVP) / SQL Server Standard (production)

---

## Getting Started Locally

### Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js**: v20.x or later (LTS recommended)
- **.NET SDK**: 8.0 or later
- **SQL Server**: 2022 (Express edition sufficient for development)
- **Git**: For version control

### Backend Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/mulatom1/LottoTM.git
   cd LottoTM
   ```

2. **Navigate to the backend directory**:
   ```bash
   cd src/server/api
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Configure database connection**:
   - Create a `appsettings.Development.json` file
   - Add your SQL Server connection string:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Server=localhost;Database=LottoTM;Trusted_Connection=True;TrustServerCertificate=True"
       },
       "JwtSettings": {
         "SecretKey": "your-secret-key-min-32-characters",
         "ExpiryInMinutes": 1440
       }
     }
     ```

5. **Run database migrations**:
   ```bash
   dotnet ef database update
   ```

6. **Start the backend server**:
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001`

### Frontend Setup

1. **Navigate to the frontend directory**:
   ```bash
   cd src/client/app01
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Configure environment variables**:
   - Create a `.env.local` file
   - Add the API URL:
     ```
     VITE_API_URL=https://localhost:5001/api
     ```

4. **Start the development server**:
   ```bash
   npm run dev
   ```

   The application will be available at `http://localhost:5173`

### First Time Setup

1. Open your browser and navigate to `http://localhost:5173`
2. Click "Register" to create a new account
3. Enter your email and password (no email verification required in MVP)
4. You'll be automatically logged in and redirected to the dashboard

---

## Available Scripts

### Frontend (React)

Located in `src/client/app01`:

- **`npm run dev`** - Start development server with hot reload (development mode)
- **`npm run prod`** - Start development server with production configuration
- **`npm run build`** - Build production-ready application (TypeScript compilation + Vite build)
- **`npm run preview`** - Preview production build locally
- **`npm run lint`** - Run ESLint to check code quality

### Backend (.NET)

Located in `src/server/api`:

- **`dotnet run`** - Start the API server
- **`dotnet watch run`** - Start with hot reload for development
- **`dotnet build`** - Compile the application
- **`dotnet test`** - Run unit tests
- **`dotnet ef migrations add <name>`** - Create a new database migration
- **`dotnet ef database update`** - Apply pending migrations to database
- **`dotnet ef database drop`** - Drop the database (use with caution)

---

## Project Scope

### MVP Features (Must Have)

#### Authentication & Authorization
- User registration with email and password
- JWT-based login/logout
- Data isolation between users
- No email verification required (immediate access)

#### Ticket Management
- View all saved ticket sets (max 100 per user)
- Add tickets manually (6 unique numbers, 1-49 range)
- Edit existing tickets
- Delete tickets
- Random number generator (single set)
- System generator (9 sets covering all numbers 1-49)

#### Draw Management
- Manual entry of official LOTTO draw results
- Store draw history
- Date picker for draw date selection
- Real-time validation (6 unique numbers, 1-49 range)

#### Verification System
- Check tickets against draws in a date range
- Visual highlighting of winning numbers (bold text)
- Win labels for 3+ matches ("Wygrana 3 (trójka)", etc.)
- Performance: 100 tickets verified in <2 seconds
- Visual indicator for new unchecked draws

### Out of Scope (Future Versions)

The following features are explicitly excluded from MVP and planned for future releases:

- Support for other lottery games (MINI LOTTO, Multi Multi, EuroJackpot)
- Email verification during registration
- Automatic draw result fetching from external APIs
- Import/export tickets (CSV, PDF)
- Email or push notifications
- Social features (sharing tickets)
- Native mobile apps (iOS, Android)
- Prize categories and amounts
- AI/ML pattern analysis
- Payment system integration
- Group play (syndicates)

### Technical Constraints

- **Ticket Limit**: Maximum 100 tickets per user
- **Game Support**: Polish LOTTO only (6 from 49)
- **Number Range**: 1-49 inclusive
- **Verification Window**: Flexible date range selection
- **Response Time**: API responses <500ms (95th percentile)
- **Page Load**: First Contentful Paint <3 seconds

---

## Project Status

### Current Phase: Step 4 - Documentation (README)

The project is in active development following a structured timeline:

#### Completed Phases

- ✅ **Phase 0**: Project Planning & PRD (2 days)
  - Product Requirements Document finalized
  - Technical stack analysis completed
  - Repository structure established

- ✅ **Phase 1**: Foundation (3 days) - PLANNED
  - Database schema design
  - Authentication module (JWT)
  - Basic frontend routing and layout
  - CI/CD pipeline setup

- ⏳ **Phase 2**: Ticket Management (3 days) - PLANNED
  - CRUD operations for tickets
  - Random number generator
  - System generator (9 sets)
  - 100-ticket limit enforcement

- ⏳ **Phase 3**: Draw Management (3 days) - PLANNED
  - Draw result entry interface
  - Draw history viewing
  - Input validation

- ⏳ **Phase 4**: Verification Engine (3 days) - PLANNED
  - Verification algorithm implementation
  - Date range picker
  - Results visualization
  - Performance optimization

- ⏳ **Phase 5**: Testing & QA (1 day) - PLANNED
  - Unit tests for core algorithms
  - Integration tests for API endpoints
  - Performance testing

- ⏳ **Phase 6**: Deployment (1 day) - PLANNED
  - Production environment setup
  - Database migration
  - Smoke testing

### Timeline

- **MVP Target**: 14 days from project start
- **Current Sprint**: Step 4 - Documentation
- **Next Milestone**: Complete foundation architecture

### Success Metrics

The MVP will be considered successful when:

1. **Generator Acceptance**: ≥75% of generated tickets are saved by users
2. **Verification Speed**: 100 tickets checked in ≤2 seconds
3. **User Retention**: 40% weekly active users
4. **User Satisfaction**: ≥4/5 average rating
5. **System Reliability**: <1% sessions end in critical errors

### Known Issues & Limitations

- Email verification not implemented (by design for MVP)
- Manual draw entry required (no automatic fetching)
- Polish LOTTO only (other games planned for future)
- Desktop-optimized UI (mobile responsive but not optimized)
- No offline support

### Contributing

This is currently a private project under active development. Contribution guidelines will be published after MVP release.

### Roadmap

#### Version 1.1 (Post-MVP)
- Email verification
- Password reset functionality
- Email notifications for new draws
- CSV export for tickets
- Prize categories and estimated amounts

#### Version 2.0 (Future)
- Support for additional lottery games
- Automatic draw result fetching
- AI-powered number recommendations
- Progressive Web App (PWA) capabilities
- Dark mode support

---

## License

Copyright © 2025 Tomasz Mularczyk. All rights reserved.

This project is proprietary software. Unauthorized copying, distribution, or modification is prohibited without explicit permission from the author.

---

## Contact & Support

- **Author**: Tomasz Mularczyk
- **Project Repository**: [github.com/mulatom1/LottoTM](https://github.com/mulatom1/LottoTM)
- **Documentation**: See `.ai/prd.md` for detailed product requirements
- **Technical Docs**: See `.ai/tech-stack.md` for architecture analysis

---

**Built with** ❤️ **for LOTTO players in Poland**
