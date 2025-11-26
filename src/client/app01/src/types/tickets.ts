// DTO Types - API Contracts

export interface Ticket {
  id: number;
  userId: number;
  groupName: string;
  numbers: number[];
  createdAt: string;
}

export interface GetTicketsResponse {
  tickets: Ticket[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  limit: number;
}

export interface TicketRequest {
  groupName?: string;
  numbers: number[];
}

export interface TicketResponse {
  message: string;
}

export interface UpdateTicketResponse {
  message: string;
}

export interface DeleteTicketResponse {
  message: string;
}

export interface GenerateRandomResponse {
  message: string;
}

export interface GenerateSystemResponse {
  message: string;
}

export interface ApiErrorResponse {
  errors?: {
    [field: string]: string[];
  };
  error?: string;
}

// ViewModel Types - Local UI State

export interface TicketFormState {
  mode: 'add' | 'edit';
  initialNumbers?: number[];
  initialGroupName?: string;
  ticketId?: number;
}

export interface GeneratorState {
  type: 'random' | 'system';
  numbers: number[] | number[][];
}

export interface DeleteModalState {
  ticket: Ticket | null;
}

export interface ToastState {
  message: string;
  variant: 'success' | 'error' | 'warning';
  visible: boolean;
}

// Component Props Interfaces

export interface TicketCounterProps {
  count: number;
  max?: number;
}

export interface TicketListProps {
  tickets: Ticket[];
  loading: boolean;
  onEdit: (ticketId: number) => void;
  onDelete: (ticketId: number) => void;
  searchTerm?: string;
  onClearSearch?: () => void;
}

export interface TicketItemProps {
  ticket: Ticket;
  onEdit: () => void;
  onDelete: () => void;
}

export interface TicketFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: 'add' | 'edit';
  initialNumbers?: number[];
  initialGroupName?: string;
  ticketId?: number;
  onSubmit: (numbers: number[], groupName: string, ticketId?: number) => Promise<void>;
}

export interface GeneratorPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  numbers: number[];
  onRegenerate: () => void;
  onSave: (numbers: number[], groupName: string) => Promise<void>;
}

export interface GeneratorSystemPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  tickets: number[][];
  onRegenerate: () => void;
  onSaveAll: (tickets: number[][], groupName: string) => Promise<void>;
}

export interface DeleteConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  ticket: Ticket;
  onConfirm: (ticketId: number) => Promise<void>;
}

// Shared Components Props

export interface NumberInputProps {
  label: string;
  value: number | '';
  onChange: (value: number | '') => void;
  error?: string;
  min?: number;
  max?: number;
  required?: boolean;
}

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

export interface ErrorModalProps {
  isOpen: boolean;
  onClose: () => void;
  errors: string[] | string;
}

export interface ToastProps {
  message: string;
  variant: 'success' | 'error' | 'warning';
  duration?: number;
  onClose?: () => void;
}

export interface ButtonProps {
  variant: 'primary' | 'secondary' | 'danger';
  onClick?: () => void;
  disabled?: boolean;
  className?: string;
  children: React.ReactNode;
  type?: 'button' | 'submit' | 'reset';
}
