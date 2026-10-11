import { Component, inject } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { MatBadge } from '@angular/material/badge';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { MatToolbar } from '@angular/material/toolbar';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthSession } from './core/auth/auth-session';
import { CartStore } from './core/cart/cart-store';

/** The app shell: top bar (links depend on the user's roles) and the current page. */
@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, NgTemplateOutlet,
    MatToolbar, MatButton, MatIconButton, MatIcon, MatBadge, MatMenu, MatMenuItem, MatMenuTrigger, MatDivider,
    TranslocoPipe,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly session = inject(AuthSession);
  protected readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  protected async logout(): Promise<void> {
    await this.session.logout();
    await this.router.navigate(['/']);
  }
}
