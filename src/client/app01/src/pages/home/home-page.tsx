import { useNavigate } from "react-router";
import { useAppContext } from "../../context/app-context";
import Button from "../../components/shared/button";
import { useMemo } from "react";

function HomePage() {
  const { user, isLoggedIn } = useAppContext();
  const navigate = useNavigate();

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

      {/* Main content */}
      <div className="max-w-md w-full text-center space-y-8 relative z-10">
        {/* Header section */}
        <div className="space-y-4">
          <h1 className="text-3xl sm:text-4xl font-bold text-gray-900">
            LottoTM - System Zarządzania Kuponami LOTTO
          </h1>

          {/* Description section */}
          <p className="text-lg text-gray-600">
            Zarządzaj swoimi zestawami liczb LOTTO i automatycznie weryfikuj wygrane.
            Szybko, wygodnie, bezpiecznie.
          </p>
        </div>

        {/* Conditional rendering based on login state */}
        {!isLoggedIn ? (
          /* CTA Buttons for guest users */
          <div className="space-y-4 pt-8">
            <Button
              variant="primary"
              onClick={() => navigate('/login')}
              className="w-full"
            >
              Zaloguj się
            </Button>

            <Button
              variant="secondary"
              onClick={() => navigate('/register')}
              className="w-full"
            >
              Zarejestruj się
            </Button>
          </div>
        ) : (
          /* Navigation buttons for logged in users */
          <div className="space-y-4 pt-8">
            <p className="text-lg text-gray-700">
              Witaj, <span className="font-bold">{user?.email}</span>!
            </p>

            <div className="space-y-3">
              <Button
                variant="primary"
                onClick={() => navigate('/draws')}
                className="w-full"
              >
                Historia losowań
              </Button>

              <Button
                variant="primary"
                onClick={() => navigate('/tickets')}
                className="w-full"
              >
                Moje zestawy
              </Button>
            </div>
          </div>
        )}
      </div>
    </main>
  );
}

export default HomePage;
