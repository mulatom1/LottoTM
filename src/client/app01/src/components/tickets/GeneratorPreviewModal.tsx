import React from 'react';
import { Modal, Button } from '../shared';
import type { GeneratorPreviewModalProps } from '../../types/tickets';

/**
 * Modal preview dla losowego zestawu
 * Wyświetla 6 wygenerowanych liczb z możliwością regeneracji lub zapisu
 */
const GeneratorPreviewModal: React.FC<GeneratorPreviewModalProps> = ({
  isOpen,
  onClose,
  numbers,
  onRegenerate,
  onSave
}) => {
  const [isSaving, setIsSaving] = React.useState(false);
  const [groupName, setGroupName] = React.useState('');

  // Reset groupName when modal opens or numbers change (regenerate)
  React.useEffect(() => {
    if (isOpen) {
      setGroupName('');
    }
  }, [isOpen, numbers]);

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await onSave(numbers, groupName);
      // Sukces - parent component zamyka modal i pokazuje toast
    } catch (error) {
      // Błędy obsługiwane w parent component
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Generator losowy" size="md">
      <div className="mb-6">
        <p className="text-sm text-gray-600 mb-4">Wygenerowany zestaw:</p>
        <div className="flex flex-wrap gap-2 justify-center">
          {numbers.map((num, index) => (
            <span
              key={index}
              className="px-4 py-2 bg-green-100 text-green-800 rounded-full font-bold text-lg min-w-[3rem] text-center"
            >
              {num}
            </span>
          ))}
        </div>
      </div>

      <div className="mb-6">
        <label htmlFor="groupName" className="block text-sm font-medium text-gray-700 mb-2">
          Nazwa zestawu (opcjonalnie)
        </label>
        <input
          id="groupName"
          type="text"
          value={groupName}
          onChange={(e) => setGroupName(e.target.value)}
          placeholder="Podaj nazwę zestawu..."
          className="w-full px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          disabled={isSaving}
        />
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
          <Button onClick={handleSave} variant="primary" disabled={isSaving}>
            {isSaving ? 'Zapisywanie...' : 'Zapisz'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default GeneratorPreviewModal;
