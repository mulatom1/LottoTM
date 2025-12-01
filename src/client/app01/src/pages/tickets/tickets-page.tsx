import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { useNavigate } from 'react-router';
import { useAppContext } from '../../context/app-context';
import { Button, ErrorModal, Toast } from '../../components/shared';
import {
  TicketCounter,
  TicketList,
  TicketFormModal,
  GeneratorPreviewModal,
  GeneratorSystemPreviewModal,
  DeleteConfirmModal
} from '../../components/tickets';
import type {
  Ticket,
  TicketFormState,
  GeneratorState
} from '../../types/tickets';
import type { TicketsImportCsvResponse } from '../../services/contracts/tickets-import-csv-response';
import { generateRandomNumbers, generateSystemTickets } from '../../utils/generators';
import { validateNumbers } from '../../utils/validators';

function TicketsPage() {
  const { getApiService, logout, isLoggedIn } = useAppContext();
  const navigate = useNavigate();
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

  // Redirect if no API service available
  useEffect(() => {
    if (!apiService) {
      navigate('/login');
    }
  }, [apiService, navigate]);

  // Data state
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchTerm, setSearchTerm] = useState<string>('');

  // Modal state
  const [isTicketFormOpen, setIsTicketFormOpen] = useState<boolean>(false);
  const [ticketFormState, setTicketFormState] = useState<TicketFormState | null>(null);
  const [isGeneratorPreviewOpen, setIsGeneratorPreviewOpen] = useState<boolean>(false);
  const [generatorState, setGeneratorState] = useState<GeneratorState | null>(null);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState<boolean>(false);
  const [ticketToDelete, setTicketToDelete] = useState<Ticket | null>(null);
  const [isDeleteAllConfirmOpen, setIsDeleteAllConfirmOpen] = useState<boolean>(false);
  const [isImportModalOpen, setIsImportModalOpen] = useState<boolean>(false);
  const [importResult, setImportResult] = useState<TicketsImportCsvResponse | null>(null);
  const [isErrorModalOpen, setIsErrorModalOpen] = useState<boolean>(false);
  const [errors, setErrors] = useState<string[]>([]);

  // File input ref for import
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Toast state
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [toastVariant, setToastVariant] = useState<'success' | 'error' | 'info'>('success');

  /**
   * Fetch all tickets from API with optional filter
   */
  const fetchTickets = useCallback(async (groupName?: string) => {
    if (!apiService) return;

    setLoading(true);
    try {
      const response = await apiService.ticketsGetList(
        groupName ? { groupName } : undefined
      );
      setTickets(response.tickets);
      setTotalCount(response.totalCount);
    } catch (error) {
      handleApiError(error);
    } finally {
      setLoading(false);
    }
  }, [apiService]);

  // Fetch tickets on mount
  useEffect(() => {
    if (isLoggedIn && apiService) {
      fetchTickets();
    }
  }, [isLoggedIn, apiService, fetchTickets]);

  // Warning toast jeśli limit bliski (>95 zestawów)
  useEffect(() => {
    if (totalCount > 95 && totalCount < 100) {
      setToastMessage(`Uwaga: Pozostało tylko ${100 - totalCount} wolnych miejsc`);
      setToastVariant('info');
    }
  }, [totalCount]);

  /**
   * Centralized error handler
   */
  const handleApiError = (error: unknown) => {
    if (error instanceof Error) {
      const errorMessage = error.message;

      // Specjalny przypadek: 401 (wygasły token)
      if (errorMessage.includes('Brak autoryzacji') || errorMessage.includes('401')) {
        logout();
        navigate('/login');
        setErrors(['Twoja sesja wygasła. Zaloguj się ponownie.']);
        setIsErrorModalOpen(true);
        return;
      }

      // Parsowanie błędów z API
      const errorLines = errorMessage.split(';').map(e => e.trim()).filter(e => e);
      setErrors(errorLines.length > 0 ? errorLines : ['Wystąpił nieoczekiwany błąd']);
      setIsErrorModalOpen(true);
    } else {
      setErrors(['Wystąpił nieoczekiwany błąd']);
      setIsErrorModalOpen(true);
    }
  };

  /**
   * Handler: Otwórz modal dodawania zestawu
   */
  const handleAddTicket = () => {
    // Sprawdzenie limitu
    if (totalCount >= 100) {
      setErrors(['Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.']);
      setIsErrorModalOpen(true);
      return;
    }

    setTicketFormState({ mode: 'add' });
    setIsTicketFormOpen(true);
  };

  /**
   * Handler: Otwórz modal edycji zestawu
   */
  const handleEditTicket = (ticketId: number) => {
    const ticket = tickets.find(t => t.id === ticketId);
    if (!ticket) return;

    setTicketFormState({
      mode: 'edit',
      initialNumbers: ticket.numbers,
      initialGroupName: ticket.groupName,
      ticketId: ticket.id
    });
    setIsTicketFormOpen(true);
  };

  /**
   * Handler: Otwórz modal usunięcia zestawu
   */
  const handleDeleteTicket = (ticketId: number) => {
    const ticket = tickets.find(t => t.id === ticketId);
    if (!ticket) return;

    setTicketToDelete(ticket);
    setIsDeleteConfirmOpen(true);
  };

  /**
   * Handler: Wyszukiwanie po nazwie grupy
   */
  const handleSearch = useCallback((term: string) => {
    setSearchTerm(term);
    fetchTickets(term || undefined);
  }, [fetchTickets]);

  /**
   * Handler: Czyszczenie filtra wyszukiwania
   */
  const handleClearSearch = useCallback(() => {
    setSearchTerm('');
    fetchTickets();
  }, [fetchTickets]);

  /**
   * Handler: Zapisz zestaw (dodawanie lub edycja)
   */
  const handleSaveTicket = async (numbers: number[], groupName: string, ticketId?: number) => {
    if (!apiService) return;

    // Frontend validation
    const validationErrors = validateNumbers(numbers);
    if (validationErrors.length > 0) {
      setErrors(validationErrors);
      setIsErrorModalOpen(true);
      return;
    }

    try {
      if (ticketId) {
        // Edycja
        await apiService.ticketsUpdate(ticketId, { groupName, numbers });
        setToastMessage('Zestaw zaktualizowany pomyślnie');
      } else {
        // Dodawanie
        await apiService.ticketsCreate({ groupName, numbers });
        setToastMessage('Zestaw zapisany pomyślnie');
      }

      setToastVariant('success');
      setIsTicketFormOpen(false);
      await fetchTickets(searchTerm || undefined);
    } catch (error) {
      handleApiError(error);
    }
  };

  /**
   * Handler: Potwierdź usunięcie zestawu
   */
  const handleConfirmDelete = async (ticketId: number) => {
    if (!apiService) return;

    try {
      await apiService.ticketsDelete(ticketId);
      setToastMessage('Zestaw usunięty pomyślnie');
      setToastVariant('success');
      setIsDeleteConfirmOpen(false);
      setTicketToDelete(null);
      await fetchTickets(searchTerm || undefined);
    } catch (error) {
      handleApiError(error);
    }
  };

  /**
   * Handler: Generuj losowy zestaw
   */
  const handleGenerateRandom = () => {
    // Sprawdzenie limitu
    if (totalCount >= 100) {
      setErrors(['Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.']);
      setIsErrorModalOpen(true);
      return;
    }

    const numbers = generateRandomNumbers();
    setGeneratorState({ type: 'random', numbers });
    setIsGeneratorPreviewOpen(true);
  };

  /**
   * Handler: Regeneruj losowy zestaw
   */
  const handleRegenerateRandom = () => {
    const numbers = generateRandomNumbers();
    setGeneratorState({ type: 'random', numbers });
  };

  /**
   * Handler: Zapisz wygenerowany losowy zestaw
   */
  const handleSaveGeneratedRandom = async (numbers: number[], groupName: string) => {
    if (!apiService) return;

    // Sprawdzenie limitu ponownie (race condition)
    if (totalCount >= 100) {
      setErrors(['Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.']);
      setIsErrorModalOpen(true);
      setIsGeneratorPreviewOpen(false);
      return;
    }

    try {
      await apiService.ticketsCreate({ numbers, groupName });
      setToastMessage('Zestaw wygenerowany i zapisany');
      setToastVariant('success');
      setIsGeneratorPreviewOpen(false);
      await fetchTickets(searchTerm || undefined);
    } catch (error) {
      handleApiError(error);
    }
  };

  /**
   * Handler: Generuj 9 zestawów systemowych
   */
  const handleGenerateSystem = () => {
    const available = 100 - totalCount;

    // Sprawdzenie miejsca na 9 zestawów
    if (available < 9) {
      setErrors([
        `Brak miejsca na 9 zestawów. Dostępne: ${available} ${available === 1 ? 'zestaw' : 'zestawy'}. Usuń istniejące zestawy, aby kontynuować.`
      ]);
      setIsErrorModalOpen(true);
      return;
    }

    const systemTickets = generateSystemTickets();
    setGeneratorState({ type: 'system', numbers: systemTickets });
    setIsGeneratorPreviewOpen(true);
  };

  /**
   * Handler: Regeneruj 9 zestawów systemowych
   */
  const handleRegenerateSystem = () => {
    const systemTickets = generateSystemTickets();
    setGeneratorState({ type: 'system', numbers: systemTickets });
  };

  /**
   * Handler: Zapisz 9 wygenerowanych zestawów systemowych
   */
  const handleSaveGeneratedSystem = async (systemTickets: number[][], groupName: string) => {
    if (!apiService) return;

    const available = 100 - totalCount;

    // Sprawdzenie miejsca ponownie (race condition)
    if (available < 9) {
      setErrors([
        `Brak miejsca na 9 zestawów. Dostępne: ${available} ${available === 1 ? 'zestaw' : 'zestawy'}. Usuń istniejące zestawy, aby kontynuować.`
      ]);
      setIsErrorModalOpen(true);
      setIsGeneratorPreviewOpen(false);
      return;
    }

    try {
      // Zapisz każdy zestaw osobno z tą samą nazwą grupy
      for (const numbers of systemTickets) {
        await apiService.ticketsCreate({
          numbers,
          groupName: groupName || undefined
        });
      }

      setToastMessage('9 zestawów systemowych zapisanych pomyślnie');
      setToastVariant('success');
      setIsGeneratorPreviewOpen(false);
      await fetchTickets(searchTerm || undefined);
    } catch (error) {
      handleApiError(error);
    }
  };

  /**
   * Handler: Otwórz dialog wyboru pliku CSV do importu
   */
  const handleImportCsv = () => {
    fileInputRef.current?.click();
  };

  /**
   * Handler: Przetwórz wybrany plik CSV
   */
  const handleFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !apiService) return;

    // Walidacja rozszerzenia pliku
    if (!file.name.endsWith('.csv')) {
      setErrors(['Nieprawidłowy format pliku. Dozwolony tylko format CSV.']);
      setIsErrorModalOpen(true);
      return;
    }

    // Walidacja rozmiaru pliku (max 1MB)
    if (file.size > 1048576) {
      setErrors(['Rozmiar pliku przekracza 1MB.']);
      setIsErrorModalOpen(true);
      return;
    }

    setLoading(true);
    try {
      const result = await apiService.ticketsImportCsv(file);
      setImportResult(result);
      setIsImportModalOpen(true);

      // Jeśli zaimportowano jakiekolwiek zestawy, odśwież listę
      if (result.imported > 0) {
        await fetchTickets(searchTerm || undefined);
      }
    } catch (error) {
      handleApiError(error);
    } finally {
      setLoading(false);
      // Reset input to allow selecting the same file again
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  /**
   * Handler: Eksportuj wszystkie zestawy do CSV
   */
  const handleExportCsv = async () => {
    if (!apiService) return;

    // Sprawdź czy są jakiekolwiek zestawy do eksportu
    if (totalCount === 0) {
      setErrors(['Brak zestawów do eksportu.']);
      setIsErrorModalOpen(true);
      return;
    }

    setLoading(true);
    try {
      const blob = await apiService.ticketsExportCsv();

      // Utwórz URL dla blob
      const url = window.URL.createObjectURL(blob);

      // Utwórz link do pobrania
      const a = document.createElement('a');
      a.href = url;
      a.download = `tickets_${new Date().toISOString().split('T')[0]}.csv`;
      document.body.appendChild(a);
      a.click();

      // Cleanup
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);

      setToastMessage('Eksport CSV zakończony pomyślnie');
      setToastVariant('success');
    } catch (error) {
      handleApiError(error);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Handler: Otwórz modal potwierdzenia usunięcia wszystkich zestawów
   */
  const handleDeleteAllTickets = () => {
    if (totalCount === 0) {
      setErrors(['Brak zestawów do usunięcia.']);
      setIsErrorModalOpen(true);
      return;
    }

    setIsDeleteAllConfirmOpen(true);
  };

  /**
   * Handler: Potwierdź usunięcie wszystkich zestawów
   */
  const handleConfirmDeleteAll = async () => {
    if (!apiService) return;

    setLoading(true);
    try {
      const result = await apiService.ticketsDeleteAll();
      setToastMessage(result.message);
      setToastVariant('success');
      setIsDeleteAllConfirmOpen(false);
      await fetchTickets(searchTerm || undefined);
    } catch (error) {
      handleApiError(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-gradient-to-br from-gray-100 via-blue-50 to-yellow-50 relative overflow-hidden">
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

      <div className="container mx-auto px-4 py-8 max-w-6xl relative z-10 bg-white/90 backdrop-blur-sm rounded-lg shadow-lg">
        {/* Header Section */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 gap-4">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold text-gray-900">Moje zestawy</h1>
            <TicketCounter count={totalCount} max={100} />
          </div>
        </div>

        {/* Action Buttons Row */}
        <div className="flex flex-col sm:flex-row gap-3 mb-6">
          <Button onClick={handleAddTicket} variant="primary">
            + Dodaj ręcznie
          </Button>
          <Button onClick={handleGenerateRandom} variant="secondary">
            🎲 Generuj losowy
          </Button>
          <Button onClick={handleGenerateSystem} variant="secondary">
            🔢 Generuj systemowy
          </Button>
          <Button onClick={handleImportCsv} variant="secondary">
            📥 Importuj CSV
          </Button>
          <Button onClick={handleExportCsv} variant="secondary" disabled={totalCount === 0}>
            📤 Eksportuj CSV
          </Button>
          <Button onClick={handleDeleteAllTickets} variant="danger" disabled={totalCount === 0}>
            🗑️ Usuń wszystkie
          </Button>
        </div>

        {/* Search Filter */}
        <SearchFilter
          searchTerm={searchTerm}
          onSearch={handleSearch}
          onClear={handleClearSearch}
        />

        {/* Hidden file input for CSV import */}
        <input
          ref={fileInputRef}
          type="file"
          accept=".csv"
          onChange={handleFileSelected}
          className="hidden"
        />

        {/* Tickets List */}
        <TicketList
          tickets={tickets}
          loading={loading}
          onEdit={handleEditTicket}
          onDelete={handleDeleteTicket}
          searchTerm={searchTerm}
          onClearSearch={handleClearSearch}
        />
      </div>

      {/* Modals */}
      {ticketFormState && (
        <TicketFormModal
          isOpen={isTicketFormOpen}
          onClose={() => setIsTicketFormOpen(false)}
          mode={ticketFormState.mode}
          initialNumbers={ticketFormState.initialNumbers}
          initialGroupName={ticketFormState.initialGroupName}
          ticketId={ticketFormState.ticketId}
          onSubmit={handleSaveTicket}
        />
      )}

      {generatorState && generatorState.type === 'random' && (
        <GeneratorPreviewModal
          isOpen={isGeneratorPreviewOpen}
          onClose={() => setIsGeneratorPreviewOpen(false)}
          numbers={generatorState.numbers as number[]}
          onRegenerate={handleRegenerateRandom}
          onSave={handleSaveGeneratedRandom}
        />
      )}

      {generatorState && generatorState.type === 'system' && (
        <GeneratorSystemPreviewModal
          isOpen={isGeneratorPreviewOpen}
          onClose={() => setIsGeneratorPreviewOpen(false)}
          tickets={generatorState.numbers as number[][]}
          onRegenerate={handleRegenerateSystem}
          onSaveAll={handleSaveGeneratedSystem}
        />
      )}

      {ticketToDelete && (
        <DeleteConfirmModal
          isOpen={isDeleteConfirmOpen}
          onClose={() => {
            setIsDeleteConfirmOpen(false);
            setTicketToDelete(null);
          }}
          ticket={ticketToDelete}
          onConfirm={handleConfirmDelete}
        />
      )}

      {/* Delete All Confirmation Modal */}
      <DeleteAllConfirmModal
        isOpen={isDeleteAllConfirmOpen}
        onClose={() => setIsDeleteAllConfirmOpen(false)}
        totalCount={totalCount}
        onConfirm={handleConfirmDeleteAll}
      />

      <ErrorModal
        isOpen={isErrorModalOpen}
        onClose={() => setIsErrorModalOpen(false)}
        errors={errors}
      />

      {/* Import CSV Result Modal */}
      {importResult && (
        <ImportCsvModal
          isOpen={isImportModalOpen}
          onClose={() => {
            setIsImportModalOpen(false);
            setImportResult(null);
          }}
          result={importResult}
        />
      )}

      {/* Toast */}
      {toastMessage && (
        <Toast
          message={toastMessage}
          variant={toastVariant}
          onClose={() => setToastMessage(null)}
        />
      )}
    </main>
  );
}

/**
 * SearchFilter - Filter tickets by group name
 */
interface SearchFilterProps {
  searchTerm: string;
  onSearch: (term: string) => void;
  onClear: () => void;
}

function SearchFilter({ searchTerm, onSearch, onClear }: SearchFilterProps) {
  const [localTerm, setLocalTerm] = useState(searchTerm);
  const timeoutRef = useRef<number | null>(null);

  // Synchronize local state with props (when parent changes searchTerm externally)
  useEffect(() => {
    setLocalTerm(searchTerm);
  }, [searchTerm]);

  // Debounced search
  useEffect(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    timeoutRef.current = setTimeout(() => {
      onSearch(localTerm);
    }, 300);

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [localTerm, onSearch]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setLocalTerm(e.target.value);
  };

  const handleClear = () => {
    setLocalTerm('');
    onClear();
  };

  return (
    <div className="flex flex-col sm:flex-row items-stretch gap-2 mb-8">
      <div className="relative flex-1">
        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">
          🔍
        </span>
        <input
          type="text"
          value={localTerm}
          onChange={handleInputChange}
          placeholder="Szukaj w grupach..."
          maxLength={100}
          className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-all"
          aria-label="Wyszukaj zestawy po nazwie grupy"
        />
      </div>
      {searchTerm && (
        <button
          onClick={handleClear}
          className="px-3 py-2 text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded-lg transition-colors whitespace-nowrap"
          aria-label="Wyczyść filtr"
        >
          ✕ Wyczyść
        </button>
      )}
    </div>
  );
}

/**
 * ImportCsvModal - Displays CSV import results
 */
interface ImportCsvModalProps {
  isOpen: boolean;
  onClose: () => void;
  result: TicketsImportCsvResponse;
}

function ImportCsvModal({ isOpen, onClose, result }: ImportCsvModalProps) {
  if (!isOpen) return null;

  const hasErrors = result.errors.length > 0;
  const allRejected = result.imported === 0 && result.rejected > 0;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[80vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center">
          <h2 className="text-xl font-bold text-gray-900">
            Raport importu CSV
          </h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-6">
          {/* Summary */}
          <div className="grid grid-cols-2 gap-4">
            <div className={`p-4 rounded-lg ${result.imported > 0 ? 'bg-green-50 border border-green-200' : 'bg-gray-50 border border-gray-200'}`}>
              <div className="text-sm text-gray-600 mb-1">Zaimportowano</div>
              <div className={`text-2xl font-bold ${result.imported > 0 ? 'text-green-600' : 'text-gray-400'}`}>
                {result.imported}
              </div>
            </div>
            <div className={`p-4 rounded-lg ${result.rejected > 0 ? 'bg-red-50 border border-red-200' : 'bg-gray-50 border border-gray-200'}`}>
              <div className="text-sm text-gray-600 mb-1">Odrzucono</div>
              <div className={`text-2xl font-bold ${result.rejected > 0 ? 'text-red-600' : 'text-gray-400'}`}>
                {result.rejected}
              </div>
            </div>
          </div>

          {/* Success Message */}
          {result.imported > 0 && !allRejected && (
            <div className="bg-green-50 border border-green-200 rounded-lg p-4">
              <div className="flex items-start gap-3">
                <svg className="w-5 h-5 text-green-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <div className="flex-1">
                  <p className="text-sm font-medium text-green-800">
                    Pomyślnie zaimportowano {result.imported} {result.imported === 1 ? 'zestaw' : 'zestawy'}.
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* All Rejected Message */}
          {allRejected && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <div className="flex items-start gap-3">
                <svg className="w-5 h-5 text-red-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
                <div className="flex-1">
                  <p className="text-sm font-medium text-red-800">
                    Nie zaimportowano żadnego zestawu. Wszystkie wiersze zawierały błędy.
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Errors List */}
          {hasErrors && (
            <div>
              <h3 className="text-sm font-semibold text-gray-700 mb-3">
                Błędy walidacji ({result.errors.length})
              </h3>
              <div className="bg-gray-50 border border-gray-200 rounded-lg divide-y divide-gray-200 max-h-60 overflow-y-auto">
                {result.errors.map((error, index) => (
                  <div key={index} className="px-4 py-3 hover:bg-gray-100 transition-colors">
                    <div className="flex items-start gap-3">
                      <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-red-100 text-red-600 text-xs font-bold flex-shrink-0">
                        {error.row}
                      </span>
                      <p className="text-sm text-gray-700 flex-1">
                        {error.reason}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 bg-gray-50 border-t border-gray-200 px-6 py-4 flex justify-end">
          <Button onClick={onClose} variant="primary">
            Zamknij
          </Button>
        </div>
      </div>
    </div>
  );
}

/**
 * DeleteAllConfirmModal - Confirmation dialog for deleting all tickets
 */
interface DeleteAllConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  totalCount: number;
  onConfirm: () => void;
}

function DeleteAllConfirmModal({ isOpen, onClose, totalCount, onConfirm }: DeleteAllConfirmModalProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
        {/* Header */}
        <div className="bg-red-50 border-b border-red-200 px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center flex-shrink-0">
              <svg className="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            <div>
              <h2 className="text-xl font-bold text-red-900">Usuń wszystkie zestawy</h2>
              <p className="text-sm text-red-700 mt-1">Ta operacja jest nieodwracalna</p>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="p-6 space-y-4">
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <p className="text-sm text-gray-700">
              Czy na pewno chcesz usunąć <span className="font-bold text-red-600">{totalCount}</span> {totalCount === 1 ? 'zestaw' : totalCount < 5 ? 'zestawy' : 'zestawów'}?
            </p>
          </div>
          <p className="text-sm text-gray-600">
            Wszystkie Twoje zestawy zostaną trwale usunięte. Nie będzie można ich przywrócić.
          </p>
        </div>

        {/* Footer */}
        <div className="bg-gray-50 border-t border-gray-200 px-6 py-4 flex justify-end gap-3">
          <Button onClick={onClose} variant="secondary">
            Anuluj
          </Button>
          <Button onClick={onConfirm} variant="danger">
            Tak, usuń wszystkie
          </Button>
        </div>
      </div>
    </div>
  );
}

export default TicketsPage;
