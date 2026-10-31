import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { TranslocoPipe } from '@jsverse/transloco';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, MatButton, TranslocoPipe, PageHeader],
  templateUrl: './not-found.html',
})
export class NotFound {}
