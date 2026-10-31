import { Component, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIcon } from '@angular/material/icon';

/** A number with a label and an icon (dashboard, account, home). Becomes a link when `link` is set. */
@Component({
  selector: 'app-stat-card',
  imports: [NgTemplateOutlet, RouterLink, MatIcon],
  templateUrl: './stat-card.html',
  styleUrl: './stat-card.scss',
})
export class StatCard {
  readonly icon = input.required<string>();
  readonly value = input.required<string | number>();
  readonly label = input.required<string>();
  readonly link = input<string>();
  /** 'warn' highlights something that needs attention (e.g. low stock). */
  readonly tone = input<'default' | 'warn'>('default');
}
