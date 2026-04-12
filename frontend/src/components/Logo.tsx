import React from 'react';
import logoPng from '../content/logo.png';

export function Logo({ width = 180, height = 'auto' }: { width?: number | string, height?: number | string, hideText?: boolean }) {
  return (
    <img 
      src={logoPng} 
      alt="PostPebble Logo" 
      style={{ width: typeof width === 'number' ? `${width}px` : width, height }} 
    />
  );
}
