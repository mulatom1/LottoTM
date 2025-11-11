# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LottoTM is a lottery ticket management system built as a full-stack application. The system helps players manage multiple number sets and identify wins by checking them against draw results. This is an MVP version focused on core functionality.

**Tech Stack:**
- **Backend:** .NET 8 (C#) with ASP.NET Core Minimal APIs, SQL Server 2022, Entity Framework Core
- **Frontend:** React 19, TypeScript, Tailwind CSS 4, React Router 7, Vite 7
- **Testing:** xUnit (backend), integration tests with WebApplicationFactory
- **Architecture:** Vertical Slice Architecture with MediatR, FluentValidation, Serilog

## Common Commands

### Frontend (React)
Located in `src/client/app01/`

```bash
# Development server (dev mode)
npm run dev

# Development server (prod mode)
npm run prod

# Build for production
npm run build

# Lint code
npm run lint

# Preview production build
npm run preview
```

### Backend (.NET)
Located in `src/server/LottoTM.Server.Api/`

```bash
# Run the API server
dotnet run --project src/server/LottoTM.Server.Api/LottoTM.Server.Api.csproj

# Build solution
dotnet build LottoTM.sln

# Run all tests
dotnet test LottoTM.sln

# Run specific test project
dotnet test tests/server/LottoTM.Server.Api.Tests/LottoTM.Server.Api.Tests.csproj

# Entity Framework migrations
dotnet ef migrations add <MigrationName> --project src/server/LottoTM.Server.Api
dotnet ef database update --project src/server/LottoTM.Server.Api
```

## Architecture

### Backend: Vertical Slice Architecture

The backend follows Vertical Slice Architecture where each feature is self-contained in its own folder under `Features/`:

```
Features/
  ApiVersion/
    Endpoint.cs      - Minimal API endpoint definition
    Contracts.cs     - Request/Response DTOs (using MediatR IRequest<T>)
    Handler.cs       - MediatR request handler (business logic)
    Validator.cs     - FluentValidation validator
```

**Pattern for new features:**
1. Create a new folder under `Features/`
2. Define contracts (Request/Response records implementing IRequest<Response>)
3. Create handler implementing `IRequestHandler<Request, Response>`
4. Add FluentValidation validator for the request
5. Register endpoint in `Endpoint.cs` using `IEndpointRouteBuilder.MapGet/Post/etc`
6. Register the endpoint in `Program.cs` by calling `YourFeature.Endpoint.AddEndpoint(app)`

**Key services configured in Program.cs:**
- MediatR for CQRS pattern
- FluentValidation for request validation
- JWT Bearer authentication
- Entity Framework Core with SQL Server
- Serilog for structured logging
- Global exception handling via `ExceptionHandlingMiddleware`

### Frontend: React with Context API

The frontend uses React 19 with functional components and hooks. Application state is managed through Context API:

```
src/
  main.tsx                          - App entry point, routing setup
  context/
    app-context.ts                  - Context definition
    app-context-provider.tsx        - Context provider with user auth state
  pages/
    home/home-page.tsx
    auth/login-page.tsx
    auth/register-page.tsx
    draws/draws-page.tsx            - Draw results management
    tickets/tickets-page.tsx        - Ticket management (note: typo in folder name)
    checks/checks-page.tsx          - Compare and checks drows with user tickets
  services/
    api-service.ts                  - HTTP client for backend API
    contracts/                      - TypeScript interfaces for API responses
```

**AppContext provides:**
- `user`: Current user object with JWT token
- `isLoggedIn`: Authentication state
- `login(user)`: Sets user and token
- `logout()`: Clears user and token
- `getApiService()`: Returns configured ApiService instance

**ApiService pattern:**
- Configured with `VITE_API_URL` and `VITE_APP_TOKEN` from environment variables
- Automatically includes headers: `Content-Type`, `X-TOKEN` (app token), `Authorization` (Bearer token)
- Example: `apiService.getApiVersion()` returns `Promise<ApiVersionResponse>`

### Environment Configuration

**Frontend (.env files in src/client/app01/):**
- `.env` - Base configuration
- `.env.dev` - Development overrides
- `.env.prod` - Production overrides
- Required: `VITE_API_URL`, `VITE_APP_TOKEN`

**Backend (appsettings.json in src/server/LottoTM.Server.Api/):**
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- Required sections: `ConnectionStrings:DefaultConnection`, `Jwt:Key/Issuer/Audience`, `ApiVersion`, `Swagger:Enabled`, `Serilog`

### Testing Strategy

Backend tests use `WebApplicationFactory<Program>` for integration testing:
- Tests are located in `tests/server/LottoTM.Server.Api.Tests/`
- Each feature should have corresponding test file in `Futures/{FeatureName}/EndpointTests.cs`
- Tests use in-memory configuration via `ConfigureAppConfiguration`
- Entity Framework can use InMemory provider for testing

### Global Exception Handling

All unhandled exceptions are caught by `ExceptionHandlingMiddleware` which:
- Logs errors with Serilog including stack trace analysis
- Returns standardized `ProblemDetails` JSON response
- Includes error source class name for debugging

## Important Notes

- The frontend folder name has a typo: `tickets` instead of `tickets`
- Backend uses Polish comments in some files (e.g., validation error messages)
- JWT configuration requires minimum 32-character key
- CORS is configured to allow all origins (consider restricting in production)
- Swagger is disabled by default (controlled via `Swagger:Enabled` in appsettings)
- Serilog logs to console and file (`Logs/applog-{Date}.txt`)
- The `Program` class is made public/partial for test project access


# AI Rules for {{project-name}}

{{project-description}}

## BACKEND

### Guidelines for DOTNET

#### ENTITY_FRAMEWORK

- Use the repository and unit of work patterns to abstract data access logic and simplify testing
- Implement eager loading with Include() to avoid N+1 query problems for {{entity_relationships}}
- Use migrations for database schema changes and version control with proper naming conventions
- Apply appropriate tracking behavior (AsNoTracking() for read-only queries) to optimize performance
- Implement query optimization techniques like compiled queries for frequently executed database operations
- Use value conversions for complex property transformations and proper handling of {{custom_data_types}}

#### ASP_NET

- Use minimal APIs for simple endpoints in .NET 6+ applications to reduce boilerplate code
- Implement the mediator pattern with MediatR for decoupling request handling and simplifying cross-cutting concerns
- Use API controllers with model binding and validation attributes for {{complex_data_models}}
- Apply proper response caching with cache profiles and ETags for improved performance on {{high_traffic_endpoints}}
- Implement proper exception handling with ExceptionFilter or middleware to provide consistent error responses
- Use dependency injection with scoped lifetime for request-specific services and singleton for stateless services

## DATABASE

### Guidelines for SQL

#### SQLSERVER

- Use parameterized queries to prevent SQL injection
- Implement proper indexing strategies based on query patterns
- Use stored procedures for complex business logic that requires database access to {{business_entities}}

## DEVOPS

### Guidelines for CI_CD

#### GITHUB_ACTIONS

- Check if `package.json` exists in project root and summarize key scripts
- Check if `.nvmrc` exists in project root
- Check if `.env.example` exists in project root to identify key `env:` variables
- Always use terminal command: `git branch -a | cat` to verify whether we use `main` or `master` branch
- Always use `env:` variables and secrets attached to jobs instead of global workflows
- Always use `npm ci` for Node-based dependency setup
- Extract common steps into composite actions in separate files
- Once you're done, as a final step conduct the following: for each public action always use <tool>"Run Terminal"</tool> to see what is the most up-to-date version (use only major version) - extract tag_name from the response:
- ```bash curl -s https://api.github.com/repos/{owner}/{repo}/releases/latest ```

## FRONTEND

### Guidelines for REACT

#### REACT_CODING_STANDARDS

- Use functional components with hooks instead of class components
- Implement React.memo() for expensive components that render often with the same props
- Utilize React.lazy() and Suspense for code-splitting and performance optimization
- Use the useCallback hook for event handlers passed to child components to prevent unnecessary re-renders
- Prefer useMemo for expensive calculations to avoid recomputation on every render
- Implement useId() for generating unique IDs for accessibility attributes
- Use the new use hook for data fetching in React 19+ projects
- Leverage Server Components for {{data_fetching_heavy_components}} when using React with Next.js or similar frameworks
- Consider using the new useOptimistic hook for optimistic UI updates in forms
- Use useTransition for non-urgent state updates to keep the UI responsive

#### REACT_ROUTER

- Use createBrowserRouter instead of BrowserRouter for better data loading and error handling
- Implement lazy loading with React.lazy() for route components to improve initial load time
- Use the useNavigate hook instead of the navigate component prop for programmatic navigation
- Leverage loader and action functions to handle data fetching and mutations at the route level
- Implement error boundaries with errorElement to gracefully handle routing and data errors
- Use relative paths with dot notation (e.g., "../parent") to maintain route hierarchy flexibility
- Utilize the useRouteLoaderData hook to access data from parent routes
- Implement fetchers for non-navigation data mutations
- Use route.lazy() for route-level code splitting with automatic loading states
- Implement shouldRevalidate functions to control when data revalidation happens after navigation

#### REDUX

- Use Redux Toolkit (RTK) instead of plain Redux to reduce boilerplate code
- Implement the slice pattern for organizing related state, reducers, and actions
- Use RTK Query for data fetching to eliminate manual loading state management
- Prefer createSelector for memoized selectors to prevent unnecessary recalculations
- Normalize complex state structures using a flat entities approach with IDs as references
- Implement middleware selectively and avoid overusing thunks for simple state updates
- Use the listener middleware for complex side effects instead of thunks where appropriate
- Leverage createEntityAdapter for standardized CRUD operations
- Implement Redux DevTools for debugging in development environments
- Use typed hooks (useAppDispatch, useAppSelector) with TypeScript for type safety

#### REACT_QUERY

- Use TanStack Query (formerly React Query) with appropriate staleTime and gcTime based on data freshness requirements
- Implement the useInfiniteQuery hook for pagination and infinite scrolling
- Use optimistic updates for mutations to make the UI feel more responsive
- Leverage queryClient.setQueryDefaults to establish consistent settings for query categories
- Use suspense mode with <Suspense> boundaries for a more declarative data fetching approach
- Implement retry logic with custom backoff algorithms for transient network issues
- Use the select option to transform and extract specific data from query results
- Implement mutations with onMutate, onError, and onSettled for robust error handling
- Use Query Keys structuring pattern ([entity, params]) for better organization and automatic refetching
- Implement query invalidation strategies to keep data fresh after mutations


### Guidelines for STYLING

#### TAILWIND

- Use the @layer directive to organize styles into components, utilities, and base layers
- Implement Just-in-Time (JIT) mode for development efficiency and smaller CSS bundles
- Use arbitrary values with square brackets (e.g., w-[123px]) for precise one-off designs
- Leverage the @apply directive in component classes to reuse utility combinations
- Implement the Tailwind configuration file for customizing theme, plugins, and variants
- Use component extraction for repeated UI patterns instead of copying utility classes
- Leverage the theme() function in CSS for accessing Tailwind theme values
- Implement dark mode with the dark: variant
- Use responsive variants (sm:, md:, lg:, etc.) for adaptive designs
- Leverage state variants (hover:, focus:, active:, etc.) for interactive elements
