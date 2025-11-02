import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from "react-router";
import './index.css'
import { AppContextProvider } from './context/app-context-provider';
import HomePage from './pages/home/home-page';
import LoginPage from './pages/auth/login-page';
import RegisterPage from './pages/auth/register-page';
import WinsPage from './pages/wins/wins-page';
import TicketsPage from './pages/tikets/tikets-page';

createRoot(document.getElementById('root')!).render(
  <AppContextProvider>
    <BrowserRouter>
      <Routes>
        <Route index element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/wins" element={<WinsPage />} />
        <Route path="/tickets" element={<TicketsPage />} />
      </Routes>
    </BrowserRouter>
  </AppContextProvider>,
)
