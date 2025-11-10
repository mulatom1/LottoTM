/**
 * Walidacja pojedynczego pola NumberInput
 * @param _index - Indeks pola w tablicy numbers (nieużywany, ale potrzebny dla spójności API)
 * @param value - Wartość pola (number lub pusty string)
 * @param numbers - Cała tablica liczb
 * @returns Komunikat błędu lub undefined jeśli OK
 */
export function validateField(
  _index: number,
  value: number | '',
  numbers: (number | '')[]
): string | undefined {
  if (value === '') {
    return 'To pole jest wymagane';
  }

  if (value < 1 || value > 49) {
    return 'Liczba musi być w zakresie 1-49';
  }

  // Sprawdzenie duplikatu
  const duplicateCount = numbers.filter(n => n === value).length;
  if (duplicateCount > 1) {
    return 'Liczby w zestawie muszą być unikalne';
  }

  return undefined;
}

/**
 * Walidacja całej tablicy liczb przed submitem
 * @param numbers - Tablica 6 liczb (lub pustych stringów)
 * @returns Tablica komunikatów błędów
 */
export function validateNumbers(numbers: (number | '')[]): string[] {
  const errors: string[] = [];

  // Sprawdzenie wypełnienia
  if (numbers.some(n => n === '')) {
    errors.push('Wszystkie pola są wymagane');
  }

  // Filtrowanie tylko wypełnionych liczb dla dalszej walidacji
  const validNumbers = numbers.filter(n => n !== '') as number[];

  // Sprawdzenie zakresu
  if (validNumbers.some(n => n < 1 || n > 49)) {
    errors.push('Liczby muszą być w zakresie 1-49');
  }

  // Sprawdzenie unikalności
  const uniqueNumbers = new Set(validNumbers);
  if (uniqueNumbers.size !== validNumbers.length) {
    errors.push('Liczby w zestawie muszą być unikalne');
  }

  return errors;
}
