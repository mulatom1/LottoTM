import React from 'react';
import { Modal, Button } from '../shared';
import type { DeleteConfirmModalProps } from '../../types/tickets';

/**
 * Modal potwierdzenia usunięcia zestawu
 * Wyświetla liczby zestawu do usunięcia
 */
const DeleteConfirmModal: React.FC<DeleteConfirmModalProps> = ({
  isOpen,
  onClose,
  ticket,
  onConfirm
}) => {
  const [isDeleting, setIsDeleting] = React.useState(false);

  const handleConfirm = async () => {
    setIsDeleting(true);
    try {
      await onConfirm(ticket.id);
      // Sukces - parent component zamyka modal i pokazuje toast
    } catch (error) {
      // Błędy obsługiwane w parent component
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Usuń zestaw" size="sm">
      <div className="mb-6">
        <p className="mb-2 text-gray-700">
          Czy na pewno chcesz usunąć ten zestaw?
        </p>

        {/* Nazwa grupy (jeśli istnieje) */}
        {ticket.groupName && (
          <div className="mt-2 mb-3 text-center">
            <span className="inline-block px-2 py-1 bg-purple-100 text-purple-800 rounded text-sm font-medium">
              {ticket.groupName}
            </span>
          </div>
        )}

        {/* Liczby zestawu */}
        <div className="flex flex-wrap gap-2 justify-center mt-4 p-4 bg-gray-50 rounded">
          {ticket.numbers.map((num, index) => (
            <span
              key={index}
              className="px-3 py-1 bg-gray-200 text-gray-700 rounded font-semibold min-w-[2.5rem] text-center"
            >
              {num}
            </span>
          ))}
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button onClick={onClose} variant="secondary" disabled={isDeleting}>
          Anuluj
        </Button>
        <Button onClick={handleConfirm} variant="danger" disabled={isDeleting}>
          {isDeleting ? 'Usuwanie...' : 'Usuń'}
        </Button>
      </div>
    </Modal>
  );
};

export default DeleteConfirmModal;
