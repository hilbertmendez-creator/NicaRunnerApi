import * as React from 'react';

/**
 * Textarea — from @nicarunner/ui@0.1.0.
 * @replaces textarea
 */
export interface TextareaProps {
  invalid?: boolean;
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
}

export declare const Textarea: React.ComponentType<TextareaProps>;
