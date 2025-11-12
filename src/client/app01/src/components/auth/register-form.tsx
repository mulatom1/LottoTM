import { useState } from 'react';
import { useNavigate } from 'react-router';
import InputField from '../shared/input-field';
import ErrorModal from '../shared/error-modal';
import Button from '../shared/button';
import { useAppContext } from '../../context/app-context';
import type { AuthRegisterRequest } from '../../services/contracts/auth-register-request';

interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
}

interface RegisterFormErrors {
  email?: string;
  password?: string;
  confirmPassword?: string;
}

function RegisterForm() {
  const navigate = useNavigate();
  const { getApiService, login } = useAppContext();
  const apiService = getApiService();

  // Stan formularza
  const [formData, setFormData] = useState<RegisterFormData>({
    email: '',
    password: '',
    confirmPassword: ''
  });

  // Błędy walidacji inline
  const [errors, setErrors] = useState<RegisterFormErrors>({});

  // Stan submitu
  const [isSubmitting, setIsSubmitting] = useState(false);

  // ErrorModal
  const [isErrorModalOpen, setIsErrorModalOpen] = useState(false);
  const [apiErrors, setApiErrors] = useState<string[]>([]);

  // Walidacja email
  const validateEmail = (email: string): string | undefined => {
    if (!email.trim()) {
      return 'Email jest wymagany';
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      return 'Nieprawidłowy format email';
    }

    return undefined;
  };

  // Walidacja hasła
  const validatePassword = (password: string): string | undefined => {
    if (!password) {
      return 'Hasło jest wymagane';
    }

    if (password.length < 8) {
      return 'Hasło musi mieć min. 8 znaków';
    }

    if (!/[A-Z]/.test(password)) {
      return 'Hasło musi zawierać wielką literę';
    }

    if (!/[0-9]/.test(password)) {
      return 'Hasło musi zawierać cyfrę';
    }

    if (!/[\W_]/.test(password)) {
      return 'Hasło musi zawierać znak specjalny';
    }

    return undefined;
  };

  // Walidacja potwierdzenia hasła
  const validateConfirmPassword = (confirmPassword: string, password: string): string | undefined => {
    if (!confirmPassword) {
      return 'Potwierdzenie hasła jest wymagane';
    }

    if (confirmPassword !== password) {
      return 'Hasła nie są identyczne';
    }

    return undefined;
  };

  // Handle change dla email
  const handleEmailChange = (value: string) => {
    setFormData(prev => ({ ...prev, email: value }));
    const error = validateEmail(value);
    setErrors(prev => ({ ...prev, email: error }));
  };

  // Handle change dla hasła
  const handlePasswordChange = (value: string) => {
    setFormData(prev => ({ ...prev, password: value }));
    const error = validatePassword(value);
    setErrors(prev => ({ ...prev, password: error }));

    // Rewalidacja confirm password jeśli już wypełnione
    if (formData.confirmPassword) {
      const confirmError = validateConfirmPassword(formData.confirmPassword, value);
      setErrors(prev => ({ ...prev, confirmPassword: confirmError }));
    }
  };

  // Handle change dla potwierdzenia hasła
  const handleConfirmPasswordChange = (value: string) => {
    setFormData(prev => ({ ...prev, confirmPassword: value }));
    const error = validateConfirmPassword(value, formData.password);
    setErrors(prev => ({ ...prev, confirmPassword: error }));
  };

  // Walidacja wszystkich pól
  const validateAllFields = (): boolean => {
    const emailError = validateEmail(formData.email);
    const passwordError = validatePassword(formData.password);
    const confirmPasswordError = validateConfirmPassword(formData.confirmPassword, formData.password);

    setErrors({
      email: emailError,
      password: passwordError,
      confirmPassword: confirmPasswordError
    });

    return !emailError && !passwordError && !confirmPasswordError;
  };

  // Handle submit
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Walidacja wszystkich pól
    if (!validateAllFields()) {
      return;
    }

    if (!apiService) {
      setApiErrors(['Błąd połączenia z serwerem']);
      setIsErrorModalOpen(true);
      return;
    }

    setIsSubmitting(true);

    try {
      const request: AuthRegisterRequest = {
        email: formData.email,
        password: formData.password,
        confirmPassword: formData.confirmPassword
      };

      const response = await apiService.authRegister(request);

      // Automatyczne logowanie - zapisanie tokenu i danych użytkownika
      login({
        id: response.userId,
        email: response.email,
        isAdmin: response.isAdmin,
        token: response.token,
        expiresAt: response.expiresAt,
      });

      // Sukces: redirect do tickets (użytkownik już zalogowany)
      navigate('/tickets');
    } catch (error) {
      // Obsługa błędów
      if (error instanceof Error) {
        // Parsowanie błędów z API
        const errorMessage = error.message;

        // Sprawdzenie czy to błędy walidacji (format: "field: message; field: message")
        if (errorMessage.includes(':')) {
          const errorMessages = errorMessage.split(';').map(e => e.trim());
          setApiErrors(errorMessages);
        } else {
          setApiErrors([errorMessage]);
        }
      } else {
        setApiErrors(['Wystąpił nieoczekiwany błąd. Spróbuj ponownie.']);
      }

      setIsErrorModalOpen(true);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Email */}
        <InputField
          label="Email"
          type="email"
          value={formData.email}
          onChange={handleEmailChange}
          error={errors.email}
          placeholder="twoj@email.com"
          required
          autoFocus
        />

        {/* Hasło */}
        <InputField
          label="Hasło"
          type="password"
          value={formData.password}
          onChange={handlePasswordChange}
          error={errors.password}
          placeholder="••••••••"
          required
        />

        {/* Hint text - wymagania hasła */}
        <p className="text-sm text-gray-600 mt-1 -mt-3">
          Min. 8 znaków, 1 wielka litera, 1 cyfra, 1 znak specjalny
        </p>

        {/* Potwierdzenie hasła */}
        <InputField
          label="Potwierdź hasło"
          type="password"
          value={formData.confirmPassword}
          onChange={handleConfirmPasswordChange}
          error={errors.confirmPassword}
          placeholder="••••••••"
          required
        />

        {/* Przycisk submit */}
        <div className="mt-6">
          <Button
            type="submit"
            variant="primary"
            onClick={() => {}} // Submit handled by form onSubmit
            disabled={isSubmitting}
            className="w-full"
          >
            {isSubmitting ? 'Rejestrowanie...' : 'Zarejestruj się'}
          </Button>
        </div>
      </form>

      {/* ErrorModal */}
      <ErrorModal
        isOpen={isErrorModalOpen}
        onClose={() => setIsErrorModalOpen(false)}
        errors={apiErrors}
      />
    </>
  );
}

export default RegisterForm;
