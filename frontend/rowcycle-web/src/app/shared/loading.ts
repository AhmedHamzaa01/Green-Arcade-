import { Component } from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading',
  imports: [MatProgressSpinner],
  template: `<div class="loading"><mat-progress-spinner mode="indeterminate" diameter="40" /></div>`,
  styles: `.loading { display: flex; justify-content: center; padding: 32px; }`,
})
export class Loading {}
