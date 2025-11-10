/**
 * Generuje losowy zestaw 6 liczb z zakresu 1-49
 * Algorytm: Fisher-Yates shuffle
 * @returns Posortowana tablica 6 unikalnych liczb
 */
export function generateRandomNumbers(): number[] {
  const numbers = Array.from({ length: 49 }, (_, i) => i + 1);

  // Fisher-Yates shuffle
  for (let i = numbers.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [numbers[i], numbers[j]] = [numbers[j], numbers[i]];
  }

  // Zwróć pierwsze 6 liczb, posortowane
  return numbers.slice(0, 6).sort((a, b) => a - b);
}

/**
 * Generuje 9 zestawów systemowych pokrywających wszystkie liczby 1-49
 * Każda liczba pojawia się minimum raz
 * @returns Tablica 9 zestawów (każdy po 6 posortowanych liczb)
 */
export function generateSystemTickets(): number[][] {
  const tickets: number[][] = [];
  const pool = Array.from({ length: 49 }, (_, i) => i + 1);

  // Inicjalizacja 9 zestawów
  for (let i = 0; i < 9; i++) {
    tickets.push([]);
  }

  // Shuffle pool
  const shuffledPool = [...pool];
  for (let i = shuffledPool.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [shuffledPool[i], shuffledPool[j]] = [shuffledPool[j], shuffledPool[i]];
  }

  // Rozdziel 49 liczb w 9 zestawach (round-robin)
  let ticketIndex = 0;
  for (const number of shuffledPool) {
    tickets[ticketIndex].push(number);

    // Przejdź do następnego zestawu gdy obecny ma 6 liczb
    if (tickets[ticketIndex].length === 6) {
      ticketIndex++;
    }

    // Jeśli wszystkie zestawy mają po 6, wróć do pierwszego
    if (ticketIndex === 9) {
      ticketIndex = 0;
    }
  }

  // Dopełnij zestawy które mają < 6 liczb (ostatnie 5 pozycji)
  for (let i = 0; i < 9; i++) {
    while (tickets[i].length < 6) {
      // Losowa liczba z pool
      const randomNumber = pool[Math.floor(Math.random() * pool.length)];

      // Dodaj tylko jeśli nie jest duplikatem w tym zestawie
      if (!tickets[i].includes(randomNumber)) {
        tickets[i].push(randomNumber);
      }
    }

    // Sortuj dla czytelności
    tickets[i].sort((a, b) => a - b);
  }

  return tickets;
}

/**
 * Formatuje datę ISO 8601 do formatu YYYY-MM-DD HH:MM
 * @param isoDate - Data w formacie ISO 8601 (np. "2025-10-25T10:00:00Z")
 * @returns Sformatowana data (np. "2025-10-25 10:00")
 */
export function formatDate(isoDate: string): string {
  const date = new Date(isoDate);

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');

  return `${year}-${month}-${day} ${hours}:${minutes}`;
}

/**
 * Określa kolor licznika zestawów na podstawie procentowego wypełnienia
 * @param count - Aktualna liczba zestawów
 * @param max - Maksymalna liczba zestawów (default 100)
 * @returns Klasa Tailwind CSS dla koloru tekstu
 */
export function getCounterColor(count: number, max: number = 100): string {
  const percentage = (count / max) * 100;

  if (percentage <= 70) {
    return 'text-green-600';
  }

  if (percentage <= 90) {
    return 'text-yellow-600';
  }

  return 'text-red-600';
}
