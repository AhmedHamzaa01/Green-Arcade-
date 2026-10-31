import { Component, OnInit, inject } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { BadgesApi } from '../../../../core/api/badges-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { StatCard } from '../../../../shared/ui/stat-card/stat-card';

/**
 * /account/profile — the account hub (and the phone's "Account" tab): who you are, your points and badges,
 * and links to everything else about your account. Uses GET /me and /me/badges.
 */
@Component({
  selector: 'app-profile',
  imports: [RouterLink, DatePipe, DecimalPipe, MatCard, MatCardContent, MatButton, MatIcon, TranslocoPipe, StatCard],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  protected readonly session = inject(AuthSession);
  private readonly router = inject(Router);
  private readonly badgesApi = inject(BadgesApi);
  protected readonly badges = rxResource({ stream: () => this.badgesApi.mine() });

  ngOnInit(): void {
    // Fresh data each visit (e.g. the points balance may have changed).
    void this.session.reloadUser();
  }

  protected async logout(): Promise<void> {
    await this.session.logout();
    await this.router.navigate(['/']);
  }
}
