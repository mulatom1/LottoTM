import React from 'react';
import { Modal, Button } from '../shared';
import type { GeneratorSystemPreviewModalProps } from '../../types/tickets';

/**
 * Modal preview dla 9 zestawów systemowych
 * Wyświetla grid 3x3 (desktop) lub vertical (mobile)
 */
const GeneratorSystemPreviewModal: React.FC<GeneratorSystemPreviewModalProps> = ({
  isOpen,
  onClose,
  tickets,
  onRegenerate,
  onSaveAll
}) => {
  const [isSaving, setIsSaving] = React.useState(false);

  const handleSaveAll = async () => {
    setIsSaving(true);
    try {
      await onSaveAll();
      // Sukces - parent component zamyka modal i pokazuje toast
    } catch (error) {
      // Błędy obsługiwane w parent component
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Generator systemowy (9 zestawów)"
      size="xl"
    >
      <div className="mb-4">
        <p className="text-sm text-gray-600 mb-2">
          Generator tworzy 9 zestawów pokrywających wszystkie liczby od 1 do 49.
          Każda liczba pojawia się minimum raz.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6 max-h-[60vh] overflow-y-auto">
        {tickets.map((numbers, index) => (
          <div key={index} className="border rounded p-3 bg-gray-50">
            <p className="text-sm font-semibold mb-2 text-gray-700">
              Zestaw {index + 1}:
            </p>
            <div className="flex flex-wrap gap-1">
              {numbers.map((num, numIndex) => (
                <span
                  key={numIndex}
                  className="px-2 py-1 bg-blue-100 text-blue-800 rounded text-sm font-semibold min-w-[2rem] text-center"
                >
                  {num}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="flex flex-col sm:flex-row justify-between gap-2">
        <Button
          onClick={onRegenerate}
          variant="secondary"
          disabled={isSaving}
        >
          Generuj ponownie
        </Button>
        <div className="flex gap-2">
          <Button onClick={onClose} variant="secondary" disabled={isSaving}>
            Anuluj
          </Button>
          <Button onClick={handleSaveAll} variant="primary" disabled={isSaving}>
            {isSaving ? 'Zapisywanie...' : 'Zapisz wszystkie'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default GeneratorSystemPreviewModal;
