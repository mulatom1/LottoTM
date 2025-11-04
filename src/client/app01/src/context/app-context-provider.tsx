import React, { useState, useMemo } from 'react';
import { AppContext } from './app-context';
import type { User } from '../services/contracts/user';
import { ApiService }  from '../services/api-service';

type AppContextProviderProps = {
  children: React.ReactNode;  
};


export const AppContextProvider = ({ children }: AppContextProviderProps) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  const login = (user: User) => {
    setUser(user);
    setIsLoggedIn(true);
    apiService.setUsrToken(user.token);
  };

  const logout = () => {
    setUser(null);
    setIsLoggedIn(false);
    apiService.setUsrToken('');
  };

  const apiService = useMemo(() => {
    const apiUrl = import.meta.env.VITE_API_URL || '';
    const appToken = import.meta.env.VITE_APP_TOKEN || '';
    return new ApiService(apiUrl, appToken);
  }, []);

  const getApiService = (): ApiService => {
    return apiService;
  };

  return (
    <AppContext.Provider value={{ user, isLoggedIn, login, logout, getApiService }}>
      {children}
    </AppContext.Provider>
  );
};