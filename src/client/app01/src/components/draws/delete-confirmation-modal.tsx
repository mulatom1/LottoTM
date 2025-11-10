import React from 'react';
import Modal from '../shared/modal';
import Button from '../shared/button';

export interface DeleteConfirmationModalProps {
  isOpen: boolean;
  draw: {
    id: number;
    drawDate: string;
    numbers: number[];
  } | null;
  onConfirm: (drawId: number) => Promise<void>;
  onCancel: () => void;
}

/**
 * Confirmation modal for deleting a draw
 * Shows draw details and requires explicit confirmation
 */
const DeleteConfirmationModal: React.FC<DeleteConfirmationModalProps> = ({
  isOpen,
  draw,
  onConfirm,
  onCancel,
}) => {
  const [deleting, setDeleting] = React.useState(false);

  // Format date from YYYY-MM-DD to DD.MM.YYYY
  const formatDate = (dateString: string): string => {
    const [year, month, day] = dateString.split('-');
    return `${day}.${month}.${year}`;
  };

  const handleConfirm = async () => {
    if (!draw) return;

    setDeleting(true);
    try {
      await onConfirm(draw.id);
      // Modal will be closed by parent component on success
    } catch (error) {
      // Error handling is done by parent component
      console.error('Error deleting draw:', error);
    } finally {
      setDeleting(false);
    }
  };

  if (!draw) return null;

  return (
    <Modal isOpen={isOpen} onClose={onCancel} title="Usuń wynik losowania" size="md">
      <div className="space-y-4">
        {/* Confirmation message */}
        <p className="text-gray-700">
          Czy na pewno chcesz usunąć wynik losowania z dnia{' '}
          <span className="font-bold">{formatDate(draw.drawDate)}</span>?
        </p>

        {/* Display numbers */}
        <div className="bg-gray-50 rounded-lg p-4">
          <div className="flex flex-wrap gap-2 justify-center">
            {draw.numbers.map((number, index) => (
              <span
                key={index}
                className="inline-flex items-center justify-center w-10 h-10 bg-blue-100 text-blue-800 font-bold rounded-full text-sm"
              >
                {number}
              </span>
            ))}
          </div>
        </div>

        {/* Warning message */}
        <div className="bg-red-50 border border-red-200 rounded-lg p-3">
          <p className="text-sm text-red-800">
            <span className="font-bold">Uwaga:</span> Ta operacja jest nieodwracalna. Wynik
            losowania zostanie trwale usunięty z bazy danych.
          </p>
        </div>

        {/* Action buttons */}
        <div className="flex gap-3 pt-4 border-t border-gray-200">
          <Button
            variant="secondary"
            onClick={onCancel}
            disabled={deleting}
            className="flex-1"
          >
            Anuluj
          </Button>
          <Button
            variant="danger"
            onClick={handleConfirm}
            disabled={deleting}
            className="flex-1"
          >
            {deleting ? 'Usuwanie...' : 'Usuń'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default DeleteConfirmationModal;
