'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LogoutButton } from '@/components/logout-button';
import { getAllowedNavigation } from '@/lib/access-control';
import { useCurrentUser } from '@/hooks/use-current-user';
import type { CurrentUser } from '@/types/current-user';

type AppShellProps = {
  initialUser: CurrentUser;
  pageTitle: string;
  pageDescription?: string;
  children: React.ReactNode;
};

export function AppShell({ initialUser, pageTitle, pageDescription, children }: AppShellProps) {
  const pathname = usePathname();
  const { user } = useCurrentUser(initialUser);
  const activeUser = user ?? initialUser;
  const navigation = getAllowedNavigation(activeUser);

  return (
    <main className="portal-shell">
      <aside className="portal-sidebar">
        <div className="sidebar-brand">
          <h1>Employee Portal</h1>
          <p className="small">Sprint 2 shell + workflow modules</p>
        </div>

        <nav className="sidebar-nav" aria-label="Primary">
          {navigation.map((item) => {
            const isActive = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={isActive ? 'nav-link active' : 'nav-link'}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>

      <section className="portal-main">
        <header className="portal-header card">
          <div>
            <h2>{pageTitle}</h2>
            {pageDescription ? <p className="small">{pageDescription}</p> : null}
          </div>
          <div className="header-actions">
            <div className="user-chip">
              <strong>{activeUser.displayName}</strong>
              <span className="small">{activeUser.roles.join(', ') || 'No roles assigned'}</span>
            </div>
            <LogoutButton />
          </div>
        </header>

        <section>{children}</section>
      </section>
    </main>
  );
}
