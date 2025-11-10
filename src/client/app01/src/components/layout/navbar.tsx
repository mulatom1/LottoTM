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
    <nav className="bg-white shadow-sm border-b border-gray-200">
      <div className="container mx-auto px-4 py-3">
        <div className="flex items-center justify-between">
          {/* Logo/Brand */}
          <button
            onClick={() => navigate('/')}
            className="text-xl font-bold text-gray-900 hover:text-blue-600 transition-colors"
          >
            LottoTM
          </button>

          {/* Navigation Links */}
          <div className="flex items-center gap-4">
            <button
              onClick={() => navigate('/draws')}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                isActive('/draws')
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              Historia losowań
            </button>

            <button
              onClick={() => navigate('/tickets')}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                isActive('/tickets')
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              Moje zestawy
            </button>

            <button
              onClick={() => navigate('/checks')}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                isActive('/checks')
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              Sprawdź wygrane
            </button>

            {/* User menu */}
            <div className="flex items-center gap-3 ml-4 pl-4 border-l border-gray-300">
              <span className="text-sm text-gray-600">
                {user?.email}
              </span>
              <Button
                variant="secondary"
                onClick={handleLogout}
                className="text-sm px-3 py-1"
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
