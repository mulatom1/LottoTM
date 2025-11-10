import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from "react-router";
import './index.css'
import { AppContextProvider } from './context/app-context-provider';
import ProtectedRoute from './components/auth/protected-route';
import Navbar from './components/layout/navbar';
import HomePage from './pages/home/home-page';
import LoginPage from './pages/auth/login-page';
import RegisterPage from './pages/auth/register-page';
import DrawsPage from './pages/draws/draws-page';
import TicketsPage from './pages/tikets/tikets-page';
import ChecksPage from './pages/checks/checks-page';

createRoot(document.getElementById('root')!).render(
  <AppContextProvider>
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route index element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/draws"
          element={
            <ProtectedRoute>
              <DrawsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tickets"
          element={
            <ProtectedRoute>
              <TicketsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/checks"
          element={
            <ProtectedRoute>
              <ChecksPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  </AppContextProvider>,
)
