import React, { useState, useEffect } from 'react';
import { Modal, Button, NumberInput } from '../shared';
import type { TicketFormModalProps } from '../../types/tickets';
import { validateField, validateNumbers } from '../../utils/validators';

/**
 * Modal dodawania/edycji zestawu liczb LOTTO
 * Wspiera tryb 'add' (puste pola) i 'edit' (pre-wypełnione)
 */
const TicketFormModal: React.FC<TicketFormModalProps> = ({
  isOpen,
  onClose,
  mode,
  initialNumbers,
  initialGroupName,
  ticketId,
  onSubmit
}) => {
  const [groupName, setGroupName] = useState<string>(initialGroupName || '');
  const [numbers, setNumbers] = useState<(number | '')[]>(
    initialNumbers || ['', '', '', '', '', '']
  );
  const [inlineErrors, setInlineErrors] = useState<(string | undefined)[]>(
    [undefined, undefined, undefined, undefined, undefined, undefined]
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset form przy otwarciu modalu
  useEffect(() => {
    if (isOpen) {
      setGroupName(initialGroupName || '');
      if (mode === 'edit' && initialNumbers) {
        setNumbers(initialNumbers);
      } else {
        setNumbers(['', '', '', '', '', '']);
      }
      setInlineErrors([undefined, undefined, undefined, undefined, undefined, undefined]);
    }
  }, [isOpen, mode, initialNumbers, initialGroupName]);

  const handleNumberChange = (index: number, value: number | '') => {
    const newNumbers = [...numbers];
    newNumbers[index] = value;
    setNumbers(newNumbers);

    // Inline validation
    const newErrors = [...inlineErrors];
    newErrors[index] = validateField(index, value, newNumbers);
    setInlineErrors(newErrors);

    // Sprawdź duplikaty w innych polach
    if (value !== '') {
      newNumbers.forEach((num, i) => {
        if (i !== index && num === value) {
          newErrors[i] = 'Liczby w zestawie muszą być unikalne';
        }
      });
      setInlineErrors(newErrors);
    }
  };

  const handleClear = () => {
    setGroupName('');
    setNumbers(['', '', '', '', '', '']);
    setInlineErrors([undefined, undefined, undefined, undefined, undefined, undefined]);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Walidacja przed submitem
    const validationErrors = validateNumbers(numbers);
    if (validationErrors.length > 0) {
      // Błędy wyświetlone przez ErrorModal w parent component
      return;
    }

    // Sprawdzenie czy są inline errors
    if (inlineErrors.some(err => err !== undefined)) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit(numbers as number[], groupName, ticketId);
      // Sukces - parent component zamyka modal i pokazuje toast
    } catch (error) {
      // Błędy obsługiwane w parent component
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={mode === 'add' ? 'Dodaj nowy zestaw' : 'Edytuj zestaw'}
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        {/* Pole GroupName (opcjonalne) */}
        <div className="mb-6">
          <label htmlFor="groupName" className="block text-sm font-medium text-gray-700 mb-2">
            Nazwa grupy (opcjonalne)
          </label>
          <input
            id="groupName"
            type="text"
            value={groupName}
            onChange={(e) => setGroupName(e.target.value.slice(0, 100))}
            placeholder="np. Ulubione, Rodzina..."
            maxLength={100}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <p className="mt-1 text-xs text-gray-500">
            {groupName.length}/100 znaków
          </p>
        </div>

        {/* Pola liczb */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          {[0, 1, 2, 3, 4, 5].map((index) => (
            <NumberInput
              key={index}
              label={`Liczba ${index + 1}`}
              value={numbers[index]}
              onChange={(val) => handleNumberChange(index, val)}
              error={inlineErrors[index]}
              min={1}
              max={49}
              required
            />
          ))}
        </div>

        <div className="flex flex-col sm:flex-row justify-between gap-2">
          <Button
            type="button"
            onClick={handleClear}
            variant="secondary"
            disabled={isSubmitting}
          >
            Wyczyść
          </Button>
          <div className="flex gap-2">
            <Button
              type="button"
              onClick={onClose}
              variant="secondary"
              disabled={isSubmitting}
            >
              Anuluj
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={isSubmitting || inlineErrors.some(err => err !== undefined)}
            >
              {isSubmitting ? 'Zapisywanie...' : 'Zapisz'}
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  );
};

export default TicketFormModal;
