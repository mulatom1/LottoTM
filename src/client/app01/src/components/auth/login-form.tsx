import React from 'react';
import { Link } from 'react-router';
import EmailInput from '../shared/email-input';
import PasswordInput from '../shared/password-input';
import Button from '../shared/button';

export interface LoginFormProps {
  email: string;
  password: string;
  emailError: string;
  isSubmitting: boolean;
  onEmailChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
  onSubmit: (e: React.FormEvent) => void;
}

const LoginForm: React.FC<LoginFormProps> = ({
  email,
  password,
  emailError,
  isSubmitting,
  onEmailChange,
  onPasswordChange,
  onSubmit,
}) => {
  return (
    <form
      onSubmit={onSubmit}
      className="max-w-md w-full mx-auto p-6 bg-white rounded-lg shadow-md"
    >
      <h2 className="text-2xl font-bold mb-6 text-center text-gray-900">
        Zaloguj się
      </h2>

      <EmailInput
        label="Email"
        value={email}
        error={emailError}
        onChange={onEmailChange}
        placeholder="twoj@email.com"
        autoFocus
        required
      />

      <PasswordInput
        label="Hasło"
        value={password}
        onChange={onPasswordChange}
        placeholder="••••••••"
        required
      />

      <Button
        type="submit"
        variant="primary"
        onClick={() => {}} // Submit handled by form onSubmit
        disabled={isSubmitting}
        className="w-full mt-4"
      >
        {isSubmitting ? 'Logowanie...' : 'Zaloguj się'}
      </Button>

      <p className="mt-4 text-center text-sm text-gray-600">
        Nie masz konta?{' '}
        <Link
          to="/register"
          className="text-blue-600 hover:text-blue-800 hover:underline font-medium"
        >
          Zarejestruj się
        </Link>
      </p>
    </form>
  );
};

export default LoginForm;
