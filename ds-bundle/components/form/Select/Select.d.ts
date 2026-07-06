import * as React from 'react';

/**
 * Select — from @nicarunner/ui@0.1.0.
 * @replaces select
 */
export interface SelectProps {
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
  /** Allows getting a ref to the component instance. Once the component unmounts, React will set `ref.current` to `null` (or  */
  ref?: React.Ref;
}

export declare const Select: React.ComponentType<SelectProps>;
