import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthApi } from '../../../../core/api/auth-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

/** Opened from the email link: /verify-email?userId=…&token=… */
@Component({
  selector: 'app-verify-email',
  imports: [RouterLink, MatCard, MatCardContent, MatButton, MatIcon, TranslocoPipe, PageHeader, Loading],
  templateUrl: './verify-email.html',
})
export class VerifyEmail implements OnInit {
  private readonly api = inject(AuthApi);
  protected readonly session = inject(AuthSession);

  readonly userId = input<string>();
  readonly token = input<string>();
  protected readonly state = signal<'working' | 'success' | 'failed'>('working');

  async ngOnInit(): Promise<void> {
    const userId = this.userId();
    const token = this.token();
    if (!userId || !token) {
      this.state.set('failed');
      return;
    }

    try {
      await firstValueFrom(this.api.verifyEmail({ userId, token }));
      this.state.set('success');
      if (this.session.isLoggedIn()) {
        // Get a token that says "email verified" and refresh the profile.
        await firstValueFrom(this.session.refreshAccessToken());
        await this.session.reloadUser();
      }
    } catch {
      this.state.set('failed');
    }
  }
}
