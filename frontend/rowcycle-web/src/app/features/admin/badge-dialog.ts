import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminBadgesApi } from '../../core/api/badges-api';
import { Badge, BadgeRuleType } from '../../core/api/api.models';
import { SubmissionCategoriesAdminApi } from '../../core/api/submissions-api';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';

/** Create (no data) or edit (a badge) — closes with the saved badge. */
@Component({
  selector: 'app-badge-dialog',
  imports: [
    ReactiveFormsModule, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, MatFormField,
    MatLabel, MatHint, MatError, MatIcon, MatInput, MatSelect, MatOption, TranslocoPipe, FieldErrorPipe,
  ],
  template: `
    <h2 mat-dialog-title>{{ (badge ? 'admin.badges.edit' : 'admin.badges.new') | transloco }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="stack">
        <mat-form-field>
          <mat-label>{{ 'admin.badges.name' | transloco }}</mat-label>
          <input matInput formControlName="name" />
          <mat-error>{{ form.controls.name | fieldError }}</mat-error>
        </mat-form-field>

        <div>
          <div class="muted label">{{ 'admin.badges.icon' | transloco }}</div>
          <div class="icons" role="radiogroup" [attr.aria-label]="'admin.badges.icon' | transloco">
            @for (icon of icons.value() ?? []; track icon) {
              <button type="button" class="icon" role="radio" [class.selected]="selectedIcon() === icon"
                      [attr.aria-checked]="selectedIcon() === icon" [attr.aria-label]="icon" (click)="form.controls.icon.setValue(icon)">
                <mat-icon>{{ icon }}</mat-icon>
              </button>
            }
          </div>
          @if (form.controls.icon.errors?.['server']; as message) {
            <p class="form-error">{{ message }}</p>
          }
        </div>

        <mat-form-field>
          <mat-label>{{ 'admin.badges.rule' | transloco }}</mat-label>
          <mat-select formControlName="ruleType">
            @for (rule of rules; track rule) {
              <mat-option [value]="rule">{{ 'admin.badges.rules.' + rule | transloco }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        @if (ruleType() === 'CategoryApproved') {
          <mat-form-field>
            <mat-label>{{ 'admin.badges.category' | transloco }}</mat-label>
            <mat-select formControlName="ruleCategoryId">
              @for (c of categories.value() ?? []; track c.id) {
                <mat-option [value]="c.id">{{ c.name }}</mat-option>
              }
            </mat-select>
            <mat-error>{{ form.controls.ruleCategoryId | fieldError }}</mat-error>
          </mat-form-field>
        }

        @if (ruleType() !== 'FirstOrder') {
          <mat-form-field>
            <mat-label>{{ 'admin.badges.threshold' | transloco }}</mat-label>
            <input matInput type="number" min="1" max="10000" formControlName="threshold" />
            <mat-hint>{{ 'admin.badges.thresholdHint' | transloco }}</mat-hint>
            <mat-error>{{ form.controls.threshold | fieldError }}</mat-error>
          </mat-form-field>
        } @else {
          <p class="muted">{{ 'admin.badges.firstOrderHint' | transloco }}</p>
        }

        @if (!badge) {
          <p class="muted small">{{ 'admin.badges.newHint' | transloco }}</p>
        }
        @if (error()) {
          <p class="form-error" role="alert">{{ error() }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>{{ 'common.cancel' | transloco }}</button>
        <button mat-flat-button type="submit" [disabled]="saving()">{{ 'common.save' | transloco }}</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .label { font-size: 12px; margin-bottom: 4px; }
    .icons { display: grid; grid-template-columns: repeat(auto-fill, 44px); gap: 6px; }
    .icon { width: 44px; height: 44px; display: grid; place-items: center; cursor: pointer; border-radius: 8px;
            border: 1px solid var(--mat-sys-outline-variant); background: none; color: var(--mat-sys-on-surface); }
    .icon.selected { border: 2px solid var(--mat-sys-primary); background: var(--mat-sys-primary-container);
                     color: var(--mat-sys-on-primary-container); }
    .small { font-size: 12px; }
  `,
})
export class BadgeDialog {
  private readonly api = inject(AdminBadgesApi);
  private readonly categoriesApi = inject(SubmissionCategoriesAdminApi);
  private readonly ref = inject(MatDialogRef<BadgeDialog, Badge>);
  private readonly transloco = inject(TranslocoService);
  protected readonly badge = inject<Badge | null>(MAT_DIALOG_DATA);

  protected readonly rules: BadgeRuleType[] = ['TotalApproved', 'CategoryApproved', 'FirstOrder'];
  protected readonly icons = rxResource({ stream: () => this.api.icons() });
  protected readonly categories = rxResource({ stream: () => this.categoriesApi.list() });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: [this.badge?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    icon: [this.badge?.icon ?? 'emoji_events', Validators.required],
    ruleType: [this.badge?.ruleType ?? ('TotalApproved' as BadgeRuleType), Validators.required],
    ruleCategoryId: [this.badge?.ruleCategoryId ?? ''],
    threshold: [this.badge?.threshold ?? 1, [Validators.required, Validators.min(1), Validators.max(10000)]],
  });
  protected readonly selectedIcon = toSignal(this.form.controls.icon.valueChanges, { initialValue: this.form.controls.icon.value });
  protected readonly ruleType = toSignal(this.form.controls.ruleType.valueChanges, { initialValue: this.form.controls.ruleType.value });
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async save(): Promise<void> {
    const value = this.form.getRawValue();
    if (value.ruleType === 'CategoryApproved' && !value.ruleCategoryId) {
      this.form.controls.ruleCategoryId.setErrors({ required: true });
    }
    // Fields the chosen rule doesn't show can't block saving.
    const thresholdOk = value.ruleType === 'FirstOrder' || this.form.controls.threshold.valid;
    if (!this.form.controls.name.valid || !thresholdOk || this.form.controls.ruleCategoryId.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const request = {
      name: value.name.trim(),
      icon: value.icon,
      ruleType: value.ruleType,
      ruleCategoryId: value.ruleType === 'CategoryApproved' ? value.ruleCategoryId : null,
      threshold: value.ruleType === 'FirstOrder' ? 1 : value.threshold,
    };
    try {
      const saved = await firstValueFrom(this.badge ? this.api.update(this.badge.id, request) : this.api.create(request));
      this.ref.close(saved);
    } catch (e) {
      const problem = toProblem(e);
      if (!applyServerErrors(this.form, problem)) {
        this.error.set(problem.detail ?? problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.saving.set(false);
    }
  }
}
