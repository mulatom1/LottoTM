import { useState, useEffect, useCallback } from 'react';
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
import { generateRandomNumbers, generateSystemTickets } from '../../utils/generators';
import { validateNumbers } from '../../utils/validators';

function TicketsPage() {
  const { getApiService, logout, isLoggedIn } = useAppContext();
  const navigate = useNavigate();
  const apiService = getApiService();

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

  // Modal state
  const [isTicketFormOpen, setIsTicketFormOpen] = useState<boolean>(false);
  const [ticketFormState, setTicketFormState] = useState<TicketFormState | null>(null);
  const [isGeneratorPreviewOpen, setIsGeneratorPreviewOpen] = useState<boolean>(false);
  const [generatorState, setGeneratorState] = useState<GeneratorState | null>(null);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState<boolean>(false);
  const [ticketToDelete, setTicketToDelete] = useState<Ticket | null>(null);
  const [isErrorModalOpen, setIsErrorModalOpen] = useState<boolean>(false);
  const [errors, setErrors] = useState<string[]>([]);

  // Toast state
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [toastVariant, setToastVariant] = useState<'success' | 'error' | 'info'>('success');

  /**
   * Fetch all tickets from API
   */
  const fetchTickets = useCallback(async () => {
    if (!apiService) return;

    setLoading(true);
    try {
      const response = await apiService.ticketsGetList();
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
      await fetchTickets();
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
      await fetchTickets();
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
  const handleSaveGeneratedRandom = async (numbers: number[]) => {
    if (!apiService) return;

    // Sprawdzenie limitu ponownie (race condition)
    if (totalCount >= 100) {
      setErrors(['Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.']);
      setIsErrorModalOpen(true);
      setIsGeneratorPreviewOpen(false);
      return;
    }

    try {
      await apiService.ticketsCreate({ numbers });
      setToastMessage('Zestaw wygenerowany i zapisany');
      setToastVariant('success');
      setIsGeneratorPreviewOpen(false);
      await fetchTickets();
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
  const handleSaveGeneratedSystem = async () => {
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
      await apiService.ticketsGenerateSystem();
      setToastMessage('9 zestawów wygenerowanych i zapisanych');
      setToastVariant('success');
      setIsGeneratorPreviewOpen(false);
      await fetchTickets();
    } catch (error) {
      handleApiError(error);
    }
  };

  return (
    <>
      <div className="container mx-auto px-4 py-8 max-w-6xl">
        {/* Header Section */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 gap-4">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold text-gray-900">Moje zestawy</h1>
            <TicketCounter count={totalCount} max={100} />
          </div>
        </div>

        {/* Action Buttons Row */}
        <div className="flex flex-col sm:flex-row gap-3 mb-8">
          <Button onClick={handleAddTicket} variant="primary">
            + Dodaj ręcznie
          </Button>
          <Button onClick={handleGenerateRandom} variant="secondary">
            🎲 Generuj losowy
          </Button>
          <Button onClick={handleGenerateSystem} variant="secondary">
            🔢 Generuj systemowy
          </Button>
        </div>

        {/* Tickets List */}
        <TicketList
          tickets={tickets}
          loading={loading}
          onEdit={handleEditTicket}
          onDelete={handleDeleteTicket}
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

      <ErrorModal
        isOpen={isErrorModalOpen}
        onClose={() => setIsErrorModalOpen(false)}
        errors={errors}
      />

      {/* Toast */}
      {toastMessage && (
        <Toast
          message={toastMessage}
          variant={toastVariant}
          onClose={() => setToastMessage(null)}
        />
      )}
    </>
  );
}

export default TicketsPage;
