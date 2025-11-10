export interface User {
  id: number;
  email: string;
  token: string;
  isAdmin: boolean;
  expiresAt: string;
}