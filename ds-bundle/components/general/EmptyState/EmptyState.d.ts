import * as React from 'react';

/**
 * EmptyState — from @nicarunner/ui@0.1.0.
 */
export interface EmptyStateProps {
  message: string;
  className?: string;
  action?: { label: string; onClick: () => void; };
}

export declare const EmptyState: React.ComponentType<EmptyStateProps>;
