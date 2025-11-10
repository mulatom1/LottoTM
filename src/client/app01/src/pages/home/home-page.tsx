import { useNavigate } from "react-router";
import { useAppContext } from "../../context/app-context";
import Button from "../../components/shared/button";

function HomePage() {
  const { user, isLoggedIn } = useAppContext();
  const navigate = useNavigate();

  return (
    <main className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full text-center space-y-8">
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
