import * as React from 'react';

/**
 * Select — from @nicarunner/ui@0.1.0.
 * @replaces select
 */
export interface SelectProps {
  invalid?: boolean;
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
}

export declare const Select: React.ComponentType<SelectProps>;
