import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { TranslocoPipe } from '@jsverse/transloco';
import { PageHeader } from '../shared/page-header';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, MatButton, TranslocoPipe, PageHeader],
  template: `
    <div class="page-narrow">
      <app-page-header [title]="'notFound.title' | transloco" />
      <a mat-flat-button routerLink="/">{{ 'notFound.home' | transloco }}</a>
    </div>
  `,
})
export class NotFound {}
