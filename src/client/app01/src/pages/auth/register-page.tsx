import { Link } from 'react-router';
import RegisterForm from '../../components/auth/register-form';

function AppUserRegister() {
  return (
    <main className="flex items-center justify-center min-h-screen bg-gray-50 px-4 py-8">
      <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full">
        {/* Nagłówek */}
        <h2 className="text-2xl font-bold text-gray-900 mb-6 text-center">
          Zarejestruj się
        </h2>

        {/* Formularz rejestracji */}
        <RegisterForm />

        {/* Link do logowania */}
        <p className="mt-6 text-center text-sm text-gray-600">
          Masz już konto?{' '}
          <Link
            to="/login"
            className="font-medium text-blue-600 hover:text-blue-500 transition-colors"
          >
            Zaloguj się
          </Link>
        </p>
      </div>
    </main>
  );
}

export default AppUserRegister