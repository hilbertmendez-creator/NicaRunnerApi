import * as React from 'react';

/**
 * DataTable — from @nicarunner/ui@0.1.0.
 * @replaces table
 */
export interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  rowKey: (row: T) => string | number;
  emptyState?: React.ReactNode;
  isLoading?: boolean;
  loadingMessage?: string;
}

export declare const DataTable: React.ComponentType<DataTableProps>;
