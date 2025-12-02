import { useNavigate, useLocation } from 'react-router';
import { useAppContext } from '../../context/app-context';
import Button from '../shared/button';

/**
 * Navigation bar component
 * Shows navigation links for logged in users
 */
function Navbar() {
  const { user, isLoggedIn, logout } = useAppContext();
  const navigate = useNavigate();
  const location = useLocation();

  if (!isLoggedIn) {
    return null; // Don't show navbar for guests
  }

  const isActive = (path: string) => location.pathname === path;

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="bg-gradient-to-r from-blue-400 to-blue-950 shadow-lg">
      <div className="container mx-auto px-4 py-3">
        <div className="flex items-center justify-between">
          {/* Logo/Brand */}
          <button
            onClick={() => navigate('/')}
            className="text-xl font-bold text-white hover:text-blue-100 transition-colors"
          >
            LottoTM
          </button>

          {/* Navigation Links */}
          <div className="flex items-center gap-4">
            <button
              onClick={() => navigate('/draws')}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                isActive('/draws')
                  ? 'bg-white text-blue-600'
                  : 'text-white hover:bg-blue-700'
              }`}
            >
              Historia losowań
            </button>

            <button
              onClick={() => navigate('/tickets')}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                isActive('/tickets')
                  ? 'bg-white text-blue-600'
                  : 'text-white hover:bg-blue-700'
              }`}
            >
              Moje zestawy
            </button>

            <button
              onClick={() => navigate('/checks')}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                isActive('/checks')
                  ? 'bg-white text-blue-600'
                  : 'text-white hover:bg-blue-700'
              }`}
            >
              Sprawdź wygrane
            </button>

            {/* User menu */}
            <div className="flex items-center gap-3 ml-4 pl-4 border-l border-blue-500">
              <span className="text-sm text-blue-100">
                {user?.email}
              </span>
              <Button
                variant="secondary"
                onClick={handleLogout}
                className="text-sm px-3 py-1 bg-white text-blue-600 hover:bg-blue-50"
              >
                Wyloguj
              </Button>
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
}

export default Navbar;
