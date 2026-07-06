import * as React from 'react';

/**
 * Tabs — from @nicarunner/ui@0.1.0.
 */
export interface TabsProps {
  tabs: TabItem[];
  activeTab: string;
  onChange: (id: string) => void;
  className?: string;
}

export declare const Tabs: React.ComponentType<TabsProps>;
