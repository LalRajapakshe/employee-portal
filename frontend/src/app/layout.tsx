import './globals.css';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Employee Portal',
  description: 'Sprint 1 foundation scaffold',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
