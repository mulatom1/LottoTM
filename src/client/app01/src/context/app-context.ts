import { createContext, useContext } from 'react';
import type { ApiService } from '../services/api-service';
import type { User } from '../services/contracts/user';

export type AppContextType = {
  user?: User | null;
  isLoggedIn: boolean;
  login: (user: User) => void;
  logout: () => void;
  getApiService: () => ApiService | undefined;
};

export const AppContext = createContext<AppContextType | undefined>(undefined);

export const useAppContext = () => {
  const context = useContext(AppContext);
  if (context === undefined) throw new Error('useAppContext must be used within a AppContextProvider');
  return context;
};