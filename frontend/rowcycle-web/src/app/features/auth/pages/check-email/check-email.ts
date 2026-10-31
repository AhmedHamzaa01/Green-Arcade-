import { Component, input, isDevMode } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

/** Shown after sign-up. */
@Component({
  selector: 'app-check-email',
  imports: [RouterLink, MatCard, MatCardContent, MatButton, MatIcon, TranslocoPipe, PageHeader],
  templateUrl: './check-email.html',
})
export class CheckEmail {
  readonly email = input<string>();
  protected readonly dev = isDevMode();
}
