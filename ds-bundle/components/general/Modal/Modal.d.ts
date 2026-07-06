import * as React from 'react';

/**
 * Modal — from @nicarunner/ui@0.1.0.
 * @replaces dialog
 */
export interface ModalProps {
  onClose: () => void;
  children: React.ReactNode;
  maxWidth?: "md" | "lg";
  labelledBy?: string;
}

export declare const Modal: React.ComponentType<ModalProps>;
