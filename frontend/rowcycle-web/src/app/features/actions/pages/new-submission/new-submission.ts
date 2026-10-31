import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatError, MatFormField, MatHint, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { SubmissionsApi } from '../../../../core/api/submissions-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { Notifier } from '../../../../core/errors/notifier';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { PhotoPicker } from '../../../../shared/ui/photo-picker/photo-picker';

/** /actions/new — submit a photo of an action for points (F3). */
@Component({
  selector: 'app-new-submission',
  imports: [
    ReactiveFormsModule, RouterLink, MatButton, MatCard, MatCardContent, MatFormField, MatLabel, MatHint, MatError, MatSuffix,
    MatIcon, MatInput, MatSelect, MatOption, TranslocoPipe, FieldErrorPipe, PageHeader, Loading, PhotoPicker,
  ],
  templateUrl: './new-submission.html',
  styleUrl: './new-submission.scss',
})
export class NewSubmission {
  private readonly api = inject(SubmissionsApi);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);
  protected readonly session = inject(AuthSession);

  protected readonly categories = rxResource({ stream: () => this.api.categories() });
  protected readonly form = inject(FormBuilder).group({
    categoryId: ['', Validators.required],
    weightKg: [null as number | null, [Validators.min(0), Validators.max(10000)]],
    note: ['', Validators.maxLength(1000)],
  });

  private readonly categoryId = toSignal(this.form.controls.categoryId.valueChanges, { initialValue: '' });
  protected readonly selectedCategory = computed(() => this.categories.value()?.find((c) => c.id === this.categoryId()));

  /** Set by the photo picker (which also checks type and size). */
  protected readonly photo = signal<File | null>(null);
  /** Shown by the picker when the member submits without a photo. */
  protected readonly photoError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    const photo = this.photo();
    this.photoError.set(photo ? null : 'actions.new.photoRequired');
    if (this.form.invalid || !photo) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    try {
      await firstValueFrom(this.api.create({
        categoryId: value.categoryId!,
        note: value.note?.trim() || null,
        // Only sent for actions that record weight; the API refuses it otherwise.
        weightKg: this.selectedCategory()?.tracksWeight && value.weightKg !== null ? value.weightKg : null,
      }, photo));
      this.notifier.success('actions.new.sent');
      await this.router.navigate(['/actions/mine']);
    } catch (err) {
      const problem = toProblem(err);
      if (!applyServerErrors(this.form, problem)) {
        this.error.set(problem.detail ?? problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.saving.set(false);
    }
  }
}
