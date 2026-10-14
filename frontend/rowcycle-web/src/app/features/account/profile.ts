import { Component, OnInit, inject } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatChip, MatChipSet } from '@angular/material/chips';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { BadgesApi } from '../../core/api/badges-api';
import { AuthSession } from '../../core/auth/auth-session';
import { PageHeader } from '../../shared/page-header';

/** GET /me and /me/badges: name, email, verification, points, roles and badges. */
@Component({
  selector: 'app-profile',
  imports: [RouterLink, DatePipe, DecimalPipe, MatCard, MatCardContent, MatButton, MatIcon, MatChipSet, MatChip, TranslocoPipe, PageHeader],
  template: `
    @if (session.user(); as user) {
      <div class="page">
        <app-page-header [title]="'account.profile.title' | transloco" />
        <div class="grid">
          <mat-card>
            <mat-card-content class="stack">
              <div class="row">
                <mat-icon class="avatar">account_circle</mat-icon>
                <div>
                  <div class="name">{{ user.fullName }}</div>
                  <div class="muted">{{ user.email }}</div>
                </div>
              </div>
              @if (user.emailVerified) {
                <span class="row positive"><mat-icon>verified</mat-icon>{{ 'account.profile.verified' | transloco }}</span>
              } @else {
                <div class="unverified">
                  <span class="row negative"><mat-icon>warning</mat-icon>{{ 'account.profile.notVerified' | transloco }}</span>
                  <p class="muted">{{ 'account.profile.notVerifiedHint' | transloco }}</p>
                </div>
              }
              <div>
                <div class="muted">{{ 'account.profile.roles' | transloco }}</div>
                <mat-chip-set>
                  @for (role of user.roles; track role) {
                    <mat-chip>{{ role }}</mat-chip>
                  }
                </mat-chip-set>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-content class="stack">
              <div class="muted">{{ 'account.profile.points' | transloco }}</div>
              <div class="balance">{{ user.pointsBalance | number }}</div>
              <a mat-button routerLink="/account/points">{{ 'account.profile.viewPoints' | transloco }}</a>
            </mat-card-content>
          </mat-card>

          <!-- F4: earned badges, newest first -->
          <mat-card class="badges">
            <mat-card-content class="stack">
              <div class="muted">{{ 'account.profile.badges' | transloco }}</div>
              @if (badges.value(); as list) {
                @if (list.length === 0) {
                  <p class="muted">{{ 'account.profile.noBadges' | transloco }} <a routerLink="/actions/new">{{ 'nav.newAction' | transloco }}</a></p>
                } @else {
                  <ul class="badge-list">
                    @for (b of list; track b.id) {
                      <li>
                        <mat-icon class="badge-icon">{{ b.icon }}</mat-icon>
                        <div>
                          <div>{{ b.name }}</div>
                          <div class="muted small">{{ b.awardedAt | date: 'mediumDate' }}</div>
                        </div>
                      </li>
                    }
                  </ul>
                }
              }
            </mat-card-content>
          </mat-card>
        </div>
      </div>
    }
  `,
  styles: `
    .grid { display: grid; gap: 16px; }
    @media (min-width: 720px) { .grid { grid-template-columns: 2fr 1fr; } }
    .avatar { font-size: 48px; width: 48px; height: 48px; color: var(--mat-sys-primary); }
    .name { font: var(--mat-sys-title-medium); }
    .balance { font: var(--mat-sys-display-small); color: var(--mat-sys-primary); }
    .unverified p { margin: 4px 0 0; }
    .badges { grid-column: 1 / -1; }
    .badge-list { list-style: none; margin: 0; padding: 0; display: grid; gap: 12px; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); }
    .badge-list li { display: flex; gap: 10px; align-items: center; }
    .badge-icon { font-size: 36px; width: 36px; height: 36px; color: var(--mat-sys-primary); }
    .small { font-size: 12px; }
  `,
})
export class Profile implements OnInit {
  protected readonly session = inject(AuthSession);
  private readonly badgesApi = inject(BadgesApi);
  protected readonly badges = rxResource({ stream: () => this.badgesApi.mine() });

  ngOnInit(): void {
    // Fresh data each visit (e.g. the points balance may have changed).
    void this.session.reloadUser();
  }
}
