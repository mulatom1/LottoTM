import React from 'react';
import { Navigate, useLocation } from 'react-router';
import { useAppContext } from '../../context/app-context';

export interface ProtectedRouteProps {
  children: React.ReactNode;
}

/**
 * Protected route wrapper component
 * Redirects to login page if user is not authenticated
 * Preserves intended destination in location state
 */
const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isLoggedIn } = useAppContext();
  const location = useLocation();

  if (!isLoggedIn) {
    // Redirect to login page with intended destination
    console.log('[ProtectedRoute] User not logged in, redirecting to /login from:', location.pathname);
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  // User is authenticated, render children
  return <>{children}</>;
};

export default ProtectedRoute;
