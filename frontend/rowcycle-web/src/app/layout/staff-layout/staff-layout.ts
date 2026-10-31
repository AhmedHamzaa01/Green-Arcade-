import { Component, computed, inject, viewChild } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from '@angular/material/sidenav';
import { TranslocoPipe } from '@jsverse/transloco';
import { Role } from '../../core/api/models';
import { AuthSession } from '../../core/auth/auth-session';

interface StaffLink {
  path: string;
  icon: string;
  label: string;
  roles: Role[];
}

interface StaffGroup {
  label: string;
  links: StaffLink[];
}

/** Every staff page, grouped. Each link shows only to the roles that may open it (the API checks again). */
const GROUPS: StaffGroup[] = [
  {
    label: 'staff.groups.overview',
    links: [{ path: '/admin/dashboard', icon: 'space_dashboard', label: 'nav.dashboard', roles: ['Admin', 'StoreManager', 'Moderator'] }],
  },
  {
    label: 'staff.groups.moderation',
    links: [{ path: '/admin/submissions', icon: 'fact_check', label: 'nav.submissions', roles: ['Admin', 'Moderator'] }],
  },
  {
    label: 'staff.groups.store',
    links: [
      { path: '/admin/orders', icon: 'local_shipping', label: 'nav.staffOrders', roles: ['Admin', 'StoreManager'] },
      { path: '/admin/products', icon: 'inventory_2', label: 'nav.products', roles: ['Admin', 'StoreManager'] },
      { path: '/admin/categories', icon: 'category', label: 'nav.categories', roles: ['Admin', 'StoreManager'] },
    ],
  },
  {
    label: 'staff.groups.admin',
    links: [
      { path: '/admin/users', icon: 'group', label: 'nav.users', roles: ['Admin'] },
      { path: '/admin/submission-categories', icon: 'checklist', label: 'nav.actionCategories', roles: ['Admin'] },
      { path: '/admin/badges', icon: 'emoji_events', label: 'nav.badges', roles: ['Admin'] },
      { path: '/admin/settings', icon: 'tune', label: 'nav.settings', roles: ['Admin'] },
      { path: '/admin/audit-log', icon: 'history', label: 'nav.auditLog', roles: ['Admin'] },
    ],
  },
];

/**
 * The staff area's frame: a side panel with the staff pages, always open on wide screens
 * and a slide-in drawer (☰) on phones that closes after a page is picked.
 */
@Component({
  selector: 'app-staff-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatIconButton, MatIcon, MatSidenav, MatSidenavContainer, MatSidenavContent, TranslocoPipe],
  templateUrl: './staff-layout.html',
  styleUrl: './staff-layout.scss',
})
export class StaffLayout {
  protected readonly session = inject(AuthSession);
  private readonly router = inject(Router);
  private readonly drawer = viewChild.required<MatSidenav>('drawer');

  protected readonly wide = toSignal(inject(BreakpointObserver).observe('(min-width: 1024px)').pipe(map((s) => s.matches)), {
    initialValue: false,
  });

  /** Only groups with at least one link this user may open. */
  protected readonly groups = computed(() => {
    const roles = this.session.roles();
    return GROUPS.map((g) => ({ ...g, links: g.links.filter((l) => l.roles.some((r) => roles.includes(r))) })).filter(
      (g) => g.links.length > 0,
    );
  });

  protected closeOnPhone(): void {
    if (!this.wide()) {
      void this.drawer().close();
    }
  }

  protected async logout(): Promise<void> {
    await this.session.logout();
    await this.router.navigate(['/']);
  }
}
