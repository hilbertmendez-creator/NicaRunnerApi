import * as React from 'react';

/**
 * Textarea — from @nicarunner/ui@0.1.0.
 * @replaces textarea
 */
export interface TextareaProps {
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
  /** Allows getting a ref to the component instance. Once the component unmounts, React will set `ref.current` to `null` (or  */
  ref?: React.Ref;
}

export declare const Textarea: React.ComponentType<TextareaProps>;
