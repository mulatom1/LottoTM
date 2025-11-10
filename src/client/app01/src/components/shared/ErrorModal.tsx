import React from 'react';
import Modal from './modal';
import Button from './button';
import type { ErrorModalProps } from '../../types/tickets';

const ErrorModal: React.FC<ErrorModalProps> = ({ isOpen, onClose, errors }) => {
  const errorArray = Array.isArray(errors) ? errors : [errors];

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Błąd" size="sm">
      <div className="mb-6">
        <ul className="list-disc list-inside text-red-600 space-y-1">
          {errorArray.map((error, index) => (
            <li key={index}>{error}</li>
          ))}
        </ul>
      </div>
      <div className="flex justify-end">
        <Button onClick={onClose} variant="primary">
          Zamknij
        </Button>
      </div>
    </Modal>
  );
};

export default ErrorModal;
