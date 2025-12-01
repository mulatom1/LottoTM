import { useState, useEffect, useMemo } from 'react';
import { useAppContext } from '../../context/app-context';
import DrawsFilterPanel from '../../components/draws/draws-filter-panel';
import DrawList from '../../components/draws/draw-list';
import DrawFormModal from '../../components/draws/draw-form-modal';
import DeleteConfirmationModal from '../../components/draws/delete-confirmation-modal';
import ErrorModal from '../../components/shared/error-modal';
import Toast from '../../components/shared/toast';
import Pagination from '../../components/shared/pagination';
import Button from '../../components/shared/button';
import type {
  FilterState,
  PaginationState,
  ModalState,
  DrawFormData,
  DrawDto
} from '../../types/draws';
import type { DrawsGetListRequest } from '../../services/contracts/draws-get-list-request';
import type { DrawsCreateRequest } from '../../services/contracts/draws-create-request';
import type { DrawsUpdateRequest } from '../../services/contracts/draws-update-request';

/**
 * Main page for viewing and managing lottery draw results
 * Features: list with pagination, filtering by date range, CRUD operations (admin only)
 */
function DrawsPage() {
  const { user, getApiService } = useAppContext();
  const apiService = getApiService()!; // Non-null assertion - AppContext always provides ApiService
  const isAdmin = user?.isAdmin || false;

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

  // State: draws list
  const [draws, setDraws] = useState<DrawDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  // State: pagination
  const [paginationState, setPaginationState] = useState<PaginationState>({
    currentPage: 1,
    totalPages: 1,
    pageSize: 20,
    totalCount: 0,
  });

  // State: filter
  const [filterState, setFilterState] = useState<FilterState>({
    dateFrom: '',
    dateTo: '',
    isActive: false,
  });

  // State: modals
  const [modalState, setModalState] = useState<ModalState>({
    isFormOpen: false,
    formMode: 'add',
    editingDraw: null,
    isDeleteOpen: false,
    deletingDraw: null,
    isErrorOpen: false,
    errorMessages: [],
  });

  // State: toast
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  /**
   * Fetch draws from API with current pagination and filter settings
   */
  const fetchDraws = async (
    page: number = paginationState.currentPage,
    filter: FilterState = filterState
  ) => {
    setLoading(true);
    try {
      const params: DrawsGetListRequest = {
        page,
        pageSize: 20,
        sortBy: 'drawDate',
        sortOrder: 'desc',
        ...(filter.isActive && {
          dateFrom: filter.dateFrom || undefined,
          dateTo: filter.dateTo || undefined,
        }),
      };

      const response = await apiService.drawsGetList(params);

      setDraws(response.draws);
      setPaginationState({
        currentPage: response.page,
        totalPages: response.totalPages,
        pageSize: response.pageSize,
        totalCount: response.totalCount,
      });
    } catch (error) {
      handleApiError(error, 'Nie udało się pobrać listy losowań');
    } finally {
      setLoading(false);
    }
  };

  /**
   * Handle filter application
   */
  const handleFilter = (dateFrom?: string, dateTo?: string) => {
    // Inline validation: dateFrom must be <= dateTo
    if (dateFrom && dateTo && dateFrom > dateTo) {
      setModalState(prev => ({
        ...prev,
        isErrorOpen: true,
        errorMessages: ["Data 'Od' musi być wcześniejsza lub równa 'Do'"]
      }));
      return;
    }

    // Create new filter state
    const newFilterState: FilterState = {
      dateFrom: dateFrom || '',
      dateTo: dateTo || '',
      isActive: !!(dateFrom || dateTo),
    };

    // Update filter state
    setFilterState(newFilterState);

    // Fetch draws with new filter from page 1
    fetchDraws(1, newFilterState);
  };

  /**
   * Handle filter clear
   */
  const handleClearFilter = () => {
    const clearedFilterState: FilterState = {
      dateFrom: '',
      dateTo: '',
      isActive: false,
    };

    setFilterState(clearedFilterState);

    // Refresh list without filter from page 1
    fetchDraws(1, clearedFilterState);
  };

  /**
   * Handle page change
   */
  const handlePageChange = (page: number) => {
    fetchDraws(page);
  };

  /**
   * Open add modal
   */
  const handleOpenAddModal = () => {
    setModalState((prev) => ({
      ...prev,
      isFormOpen: true,
      formMode: 'add',
      editingDraw: null,
    }));
  };

  /**
   * Open edit modal
   */
  const handleOpenEditModal = (drawId: number) => {
    const draw = draws.find((d) => d.id === drawId);
    if (!draw) return;

    setModalState((prev) => ({
      ...prev,
      isFormOpen: true,
      formMode: 'edit',
      editingDraw: draw,
    }));
  };

  /**
   * Open delete modal
   */
  const handleOpenDeleteModal = (drawId: number) => {
    const draw = draws.find((d) => d.id === drawId);
    if (!draw) return;

    setModalState((prev) => ({
      ...prev,
      isDeleteOpen: true,
      deletingDraw: draw,
    }));
  };

  /**
   * Handle form submit (add or edit)
   */
  const handleFormSubmit = async (data: DrawFormData) => {
    try {
      // Convert DrawFormData to API request format
      const numbers = data.numbers.filter((n): n is number => typeof n === 'number');

      // Helper function to convert '' to undefined for optional numeric fields
      const getOptionalNumber = (value: number | '' | undefined): number | undefined => {
        if (value === '' || value === undefined) return undefined;
        return value;
      };

      if (modalState.formMode === 'add') {
        const request: DrawsCreateRequest = {
          drawDate: data.drawDate,
          lottoType: data.lottoType,
          numbers,
          drawSystemId: typeof data.drawSystemId === 'number' ? data.drawSystemId : 0, // Required field
          ticketPrice: getOptionalNumber(data.ticketPrice),
          winPoolCount1: getOptionalNumber(data.winPoolCount1),
          winPoolAmount1: getOptionalNumber(data.winPoolAmount1),
          winPoolCount2: getOptionalNumber(data.winPoolCount2),
          winPoolAmount2: getOptionalNumber(data.winPoolAmount2),
          winPoolCount3: getOptionalNumber(data.winPoolCount3),
          winPoolAmount3: getOptionalNumber(data.winPoolAmount3),
          winPoolCount4: getOptionalNumber(data.winPoolCount4),
          winPoolAmount4: getOptionalNumber(data.winPoolAmount4),
        };
        await apiService.drawsCreate(request);
        setToastMessage('Wynik losowania zapisany pomyślnie');
      } else if (modalState.editingDraw) {
        const request: DrawsUpdateRequest = {
          drawDate: data.drawDate,
          lottoType: data.lottoType,
          numbers,
          drawSystemId: typeof data.drawSystemId === 'number' ? data.drawSystemId : 0, // Required field
          ticketPrice: getOptionalNumber(data.ticketPrice),
          winPoolCount1: getOptionalNumber(data.winPoolCount1),
          winPoolAmount1: getOptionalNumber(data.winPoolAmount1),
          winPoolCount2: getOptionalNumber(data.winPoolCount2),
          winPoolAmount2: getOptionalNumber(data.winPoolAmount2),
          winPoolCount3: getOptionalNumber(data.winPoolCount3),
          winPoolAmount3: getOptionalNumber(data.winPoolAmount3),
          winPoolCount4: getOptionalNumber(data.winPoolCount4),
          winPoolAmount4: getOptionalNumber(data.winPoolAmount4),
        };
        await apiService.drawsUpdate(modalState.editingDraw.id, request);
        setToastMessage('Wynik zaktualizowany pomyślnie');
      }

      // Close modal
      setModalState((prev) => ({
        ...prev,
        isFormOpen: false,
        editingDraw: null,
      }));

      // Refresh current page
      fetchDraws();
    } catch (error) {
      handleApiError(error, 'Nie udało się zapisać wyniku losowania');
      // Don't close modal on error - let user fix validation errors
      throw error; // Re-throw to prevent modal from closing in component
    }
  };

  /**
   * Handle delete confirmation
   */
  const handleDeleteConfirm = async (drawId: number) => {
    try {
      await apiService.drawsDelete(drawId);
      setToastMessage('Wynik usunięty pomyślnie');

      // Close modal
      setModalState((prev) => ({
        ...prev,
        isDeleteOpen: false,
        deletingDraw: null,
      }));

      // Refresh list - if last item on page was deleted, go to previous page
      const shouldGoBack =
        draws.length === 1 && paginationState.currentPage > 1;
      fetchDraws(shouldGoBack ? paginationState.currentPage - 1 : undefined);
    } catch (error) {
      handleApiError(error, 'Nie udało się usunąć wyniku losowania');
      throw error; // Re-throw to prevent modal from closing
    }
  };

  /**
   * Handle API errors and show error modal
   */
  const handleApiError = (error: any, defaultMessage: string) => {
    let errorMessages: string[] = [];

    if (error?.message) {
      // Error from ApiService with parsed message
      errorMessages = [error.message];
    } else if (typeof error === 'string') {
      errorMessages = [error];
    } else {
      errorMessages = [defaultMessage];
    }

    setModalState((prev) => ({
      ...prev,
      isErrorOpen: true,
      errorMessages,
    }));
  };

  /**
   * Close error modal
   */
  const handleCloseErrorModal = () => {
    setModalState((prev) => ({
      ...prev,
      isErrorOpen: false,
      errorMessages: [],
    }));
  };

  /**
   * Close form modal
   */
  const handleCloseFormModal = () => {
    setModalState((prev) => ({
      ...prev,
      isFormOpen: false,
      editingDraw: null,
    }));
  };

  /**
   * Close delete modal
   */
  const handleCloseDeleteModal = () => {
    setModalState((prev) => ({
      ...prev,
      isDeleteOpen: false,
      deletingDraw: null,
    }));
  };

  /**
   * Close toast
   */
  const handleCloseToast = () => {
    setToastMessage(null);
  };

  // Effect: Initial load
  useEffect(() => {
    fetchDraws(1);
  }, []); // Empty dependency array - run once on mount

  // Prepare initial values for edit form
  const getFormInitialValues = (): DrawFormData | undefined => {
    if (!modalState.editingDraw) return undefined;

    const draw = modalState.editingDraw;

    // Convert DateTime string (e.g., "2025-01-15T00:00:00Z") to YYYY-MM-DD format
    const drawDate = draw.drawDate.split('T')[0];

    // Helper function to convert null to '' for form fields
    const getFormValue = (value: number | null): number | '' => {
      return value ?? '';
    };

    return {
      drawDate,
      lottoType: draw.lottoType,
      numbers: draw.numbers as (number | '')[],
      drawSystemId: draw.drawSystemId,
      ticketPrice: getFormValue(draw.ticketPrice),
      winPoolCount1: getFormValue(draw.winPoolCount1),
      winPoolAmount1: getFormValue(draw.winPoolAmount1),
      winPoolCount2: getFormValue(draw.winPoolCount2),
      winPoolAmount2: getFormValue(draw.winPoolAmount2),
      winPoolCount3: getFormValue(draw.winPoolCount3),
      winPoolAmount3: getFormValue(draw.winPoolAmount3),
      winPoolCount4: getFormValue(draw.winPoolCount4),
      winPoolAmount4: getFormValue(draw.winPoolAmount4),
    };
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

      <div className="container mx-auto px-4 py-6 max-w-6xl relative z-10">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-6 gap-4">
        <h1 className="text-3xl font-bold text-gray-900">Historia losowań</h1>
        {isAdmin && (
          <Button variant="primary" onClick={handleOpenAddModal}>
            + Dodaj wynik
          </Button>
        )}
      </div>

      {/* Filter Panel */}
      <DrawsFilterPanel
        onFilter={handleFilter}
        onClearFilter={handleClearFilter}
        isFilterActive={filterState.isActive}
        initialDateFrom={filterState.dateFrom}
        initialDateTo={filterState.dateTo}
      />

      {/* Filter Info */}
      {filterState.isActive && (
        <div className="mb-4 text-sm text-gray-600">
          Filtr aktywny: {filterState.dateFrom || '...'} - {filterState.dateTo || '...'}
        </div>
      )}

      {/* Draws List */}
      <DrawList
        draws={draws}
        isAdmin={isAdmin}
        onEdit={handleOpenEditModal}
        onDelete={handleOpenDeleteModal}
        loading={loading}
      />

      {/* Pagination */}
      {!loading && draws.length > 0 && (
        <Pagination
          currentPage={paginationState.currentPage}
          totalPages={paginationState.totalPages}
          totalCount={paginationState.totalCount}
          onPageChange={handlePageChange}
        />
      )}

      {/* Modals */}
      <DrawFormModal
        isOpen={modalState.isFormOpen}
        mode={modalState.formMode}
        initialValues={getFormInitialValues()}
        onSubmit={handleFormSubmit}
        onCancel={handleCloseFormModal}
      />

      <DeleteConfirmationModal
        isOpen={modalState.isDeleteOpen}
        draw={modalState.deletingDraw}
        onConfirm={handleDeleteConfirm}
        onCancel={handleCloseDeleteModal}
      />

      <ErrorModal
        isOpen={modalState.isErrorOpen}
        onClose={handleCloseErrorModal}
        errors={modalState.errorMessages}
      />

      {/* Toast */}
      <Toast
        message={toastMessage}
        onClose={handleCloseToast}
        variant="success"
      />
      </div>
    </main>
  );
}

export default DrawsPage;
