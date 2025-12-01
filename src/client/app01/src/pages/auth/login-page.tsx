import React, { useState, useEffect, useMemo } from 'react';
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

  // Generate random lotto numbers for background
  const backgroundNumbers = useMemo(() => {
    const colors = ['text-gray-500', 'text-blue-600', 'text-yellow-600'];
    return Array.from({ length: 50 }, (_, i) => ({
      id: i,
      number: Math.floor(Math.random() * 49) + 1,
      x: Math.random() * 100,
      y: Math.random() * 100,
      size: Math.random() * 2.5 + 1.5,
      opacity: Math.random() * 0.25 + 0.15,
      duration: Math.random() * 20 + 15,
      delay: Math.random() * 5,
      color: colors[Math.floor(Math.random() * colors.length)],
    }));
  }, []);

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
    <main className="min-h-screen flex items-center justify-center bg-gradient-to-br from-gray-100 via-blue-50 to-yellow-50 px-4 relative overflow-hidden">
      {/* Animated background numbers */}
      <div className="absolute inset-0 pointer-events-none">
        {backgroundNumbers.map((item) => (
          <div
            key={item.id}
            className={`absolute ${item.color} font-bold animate-float`}
            style={{
              left: `${item.x}%`,
              top: `${item.y}%`,
              fontSize: `${item.size}rem`,
              opacity: item.opacity,
              animation: `float ${item.duration}s infinite ease-in-out`,
              animationDelay: `${item.delay}s`,
            }}
          >
            {item.number}
          </div>
        ))}
      </div>

      <div className="relative z-10 w-full max-w-md">
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
      </div>
    </main>
  );
}

export default LoginPage;
