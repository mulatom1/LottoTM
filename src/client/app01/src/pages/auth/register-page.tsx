import { Link } from 'react-router';
import { useMemo } from 'react';
import RegisterForm from '../../components/auth/register-form';

function AppUserRegister() {
  // Generate random lotto numbers for background
  const backgroundNumbers = useMemo(() => {
    const colors = ['text-gray-500', 'text-blue-600', 'text-yellow-600'];
    return Array.from({ length: 50 }, (_, i) => ({
      id: i,
      number: Math.floor(Math.random() * 49) + 1,
      x: Math.random() * 100,
      y: Math.random() * 100,
      size: Math.random() * 2.5 + 1.5,
      opacity: Math.random() * 0.15 + 0.05,
      duration: Math.random() * 20 + 15,
      delay: Math.random() * 5,
      color: colors[Math.floor(Math.random() * colors.length)],
    }));
  }, []);

  return (
    <main className="flex items-center justify-center min-h-screen bg-gradient-to-br from-gray-100 via-blue-50 to-yellow-50 px-4 py-8 relative overflow-hidden">
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

      <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full relative z-10">
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