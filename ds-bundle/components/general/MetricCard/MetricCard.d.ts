import * as React from 'react';

/**
 * MetricCard — from @nicarunner/ui@0.1.0.
 */
export interface MetricCardProps {
  label: string;
  value: string | number;
  variant?: "gray" | "orange" | "teal" | "amber" | "red";
  size?: "sm" | "md";
  className?: string;
}

export declare const MetricCard: React.ComponentType<MetricCardProps>;
