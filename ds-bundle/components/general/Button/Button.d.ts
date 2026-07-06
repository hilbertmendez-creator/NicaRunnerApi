import * as React from 'react';

/**
 * Button — from @nicarunner/ui@0.1.0.
 * @replaces button
 */
export interface ButtonProps {
  variant?: "primary" | "secondary" | "destructive" | "info";
  size?: "sm" | "md";
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
}

export declare const Button: React.ComponentType<ButtonProps>;
