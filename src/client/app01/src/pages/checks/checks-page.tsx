// ===================================================================
// ChecksPage - Main page component for verification of winning tickets
// ===================================================================

import { useState } from 'react';
import { useAppContext } from '../../context/app-context';
import { CheckPanel } from '../../components/checks/check-panel';
import { CheckResults } from '../../components/checks/check-results';
import Spinner from '../../components/shared/spinner';
import ErrorModal from '../../components/shared/error-modal';
import { getDefaultDateFrom, getDefaultDateTo } from '../../utils/date-helpers';
import type { TicketVerificationResult } from '../../services/contracts/verification-check-response';

/**
 * Checks Page - Main page for verifying user's lottery tickets against draw results
 * Features:
 * - Date range selection (default: last 31 days)
 * - Automatic verification via API
 * - Results displayed in accordion format grouped by draws
 * - Highlighted matched numbers
 * - Win badges for 3+ matches
 */
export default function ChecksPage() {
  const { getApiService } = useAppContext();
  const apiService = getApiService();

  // State management
  const [dateFrom, setDateFrom] = useState<string>(getDefaultDateFrom());
  const [dateTo, setDateTo] = useState<string>(getDefaultDateTo());
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [results, setResults] = useState<TicketVerificationResult[] | null>(null);
  const [totalTickets, setTotalTickets] = useState<number>(0);
  const [totalDraws, setTotalDraws] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);

  /**
   * Handle verification submission
   * Calls API and updates results state
   */
  const handleSubmit = async (dateFromValue: string, dateToValue: string) => {
    if (!apiService) {
      setError('Błąd inicjalizacji API. Spróbuj odświeżyć stronę.');
      return;
    }

    setIsLoading(true);
    setError(null);
    setResults(null);

    try {
      const response = await apiService.verificationCheck({
        dateFrom: dateFromValue,
        dateTo: dateToValue
      });

      setResults(response.results);
      setTotalTickets(response.totalTickets);
      setTotalDraws(response.totalDraws);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Wystąpił nieoczekiwany błąd';

      // Handle specific error cases
      if (errorMessage.includes('Brak autoryzacji') || errorMessage.includes('token')) {
        setError('Twoja sesja wygasła. Zaloguj się ponownie.');
      } else if (errorMessage.includes('dateFrom') || errorMessage.includes('dateTo')) {
        setError(errorMessage);
      } else if (errorMessage.includes('serwera')) {
        setError('Wystąpił problem z serwerem. Spróbuj ponownie za chwilę.');
      } else if (!errorMessage) {
        setError('Brak połączenia z serwerem. Sprawdź swoje połączenie internetowe.');
      } else {
        setError(errorMessage);
      }
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Close error modal
   */
  const handleCloseError = () => {
    setError(null);
  };

  return (
    <main className="min-h-screen py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-5xl mx-auto">
        {/* Page Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            Sprawdź swoje wygrane
          </h1>
          <p className="text-gray-600">
            Weryfikuj swoje zestawy liczb względem wyników losowań w wybranym zakresie dat
          </p>
        </div>

        {/* Check Panel - Date Range Form */}
        <div className="mb-8">
          <CheckPanel
            dateFrom={dateFrom}
            dateTo={dateTo}
            onDateFromChange={setDateFrom}
            onDateToChange={setDateTo}
            onSubmit={handleSubmit}
            isLoading={isLoading}
          />
        </div>

        {/* Loading Spinner */}
        {isLoading && (
          <div className="flex flex-col items-center justify-center py-16">
            <Spinner size="lg" className="mb-4" />
            <p className="text-gray-600 text-center">
              Sprawdzanie wygranych...
              <br />
              <span className="text-sm text-gray-500">
                To może potrwać kilka sekund
              </span>
            </p>
          </div>
        )}

        {/* Results Display */}
        {!isLoading && results && (
          <div className="animate-fadeIn">
            <CheckResults
              results={results}
              totalTickets={totalTickets}
              totalDraws={totalDraws}
            />
          </div>
        )}

        {/* Error Modal */}
        <ErrorModal
          isOpen={!!error}
          onClose={handleCloseError}
          errors={error || ''}
        />
      </div>
    </main>
  );
}
