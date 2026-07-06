import * as React from 'react';

/**
 * Input — from @nicarunner/ui@0.1.0.
 * @replaces input
 */
export interface InputProps {
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
  /** Allows getting a ref to the component instance. Once the component unmounts, React will set `ref.current` to `null` (or  */
  ref?: React.Ref;
}

export declare const Input: React.ComponentType<InputProps>;
