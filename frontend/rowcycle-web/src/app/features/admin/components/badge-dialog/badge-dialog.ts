import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminBadgesApi } from '../../../../core/api/badges-api';
import { Badge, BadgeRuleType } from '../../../../core/api/models';
import { SubmissionCategoriesAdminApi } from '../../../../core/api/submissions-api';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { FormDialog } from '../../../../shared/ui/form-dialog/form-dialog';

/** Create (no data) or edit (a badge) — closes with the saved badge. */
@Component({
  selector: 'app-badge-dialog',
  imports: [
    ReactiveFormsModule, MatFormField,
    MatLabel, MatHint, MatError, MatIcon, MatInput, MatSelect, MatOption, TranslocoPipe, FieldErrorPipe, FormDialog,
  ],
  templateUrl: './badge-dialog.html',
  styleUrl: './badge-dialog.scss',
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
