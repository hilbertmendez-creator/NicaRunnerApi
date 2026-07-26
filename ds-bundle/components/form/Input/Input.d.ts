import * as React from 'react';

/**
 * Input — from @nicarunner/ui@0.1.0.
 * @replaces input
 */
export interface InputProps {
  invalid?: boolean;
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
}

export declare const Input: React.ComponentType<InputProps>;
