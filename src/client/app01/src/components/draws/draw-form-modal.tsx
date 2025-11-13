import React, { useState, useEffect } from 'react';
import Modal from '../shared/modal';
import DatePicker from '../shared/date-picker';
import NumberInput from '../shared/number-input';
import Button from '../shared/button';
import type { DrawFormData, DrawFormErrors } from '../../types/draws';
import { useAppContext } from '../../context/app-context';
import type { XLottoDrawsData } from '../../services/contracts/xlotto-actual-draws-response';

export interface DrawFormModalProps {
  isOpen: boolean;
  mode: 'add' | 'edit';
  initialValues?: DrawFormData;
  onSubmit: (data: DrawFormData) => Promise<void>;
  onCancel: () => void;
}

/**
 * Modal for adding or editing lottery draw results
 * Includes inline validation for all fields
 */
const DrawFormModal: React.FC<DrawFormModalProps> = ({
  isOpen,
  mode,
  initialValues,
  onSubmit,
  onCancel,
}) => {
  // Get today's date in YYYY-MM-DD format for max date validation
  const getTodayDate = (): string => {
    const today = new Date();
    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');
    const day = String(today.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const todayDate = getTodayDate();

  // Initial form state
  const getInitialFormData = (): DrawFormData => {
    if (initialValues) {
      return initialValues;
    }
    return {
      drawDate: '',
      lottoType: 'LOTTO', // Default to LOTTO
      numbers: ['', '', '', '', '', ''],
    };
  };

  const [formData, setFormData] = useState<DrawFormData>(getInitialFormData());
  const [formErrors, setFormErrors] = useState<DrawFormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [loading, setLoading] = useState(false);
  const { getApiService } = useAppContext();

  // Reset form when modal opens/closes or mode changes
  useEffect(() => {
    if (isOpen) {
      setFormData(getInitialFormData());
      setFormErrors({});
      setSubmitting(false);
    }
  }, [isOpen, mode, initialValues]);

  // Validate draw date
  const validateDrawDate = (date: string): string | undefined => {
    if (!date) {
      return 'Data losowania jest wymagana';
    }
    if (date > todayDate) {
      return 'Data losowania nie może być w przyszłości';
    }
    return undefined;
  };

  // Validate lotto type
  const validateLottoType = (type: string): string | undefined => {
    if (!type) {
      return 'Typ loterii jest wymagany';
    }
    if (type !== 'LOTTO' && type !== 'LOTTO PLUS') {
      return 'Dozwolone wartości: LOTTO, LOTTO PLUS';
    }
    return undefined;
  };

  // Validate single number
  const validateNumber = (value: number | ''): string | undefined => {
    if (value === '') {
      return 'To pole jest wymagane';
    }
    if (value < 1 || value > 49) {
      return 'Liczba musi być w zakresie 1-49';
    }
    return undefined;
  };

  // Validate uniqueness of numbers
  const validateUniqueness = (numbers: (number | '')[]): string[] => {
    const errors: string[] = new Array(6).fill('');
    const numericValues = numbers.filter((n): n is number => typeof n === 'number');

    // Check for duplicates
    const seen = new Set<number>();
    numericValues.forEach((num, idx) => {
      if (seen.has(num)) {
        // Find original index in the numbers array
        const originalIndex = numbers.findIndex((n, i) => n === num && i <= idx);
        const duplicateIndex = numbers.findIndex((n, i) => n === num && i > originalIndex);

        if (duplicateIndex !== -1) {
          errors[duplicateIndex] = 'Liczby muszą być unikalne';
        }
      } else {
        seen.add(num);
      }
    });

    return errors;
  };

  // Validate all fields
  const validateForm = (): boolean => {
    const errors: DrawFormErrors = {};

    // Validate draw date
    const dateError = validateDrawDate(formData.drawDate);
    if (dateError) {
      errors.drawDate = dateError;
    }

    // Validate lotto type
    const lottoTypeError = validateLottoType(formData.lottoType);
    if (lottoTypeError) {
      errors.lottoType = lottoTypeError;
    }

    // Validate numbers (range)
    const numberErrors: string[] = [];
    formData.numbers.forEach((num, index) => {
      const error = validateNumber(num);
      numberErrors[index] = error || '';
    });

    // Validate uniqueness
    const uniquenessErrors = validateUniqueness(formData.numbers);
    uniquenessErrors.forEach((error, index) => {
      if (error && !numberErrors[index]) {
        numberErrors[index] = error;
      }
    });

    // Set number errors if any exist
    if (numberErrors.some((err) => err !== '')) {
      errors.numbers = numberErrors;
    }

    setFormErrors(errors);

    // Return true if no errors
    return !errors.drawDate && !errors.lottoType && !errors.numbers;
  };

  // Handle draw date change
  const handleDrawDateChange = (value: string) => {
    setFormData((prev) => ({ ...prev, drawDate: value }));

    // Clear error when user types
    if (formErrors.drawDate) {
      setFormErrors((prev) => ({ ...prev, drawDate: undefined }));
    }
  };

  // Handle lotto type change
  const handleLottoTypeChange = (value: string) => {
    setFormData((prev) => ({ ...prev, lottoType: value }));

    // Clear error when user selects
    if (formErrors.lottoType) {
      setFormErrors((prev) => ({ ...prev, lottoType: undefined }));
    }
  };

  // Handle number change
  const handleNumberChange = (index: number, value: number | '') => {
    const newNumbers = [...formData.numbers];
    newNumbers[index] = value;
    setFormData((prev) => ({ ...prev, numbers: newNumbers }));

    // Clear error for this field when user types
    if (formErrors.numbers?.[index]) {
      const newErrors = [...(formErrors.numbers || [])];
      newErrors[index] = '';
      setFormErrors((prev) => ({ ...prev, numbers: newErrors }));
    }
  };

  // Handle clear button
  const handleClear = () => {
    setFormData(getInitialFormData());
    setFormErrors({});
  };

  // Handle load from XLotto button
  const handleLoadFromXLotto = async () => {
    // Validate that date is selected
    if (!formData.drawDate) {
      alert('Proszę najpierw wybrać datę losowania');
      return;
    }

    setLoading(true);
    try {
      const apiService = getApiService();
      if (!apiService) {
        alert('API service is not available');
        return;
      }

      const response = await apiService.xLottoActualDraws({
        date: formData.drawDate,
        lottoType: formData.lottoType,
      });

      // Parse the JSON data string
      const drawsData: XLottoDrawsData = JSON.parse(response.data);

      // Find the draw matching the selected lotto type
      const drawItem = drawsData.Data.find(
        (item) => item.GameType === formData.lottoType
      );

      if (!drawItem) {
        alert(`Nie znaleziono wyników dla typu losowania: ${formData.lottoType}`);
        return;
      }

      // Update form data with fetched numbers
      setFormData((prev) => ({
        ...prev,
        numbers: drawItem.Numbers as (number | '')[],
      }));

      // Clear any existing errors
      setFormErrors({});
    } catch (error) {
      console.error('Error loading numbers from XLotto:', error);
      alert(error instanceof Error ? error.message : 'Błąd podczas pobierania danych z XLotto');
    } finally {
      setLoading(false);
    }
  };

  // Handle submit
  const handleSubmit = async () => {
    // Validate form
    const isValid = validateForm();
    if (!isValid) {
      return;
    }

    setSubmitting(true);
    try {
      await onSubmit(formData);
      // Modal will be closed by parent component on success
    } catch (error) {
      // Error handling is done by parent component
      console.error('Error submitting form:', error);
    } finally {
      setSubmitting(false);
    }
  };

  const title = mode === 'add' ? 'Dodaj wynik losowania' : 'Edytuj wynik losowania';

  return (
    <Modal isOpen={isOpen} onClose={onCancel} title={title} size="lg">
      <div className="space-y-4">
        {/* Draw Date */}
        <DatePicker
          label="Data losowania"
          value={formData.drawDate}
          onChange={handleDrawDateChange}
          error={formErrors.drawDate}
          max={todayDate}
          required
        />

        {/* Lotto Type - Radio Buttons */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Typ loterii <span className="text-red-600">*</span>
          </label>
          <div className="flex gap-4">
            <label className="inline-flex items-center cursor-pointer">
              <input
                type="radio"
                name="lottoType"
                value="LOTTO"
                checked={formData.lottoType === 'LOTTO'}
                onChange={(e) => handleLottoTypeChange(e.target.value)}
                className="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500"
              />
              <span className="ml-2 text-sm text-gray-700">LOTTO</span>
            </label>
            <label className="inline-flex items-center cursor-pointer">
              <input
                type="radio"
                name="lottoType"
                value="LOTTO PLUS"
                checked={formData.lottoType === 'LOTTO PLUS'}
                onChange={(e) => handleLottoTypeChange(e.target.value)}
                className="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500"
              />
              <span className="ml-2 text-sm text-gray-700">LOTTO PLUS</span>
            </label>
          </div>
          {formErrors.lottoType && (
            <p className="mt-1 text-sm text-red-600">{formErrors.lottoType}</p>
          )}
        </div>

        {/* Numbers Grid */}
        <div>
          <p className="text-sm font-medium text-gray-700 mb-2">
            Wylosowane liczby <span className="text-red-600">*</span>
          </p>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
            {formData.numbers.map((num, index) => (
              <NumberInput
                key={index}
                label={`Liczba ${index + 1}`}
                value={num}
                onChange={(value) => handleNumberChange(index, value)}
                error={formErrors.numbers?.[index]}
                min={1}
                max={49}
                required
              />
            ))}
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-col sm:flex-row justify-between gap-3 pt-4 border-t border-gray-200">
          {/* Left: Clear and Load from XLotto buttons */}
          <div className="flex gap-3 order-2 sm:order-1">
            <Button
              variant="secondary"
              onClick={handleClear}
              disabled={submitting || loading}
              className="flex-1 sm:flex-none"
            >
              Wyczyść
            </Button>
            <Button
              variant="secondary"
              onClick={handleLoadFromXLotto}
              disabled={submitting || loading}
              className="flex-1 sm:flex-none"
            >
              {loading ? 'Pobieranie...' : 'Pobierz z XLotto'}
            </Button>
          </div>

          {/* Right: Cancel and Save buttons */}
          <div className="flex gap-3 order-1 sm:order-2">
            <Button
              variant="primary"
              onClick={handleSubmit}
              disabled={submitting || loading}
              className="flex-1 sm:flex-none"
            >
              {submitting ? 'Zapisywanie...' : 'Zapisz'}
            </Button>
          </div>
        </div>
      </div>
    </Modal>
  );
};

export default DrawFormModal;
