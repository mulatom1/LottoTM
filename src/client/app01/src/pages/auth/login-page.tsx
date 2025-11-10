import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router';
import { useAppContext } from '../../context/app-context';
import LoginForm from '../../components/auth/login-form';
import ErrorModal from '../../components/shared/error-modal';
import { validateEmail } from '../../utils/validation';

function LoginPage() {
  // Local state
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [emailError, setEmailError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isErrorModalOpen, setIsErrorModalOpen] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  // Context and navigation
  const { login, isLoggedIn, getApiService } = useAppContext();
  const navigate = useNavigate();
  const location = useLocation();
  const apiService = getApiService();

  // Get intended destination from location state (set by ProtectedRoute)
  const from = (location.state as any)?.from || '/tickets';

  // Redirect if already logged in
  useEffect(() => {
    if (isLoggedIn) {
      console.log('[LoginPage] Already logged in, redirecting to:', from);
      navigate(from, { replace: true });
    }
  }, [isLoggedIn, navigate, from]);

  // Handle email change with inline validation
  const handleEmailChange = (value: string) => {
    setEmail(value);

    // Clear error when user starts typing
    if (emailError && value) {
      setEmailError('');
    }
  };

  // Handle password change
  const handlePasswordChange = (value: string) => {
    setPassword(value);
  };

  // Handle form submit
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Pre-validation: check email format
    if (!validateEmail(email)) {
      setEmailError('Nieprawidłowy format email');
      return;
    }

    // Clear any previous errors
    setEmailError('');
    setIsSubmitting(true);

    try {
      // API call to login endpoint
      const response = await apiService?.authLogin({
        email,
        password,
      });

      if (!response) {
        throw new Error('Brak odpowiedzi z serwera');
      }

      // Success - login to AppContext
      login({
        id: response.userId,
        email: response.email,
        isAdmin: response.isAdmin,
        token: response.token,
        expiresAt: response.expiresAt,
      });

      // Redirect to intended destination or default to tickets
      console.log('[LoginPage] Login successful, redirecting to:', from);
      navigate(from, { replace: true });

    } catch (error) {
      // Handle errors
      if (error instanceof Error) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage('Wystąpił nieoczekiwany problem. Spróbuj ponownie.');
      }
      setIsErrorModalOpen(true);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <LoginForm
        email={email}
        password={password}
        emailError={emailError}
        isSubmitting={isSubmitting}
        onEmailChange={handleEmailChange}
        onPasswordChange={handlePasswordChange}
        onSubmit={handleSubmit}
      />

      <ErrorModal
        isOpen={isErrorModalOpen}
        onClose={() => setIsErrorModalOpen(false)}
        errors={errorMessage}
      />
    </main>
  );
}

export default LoginPage;
