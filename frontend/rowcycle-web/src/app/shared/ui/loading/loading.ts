import { Component } from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading',
  imports: [MatProgressSpinner],
  templateUrl: './loading.html',
  styleUrl: './loading.scss',
})
export class Loading {}
