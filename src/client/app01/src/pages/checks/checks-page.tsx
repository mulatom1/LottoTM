// ===================================================================
// ChecksPage - Main page component for verification of winning tickets
// ===================================================================

import { useState, useMemo } from 'react';
import { useAppContext } from '../../context/app-context';
import { CheckPanel } from '../../components/checks/check-panel';
import { CheckSummary } from '../../components/checks/check-summary';
import { CheckResults } from '../../components/checks/check-results';
import Spinner from '../../components/shared/spinner';
import ErrorModal from '../../components/shared/error-modal';
import { getDefaultDateFrom, getDefaultDateTo } from '../../utils/date-helpers';
import type { DrawsResult, TicketsResult } from '../../services/contracts/verification-check-response';

/**
 * Checks Page - Main page for verifying user's lottery tickets against draw results
 * Features:
 * - Date range selection (default: last week, max: 3 years)
 * - Optional group name filter (partial match, case-insensitive)
 * - Automatic verification via API
 * - Results displayed in accordion format grouped by draws
 * - Highlighted matched numbers
 * - Win badges for 3+ matches
 */
export default function ChecksPage() {
  const { getApiService } = useAppContext();
  const apiService = getApiService();

  // Generate random lotto numbers for background
  const backgroundNumbers = useMemo(() => {
    const colors = ['text-gray-500', 'text-blue-600', 'text-yellow-600'];
    return Array.from({ length: 50 }, (_, i) => ({
      id: i,
      number: Math.floor(Math.random() * 49) + 1,
      x: Math.random() * 100,
      y: Math.random() * 100,
      size: Math.random() * 2.5 + 1.5,
      opacity: Math.random() * 0.15 + 0.05,
      duration: Math.random() * 20 + 15,
      delay: Math.random() * 5,
      color: colors[Math.floor(Math.random() * colors.length)],
    }));
  }, []);

  // State management
  const [dateFrom, setDateFrom] = useState<string>(getDefaultDateFrom());
  const [dateTo, setDateTo] = useState<string>(getDefaultDateTo());
  const [groupName, setGroupName] = useState<string>('');
  const [showNonWinningTickets, setShowNonWinningTickets] = useState<boolean>(true);
  const [show3Hits, setShow3Hits] = useState<boolean>(true);
  const [show4Hits, setShow4Hits] = useState<boolean>(true);
  const [show5Hits, setShow5Hits] = useState<boolean>(true);
  const [show6Hits, setShow6Hits] = useState<boolean>(true);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [drawsResults, setDrawsResults] = useState<DrawsResult[] | null>(null);
  const [ticketsResults, setTicketsResults] = useState<TicketsResult[] | null>(null);
  const [executionTimeMs, setExecutionTimeMs] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);

  /**
   * Handle verification submission
   * Calls API and updates results state
   */
  const handleSubmit = async (dateFromValue: string, dateToValue: string, groupNameValue: string) => {
    if (!apiService) {
      setError('Błąd inicjalizacji API. Spróbuj odświeżyć stronę.');
      return;
    }

    setIsLoading(true);
    setError(null);
    setDrawsResults(null);
    setTicketsResults(null);

    try {
      const response = await apiService.verificationCheck({
        dateFrom: dateFromValue,
        dateTo: dateToValue,
        groupName: groupNameValue && groupNameValue.trim() !== '' ? groupNameValue : null
      });

      setDrawsResults(response.drawsResults);
      setTicketsResults(response.ticketsResults);
      setExecutionTimeMs(response.executionTimeMs);
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
    <main className="min-h-screen bg-gradient-to-br from-gray-100 via-blue-50 to-yellow-50 py-8 px-4 sm:px-6 lg:px-8 relative overflow-hidden">
      {/* Animated background numbers */}
      <div className="absolute inset-0 pointer-events-none">
        {backgroundNumbers.map((item) => (
          <div
            key={item.id}
            className={`absolute ${item.color} font-bold animate-float`}
            style={{
              left: `${item.x}%`,
              top: `${item.y}%`,
              fontSize: `${item.size}rem`,
              opacity: item.opacity,
              animation: `float ${item.duration}s infinite ease-in-out`,
              animationDelay: `${item.delay}s`,
            }}
          >
            {item.number}
          </div>
        ))}
      </div>

      <div className="max-w-5xl mx-auto relative z-10">
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
            groupName={groupName}
            onDateFromChange={setDateFrom}
            onDateToChange={setDateTo}
            onGroupNameChange={setGroupName}
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
        {!isLoading && drawsResults && ticketsResults && (
          <div className="animate-fadeIn space-y-6">
            {/* Summary Statistics */}
            <CheckSummary
              drawsResults={drawsResults}
              ticketsResults={ticketsResults}
            />

            {/* Detailed Results */}
            <CheckResults
              drawsResults={drawsResults}
              ticketsResults={ticketsResults}
              executionTimeMs={executionTimeMs}
              showNonWinningTickets={showNonWinningTickets}
              onShowNonWinningTicketsChange={setShowNonWinningTickets}
              show3Hits={show3Hits}
              onShow3HitsChange={setShow3Hits}
              show4Hits={show4Hits}
              onShow4HitsChange={setShow4Hits}
              show5Hits={show5Hits}
              onShow5HitsChange={setShow5Hits}
              show6Hits={show6Hits}
              onShow6HitsChange={setShow6Hits}
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
