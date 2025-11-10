import React, { useState, useMemo, useEffect } from 'react';
import { AppContext } from './app-context';
import type { User } from '../services/contracts/user';
import { ApiService }  from '../services/api-service';

type AppContextProviderProps = {
  children: React.ReactNode;
};

const STORAGE_KEY_USER = 'lottotm_user';
const STORAGE_KEY_TOKEN = 'lottotm_token';

export const AppContextProvider = ({ children }: AppContextProviderProps) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  const apiService = useMemo(() => {
    const apiUrl = import.meta.env.VITE_API_URL || '';
    const appToken = import.meta.env.VITE_APP_TOKEN || '';
    return new ApiService(apiUrl, appToken);
  }, []);

  // Auto-restore user from localStorage on mount
  useEffect(() => {
    const storedToken = localStorage.getItem(STORAGE_KEY_TOKEN);
    const storedUserJson = localStorage.getItem(STORAGE_KEY_USER);

    if (storedToken && storedUserJson) {
      try {
        const storedUser: User = JSON.parse(storedUserJson);

        // Check if token is expired
        const expiresAt = new Date(storedUser.expiresAt);
        const now = new Date();

        if (expiresAt > now) {
          // Token still valid - restore session
          setUser(storedUser);
          setIsLoggedIn(true);
          apiService.setUsrToken(storedUser.token);
        } else {
          // Token expired - clear storage
          localStorage.removeItem(STORAGE_KEY_TOKEN);
          localStorage.removeItem(STORAGE_KEY_USER);
        }
      } catch (error) {
        // Invalid JSON - clear storage
        console.error('Failed to parse stored user data:', error);
        localStorage.removeItem(STORAGE_KEY_TOKEN);
        localStorage.removeItem(STORAGE_KEY_USER);
      }
    }
  }, [apiService]);

  const login = (user: User) => {
    setUser(user);
    setIsLoggedIn(true);
    apiService.setUsrToken(user.token);

    // Persist to localStorage
    localStorage.setItem(STORAGE_KEY_TOKEN, user.token);
    localStorage.setItem(STORAGE_KEY_USER, JSON.stringify(user));
  };

  const logout = () => {
    setUser(null);
    setIsLoggedIn(false);
    apiService.setUsrToken('');

    // Clear localStorage
    localStorage.removeItem(STORAGE_KEY_TOKEN);
    localStorage.removeItem(STORAGE_KEY_USER);
  };

  const getApiService = (): ApiService => {
    return apiService;
  };

  return (
    <AppContext.Provider value={{ user, isLoggedIn, login, logout, getApiService }}>
      {children}
    </AppContext.Provider>
  );
};