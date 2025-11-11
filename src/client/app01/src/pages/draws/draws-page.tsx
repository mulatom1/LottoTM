import { useState, useEffect } from 'react';
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
  const fetchDraws = async (page: number = paginationState.currentPage) => {
    setLoading(true);
    try {
      const params: DrawsGetListRequest = {
        page,
        pageSize: 20,
        sortBy: 'drawDate',
        sortOrder: 'desc',
        ...(filterState.isActive && {
          dateFrom: filterState.dateFrom || undefined,
          dateTo: filterState.dateTo || undefined,
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

    // Update filter state
    setFilterState({
      dateFrom: dateFrom || '',
      dateTo: dateTo || '',
      isActive: !!(dateFrom || dateTo),
    });

    // Fetch draws with new filter from page 1
    fetchDraws(1);
  };

  /**
   * Handle filter clear
   */
  const handleClearFilter = () => {
    setFilterState({
      dateFrom: '',
      dateTo: '',
      isActive: false,
    });

    // Refresh list without filter from page 1
    fetchDraws(1);
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

      if (modalState.formMode === 'add') {
        const request: DrawsCreateRequest = {
          drawDate: data.drawDate,
          lottoType: data.lottoType,
          numbers,
        };
        await apiService.drawsCreate(request);
        setToastMessage('Wynik losowania zapisany pomyślnie');
      } else if (modalState.editingDraw) {
        const request: DrawsUpdateRequest = {
          drawDate: data.drawDate,
          lottoType: data.lottoType,
          numbers,
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

    // Convert DateTime string (e.g., "2025-01-15T00:00:00Z") to YYYY-MM-DD format
    const drawDate = modalState.editingDraw.drawDate.split('T')[0];

    return {
      drawDate,
      lottoType: modalState.editingDraw.lottoType,
      numbers: modalState.editingDraw.numbers as (number | '')[],
    };
  };

  return (
    <div className="container mx-auto px-4 py-6 max-w-6xl">
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
  );
}

export default DrawsPage;
