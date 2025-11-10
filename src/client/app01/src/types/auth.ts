// Request do POST /api/auth/register
export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

// Response z POST /api/auth/register (sukces)
export interface RegisterResponse {
  message: string;
}

// Response z POST /api/auth/register (błąd walidacji)
export interface RegisterErrorResponse {
  errors: {
    email?: string[];
    password?: string[];
    confirmPassword?: string[];
  };
}

// Lokalny stan formularza
export interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
}

// Błędy walidacji inline
export interface RegisterFormErrors {
  email?: string;
  password?: string;
  confirmPassword?: string;
}
