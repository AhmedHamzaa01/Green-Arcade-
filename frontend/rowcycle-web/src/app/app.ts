import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { MatBadge } from '@angular/material/badge';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthSession } from './core/auth/auth-session';
import { CartStore } from './core/cart/cart-store';
import { BottomNav } from './layout/bottom-nav/bottom-nav';

/**
 * The app shell. Members and visitors get a slim top bar (links on wide screens) plus a bottom tab bar on phones.
 * The staff area (/admin) has its own layout with a side panel, so the member bars are hidden there.
 */
@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatButton, MatIconButton, MatIcon, MatBadge, MatMenu, MatMenuItem, MatMenuTrigger, MatDivider,
    TranslocoPipe, BottomNav,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly session = inject(AuthSession);
  protected readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  protected readonly inStaffArea = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map((e) => (e as NavigationEnd).urlAfterRedirects.startsWith('/admin')),
    ),
    { initialValue: this.router.url.startsWith('/admin') },
  );

  protected isStaff(): boolean {
    return this.session.hasRole('Admin', 'StoreManager', 'Moderator');
  }

  protected async logout(): Promise<void> {
    await this.session.logout();
    await this.router.navigate(['/']);
  }
}
