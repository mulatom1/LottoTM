import type { ApiVersionResponse } from "./contracts/api-version-response";
import type { AuthLoginRequest } from "./contracts/auth-login-request";
import type { AuthLoginResponse } from "./contracts/auth-login-response";
import type { AuthRegisterRequest } from "./contracts/auth-register-request";
import type { AuthRegisterResponse } from "./contracts/auth-register-response";
import type { DrawsCreateRequest } from "./contracts/draws-create-request";
import type { DrawsCreateResponse } from "./contracts/draws-create-response";
import type { DrawsDeleteResponse } from "./contracts/draws-delete-response";
import type { DrawsGetByIdResponse } from "./contracts/draws-get-by-id-response";
import type { DrawsGetListRequest } from "./contracts/draws-get-list-request";
import type { DrawsGetListResponse } from "./contracts/draws-get-list-response";
import type { DrawsUpdateRequest } from "./contracts/draws-update-request";
import type { DrawsUpdateResponse } from "./contracts/draws-update-response";
import type { TicketsCreateRequest } from "./contracts/tickets-create-request";
import type { TicketsCreateResponse } from "./contracts/tickets-create-response";
import type { TicketsGetByIdResponse } from "./contracts/tickets-get-by-id-response";
import type { TicketsGetListRequest } from "./contracts/tickets-get-list-request";
import type { TicketsGetListResponse } from "./contracts/tickets-get-list-response";
import type { TicketsPutByIdRequest } from "./contracts/tickets-put-by-id-request";
import type { TicketsPutByIdResponse } from "./contracts/tickets-put-by-id-response";
import type { TicketsDeleteResponse } from "./contracts/tickets-delete-response";
import type { TicketsGenerateRandomResponse } from "./contracts/tickets-generate-random-response";
import type { TicketsGenerateSystemResponse } from "./contracts/tickets-generate-system-response";
import type { VerificationCheckRequest } from "./contracts/verification-check-request";
import type { VerificationCheckResponse } from "./contracts/verification-check-response";
import type { XLottoActualDrawsRequest } from "./contracts/xlotto-actual-draws-request";
import type { XLottoActualDrawsResponse } from "./contracts/xlotto-actual-draws-response";
import type { XLottoIsEnabledResponse } from "./contracts/xlotto-is-enabled-response";
import type { TicketsImportCsvResponse } from "./contracts/tickets-import-csv-response";
import type { TicketsDeleteAllResponse } from "./contracts/tickets-delete-all-response";

export class ApiService {
    private apiUrl: string = '';
    private appToken: string = '';
    private usrToken: string = '';

    constructor(apiUrl: string, appToken: string) {
      this.setApiUrl(apiUrl);
      this.setAppToken(appToken);
    }   
    
    private setApiUrl(url: string) {
      this.apiUrl = url;
    }

    private setAppToken(appToken: string) {
      this.appToken = appToken;
    }

    public setUsrToken(usrToken: string) {
      this.usrToken = usrToken;
    }

    public getUsrToken(): string {
      return this.usrToken;
    }

    public getHeaders(): Record<string, string> {
      return {
        'Content-Type': 'application/json',
        'X-TOKEN': this.appToken,
        'Authorization': `Bearer ${this.usrToken}`,
      };
    }

    public getApiUrl(): string {
      return this.apiUrl;
    }

    public async getApiVersion(): Promise<ApiVersionResponse> {
      const response = await fetch(`${this.apiUrl}/api/version`, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        throw new Error(`Error fetching API version: ${response.statusText}`);
      }

      const result = await response.json();
      return result;
    }

    /**
     * Authenticate user with email and password
     * POST /api/auth/login
     * @param request - User credentials (email and password)
     * @returns Promise<AuthLoginResponse> - JWT token and user information
     * @throws Error if authentication fails (401) or validation fails (400)
     */
    public async authLogin(request: AuthLoginRequest): Promise<AuthLoginResponse> {
      const response = await fetch(`${this.apiUrl}/api/auth/login`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          const errorData = await response.json();
          throw new Error(errorData.error || 'Nieprawidłowy email lub hasło');
        }

        if (response.status === 400) {
          const errorData = await response.json();
          const errorMessages = errorData.errors
            ? Object.values(errorData.errors).flat().join(', ')
            : 'Błąd walidacji danych wejściowych';
          throw new Error(errorMessages);
        }

        throw new Error(`Error during login: ${response.statusText}`);
      }

      const result: AuthLoginResponse = await response.json();
      return result;
    }

    /**
     * Register a new user account
     * POST /api/auth/register
     * @param request - User registration data (email, password, confirmPassword)
     * @returns Promise<AuthRegisterResponse> - Success message
     * @throws Error if validation fails (400) or server error occurs (500)
     */
    public async authRegister(request: AuthRegisterRequest): Promise<AuthRegisterResponse> {
      const response = await fetch(`${this.apiUrl}/api/auth/register`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas rejestracji');
        }

        throw new Error(`Error during registration: ${response.statusText}`);
      }

      const result: AuthRegisterResponse = await response.json();
      return result;
    }

    /**
     * Create a new lottery draw result
     * POST /api/draws
     * @param request - Draw data (drawDate in YYYY-MM-DD format, numbers array of 6 integers 1-49)
     * @returns Promise<DrawsCreateResponse> - Success message
     * @throws Error if unauthorized (401), validation fails (400), or server error occurs (500)
     */
    public async drawsCreate(request: DrawsCreateRequest): Promise<DrawsCreateResponse> {
      const response = await fetch(`${this.apiUrl}/api/draws`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas tworzenia losowania');
        }

        throw new Error(`Error creating draw: ${response.statusText}`);
      }

      const result: DrawsCreateResponse = await response.json();
      return result;
    }

    /**
     * Update an existing lottery draw result
     * PUT /api/draws/{id}
     * @param id - Draw ID to update
     * @param request - Draw data (drawDate in YYYY-MM-DD format, numbers array of 6 integers 1-49)
     * @returns Promise<DrawsUpdateResponse> - Success message
     * @throws Error if unauthorized (401), forbidden (403), not found (404), validation fails (400), or server error occurs (500)
     */
    public async drawsUpdate(id: number, request: DrawsUpdateRequest): Promise<DrawsUpdateResponse> {
      const response = await fetch(`${this.apiUrl}/api/draws/${id}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 403) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Brak uprawnień administratora. Tylko administratorzy mogą edytować losowania.');
        }

        if (response.status === 404) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Losowanie o podanym ID nie istnieje');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          // Handle business logic errors (e.g., duplicate draw date)
          if (errorData.detail) {
            throw new Error(errorData.detail);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas aktualizacji losowania');
        }

        throw new Error(`Error updating draw: ${response.statusText}`);
      }

      const result: DrawsUpdateResponse = await response.json();
      return result;
    }

    /**
     * Delete a lottery draw result (admin only)
     * DELETE /api/draws/{id}
     * @param id - Draw ID to delete
     * @returns Promise<DrawsDeleteResponse> - Success message
     * @throws Error if unauthorized (401), forbidden (403), not found (404), validation fails (400), or server error occurs (500)
     */
    public async drawsDelete(id: number): Promise<DrawsDeleteResponse> {
      const response = await fetch(`${this.apiUrl}/api/draws/${id}`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 403) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Brak uprawnień administratora. Tylko administratorzy mogą usuwać losowania.');
        }

        if (response.status === 404) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Losowanie o podanym ID nie istnieje');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas usuwania losowania');
        }

        throw new Error(`Error deleting draw: ${response.statusText}`);
      }

      const result: DrawsDeleteResponse = await response.json();
      return result;
    }

    /**
     * Get a specific lottery draw by ID
     * GET /api/draws/{id}
     * @param id - Draw ID to retrieve
     * @returns Promise<DrawsGetByIdResponse> - Draw details including date, numbers, and creation timestamp
     * @throws Error if unauthorized (401), not found (404), validation fails (400), or server error occurs (500)
     */
    public async drawsGetById(id: number): Promise<DrawsGetByIdResponse> {
      const response = await fetch(`${this.apiUrl}/api/draws/${id}`, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 404) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Losowanie o podanym ID nie istnieje');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas pobierania losowania');
        }

        throw new Error(`Error fetching draw: ${response.statusText}`);
      }

      const result: DrawsGetByIdResponse = await response.json();
      return result;
    }

    /**
     * Get a paginated list of lottery draws with sorting options
     * GET /api/draws
     * @param request - Optional query parameters (page, pageSize, sortBy, sortOrder)
     * @returns Promise<DrawsGetListResponse> - Paginated list of draws with metadata
     * @throws Error if unauthorized (401), validation fails (400), or server error occurs (500)
     */
    public async drawsGetList(request?: DrawsGetListRequest): Promise<DrawsGetListResponse> {
      // Build query parameters with defaults
      const params = new URLSearchParams();

      if (request?.page !== undefined) {
        params.append('page', request.page.toString());
      }

      if (request?.pageSize !== undefined) {
        params.append('pageSize', request.pageSize.toString());
      }

      if (request?.sortBy) {
        params.append('sortBy', request.sortBy);
      }

      if (request?.sortOrder) {
        params.append('sortOrder', request.sortOrder);
      }

      if (request?.dateFrom) {
        params.append('dateFrom', request.dateFrom);
      }

      if (request?.dateTo) {
        params.append('dateTo', request.dateTo);
      }

      const queryString = params.toString();
      const url = queryString 
        ? `${this.apiUrl}/api/draws?${queryString}`
        : `${this.apiUrl}/api/draws`;

      const response = await fetch(url, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji parametrów zapytania');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas pobierania listy losowań');
        }

        throw new Error(`Error fetching draws list: ${response.statusText}`);
      }

      const result: DrawsGetListResponse = await response.json();
      return result;
    }

    /**
     * Get a specific lottery ticket by ID (user must own the ticket)
     * GET /api/tickets/{id}
     * @param id - Ticket ID to retrieve
     * @returns Promise<TicketsGetByIdResponse> - Ticket details including user ID, numbers, and creation timestamp
     * @throws Error if unauthorized (401), forbidden (403), not found (404), validation fails (400), or server error occurs (500)
     */
    public async ticketsGetById(id: number): Promise<TicketsGetByIdResponse> {
      const response = await fetch(`${this.apiUrl}/api/tickets/${id}`, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 403) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Nie masz uprawnień do tego zasobu');
        }

        if (response.status === 404) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Zestaw o podanym ID nie istnieje');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas pobierania zestawu');
        }

        throw new Error(`Error fetching ticket: ${response.statusText}`);
      }

      const result: TicketsGetByIdResponse = await response.json();
      return result;
    }

    /**
     * Get all lottery tickets for the authenticated user with optional filtering
     * GET /api/tickets
     * @param request - Optional query parameters (groupName filter)
     * @returns Promise<TicketsGetListResponse> - List of tickets with pagination metadata
     * @throws Error if unauthorized (401), validation fails (400), or server error occurs (500)
     */
    public async ticketsGetList(request?: TicketsGetListRequest): Promise<TicketsGetListResponse> {
      // Build query parameters
      const params = new URLSearchParams();

      if (request?.groupName) {
        params.append('groupName', request.groupName);
      }

      const queryString = params.toString();
      const url = queryString
        ? `${this.apiUrl}/api/tickets?${queryString}`
        : `${this.apiUrl}/api/tickets`;

      const response = await fetch(url, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji parametrów zapytania');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas pobierania listy zestawów');
        }

        throw new Error(`Error fetching tickets list: ${response.statusText}`);
      }

      const result: TicketsGetListResponse = await response.json();
      return result;
    }

    /**
     * Create a new lottery ticket with 6 unique numbers
     * POST /api/tickets
     * @param request - Ticket data (numbers array of 6 unique integers 1-49)
     * @returns Promise<TicketsCreateResponse> - Created ticket ID and success message
     * @throws Error if unauthorized (401), validation fails (400), limit reached (400), duplicate set (400), or server error occurs (500)
     */
    public async ticketsCreate(request: TicketsCreateRequest): Promise<TicketsCreateResponse> {
      const response = await fetch(`${this.apiUrl}/api/tickets`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          // Handle specific business logic errors
          if (errorData.detail) {
            throw new Error(errorData.detail);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas tworzenia zestawu');
        }

        throw new Error(`Error creating ticket: ${response.statusText}`);
      }

      const result: TicketsCreateResponse = await response.json();
      return result;
    }

    /**
     * Update an existing lottery ticket with new numbers
     * PUT /api/tickets/{id}
     * @param id - Ticket ID to update
     * @param request - Ticket data (numbers array of 6 unique integers 1-49)
     * @returns Promise<TicketsPutByIdResponse> - Success message
     * @throws Error if unauthorized (401), forbidden (403), not found (404), validation fails (400), duplicate set (400), or server error occurs (500)
     */
    public async ticketsUpdate(id: number, request: TicketsPutByIdRequest): Promise<TicketsPutByIdResponse> {
      const response = await fetch(`${this.apiUrl}/api/tickets/${id}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 403) {
          const errorData = await response.json();
          throw new Error(errorData.error || 'Brak dostępu do tego zasobu');
        }

        if (response.status === 404) {
          const errorData = await response.json();
          throw new Error(errorData.error || 'Zestaw nie został znaleziony');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          // Handle specific business logic errors
          if (errorData.detail) {
            throw new Error(errorData.detail);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas aktualizacji zestawu');
        }

        throw new Error(`Error updating ticket: ${response.statusText}`);
      }

      const result: TicketsPutByIdResponse = await response.json();
      return result;
    }

    /**
     * Delete a lottery ticket by ID (user must own the ticket)
     * DELETE /api/tickets/{id}
     * @param id - Ticket ID to delete
     * @returns Promise<TicketsDeleteResponse> - Success message confirming deletion
     * @throws Error if unauthorized (401), forbidden (403), validation fails (400), or server error occurs (500)
     */
    public async ticketsDelete(id: number): Promise<TicketsDeleteResponse> {
      const response = await fetch(`${this.apiUrl}/api/tickets/${id}`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 403) {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Brak dostępu do zasobu');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas usuwania zestawu');
        }

        throw new Error(`Error deleting ticket: ${response.statusText}`);
      }

      const result: TicketsDeleteResponse = await response.json();
      return result;
    }

    /**
     * Generate a random lottery ticket with 6 unique numbers (preview/proposal)
     * POST /api/tickets/generate-random
     * Returns a proposal without saving to database. User must manually save using POST /api/tickets.
     * @returns Promise<TicketsGenerateRandomResponse> - Generated numbers (200 OK)
     * @throws Error if unauthorized (401) or server error occurs (500)
     */
    public async ticketsGenerateRandom(): Promise<TicketsGenerateRandomResponse> {
      const response = await fetch(`${this.apiUrl}/api/tickets/generate-random`, {
        method: 'POST',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas generowania losowego zestawu');
        }

        throw new Error(`Error generating random ticket: ${response.statusText}`);
      }

      const result: TicketsGenerateRandomResponse = await response.json();
      return result;
    }

    /**
     * Generate 9 system lottery tickets covering all numbers 1-49 (preview/proposal)
     * POST /api/tickets/generate-system
     * Returns a proposal without saving to database. User must manually save using POST /api/tickets.
     * @returns Promise<TicketsGenerateSystemResponse> - 9 generated tickets with numbers (200 OK)
     * @throws Error if unauthorized (401) or server error occurs (500)
     */
    public async ticketsGenerateSystem(): Promise<TicketsGenerateSystemResponse> {
      const response = await fetch(`${this.apiUrl}/api/tickets/generate-system`, {
        method: 'POST',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas generowania zestawów systemowych');
        }

        throw new Error(`Error generating system tickets: ${response.statusText}`);
      }

      const result: TicketsGenerateSystemResponse = await response.json();
      return result;
    }

    /**
     * Verify all user tickets against draws within a given date range
     * POST /api/verification/check
     * @param request - Date range for verification (dateFrom, dateTo) and optional groupName filter
     * @returns Promise<VerificationCheckResponse> - Verification results with hits
     * @throws Error if unauthorized (401), validation fails (400), or server error occurs (500)
     */
    public async verificationCheck(request: VerificationCheckRequest): Promise<VerificationCheckResponse> {
      const response = await fetch(`${this.apiUrl}/api/verification/check`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => `${field}: ${(messages as string[]).join(', ')}`)
              .join('; ');
            throw new Error(errorMessages);
          }

          // Handle specific business logic errors
          if (errorData.detail) {
            throw new Error(errorData.detail);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas weryfikacji zestawów');
        }

        throw new Error(`Error during verification: ${response.statusText}`);
      }

      const result: VerificationCheckResponse = await response.json();
      return result;
    }

    /**
     * Get actual draw results from XLotto website via Gemini API
     * GET /api/xlotto/actual-draws
     * @param request - Date and lotto type (LOTTO or LOTTO PLUS)
     * @returns Promise<XLottoActualDrawsResponse> - JSON string containing latest LOTTO and LOTTO PLUS results
     * @throws Error if unauthorized (401), validation fails (400), or server error occurs (500)
     */
    public async xLottoActualDraws(request: XLottoActualDrawsRequest): Promise<XLottoActualDrawsResponse> {
      // Build query parameters
      const params = new URLSearchParams();
      params.append('Date', request.date);
      params.append('LottoType', request.lottoType);

      const url = `${this.apiUrl}/api/xlotto/actual-draws?${params.toString()}`;

      const response = await fetch(url, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          throw new Error('Błąd walidacji danych wejściowych');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas pobierania wyników z XLotto');
        }

        throw new Error(`Error fetching XLotto draws: ${response.statusText}`);
      }

      const result: XLottoActualDrawsResponse = await response.json();
      return result;
    }

    /**
     * Check if XLotto feature is enabled
     * GET /api/xlotto/is-enabled
     * @returns Promise<XLottoIsEnabledResponse> - Boolean indicating whether the feature is enabled
     * @throws Error if unauthorized (401) or server error occurs (500)
     */
    public async xLottoIsEnabled(): Promise<XLottoIsEnabledResponse> {
      const url = `${this.apiUrl}/api/xlotto/is-enabled`;

      const response = await fetch(url, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas sprawdzania statusu funkcji XLotto');
        }

        throw new Error(`Error checking XLotto feature status: ${response.statusText}`);
      }

      const result: XLottoIsEnabledResponse = await response.json();
      return result;
    }

    /**
     * Import lottery tickets from CSV file
     * POST /api/tickets/import-csv
     * @param file - CSV file with ticket data (max 1MB)
     * @returns Promise<TicketsImportCsvResponse> - Import report with imported/rejected counts and errors
     * @throws Error if unauthorized (401), feature disabled (404), validation fails (400), limit exceeded (400), or server error occurs (500)
     */
    public async ticketsImportCsv(file: File): Promise<TicketsImportCsvResponse> {
      const formData = new FormData();
      formData.append('file', file);

      // Get headers without Content-Type to let browser set multipart/form-data boundary
      const headers: Record<string, string> = {
        'X-TOKEN': this.appToken,
        'Authorization': `Bearer ${this.usrToken}`,
      };

      const response = await fetch(`${this.apiUrl}/api/tickets/import-csv`, {
        method: 'POST',
        headers: headers,
        body: formData
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 404) {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Funkcja importu CSV jest wyłączona');
        }

        if (response.status === 400) {
          const errorData = await response.json();

          // Extract validation errors from the response
          if (errorData.errors) {
            const errorMessages = Object.entries(errorData.errors)
              .map(([field, messages]) => {
                const msgArray = messages as string[];
                return `${field}: ${msgArray.join(', ')}`;
              })
              .join('; ');
            throw new Error(errorMessages);
          }

          // Handle specific business logic errors (e.g., limit exceeded)
          if (errorData.detail) {
            throw new Error(errorData.detail);
          }

          throw new Error('Błąd walidacji pliku CSV');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas importu CSV');
        }

        throw new Error(`Error importing CSV: ${response.statusText}`);
      }

      const result: TicketsImportCsvResponse = await response.json();
      return result;
    }

    /**
     * Export all user tickets to CSV file
     * GET /api/tickets/export-csv
     * @returns Promise<Blob> - CSV file content as blob for download
     * @throws Error if unauthorized (401), feature disabled (404), or server error occurs (500)
     */
    public async ticketsExportCsv(): Promise<Blob> {
      const response = await fetch(`${this.apiUrl}/api/tickets/export-csv`, {
        method: 'GET',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 404) {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Funkcja eksportu CSV jest wyłączona');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas eksportu CSV');
        }

        throw new Error(`Error exporting CSV: ${response.statusText}`);
      }

      // Return the response as a Blob for file download
      const blob = await response.blob();
      return blob;
    }

    /**
     * Delete all tickets for the authenticated user
     * DELETE /api/tickets/all
     * @returns Promise<TicketsDeleteAllResponse> - Success message and deleted count
     * @throws Error if unauthorized (401) or server error occurs (500)
     */
    public async ticketsDeleteAll(): Promise<TicketsDeleteAllResponse> {
      const response = await fetch(`${this.apiUrl}/api/tickets/all`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });

      if (!response.ok) {
        // Handle different error scenarios
        if (response.status === 401) {
          throw new Error('Brak autoryzacji. Wymagany token JWT.');
        }

        if (response.status === 500) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Błąd serwera podczas usuwania zestawów');
        }

        throw new Error(`Error deleting all tickets: ${response.statusText}`);
      }

      const result: TicketsDeleteAllResponse = await response.json();
      return result;
    }
}