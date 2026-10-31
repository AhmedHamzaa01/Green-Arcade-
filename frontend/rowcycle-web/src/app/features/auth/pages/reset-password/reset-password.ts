import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AuthApi } from '../../../../core/api/auth-api';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { passwordValidators } from '../../../../shared/validators/validators';

/** Opened from the email link: /reset-password?email=…&token=… */
@Component({
  selector: 'app-reset-password',
  imports: [
    ReactiveFormsModule, RouterLink, MatCard, MatCardContent, MatFormField, MatLabel, MatHint, MatInput, MatError,
    MatButton, TranslocoPipe, FieldErrorPipe, PageHeader,
  ],
  templateUrl: './reset-password.html',
})
export class ResetPassword {
  private readonly api = inject(AuthApi);
  private readonly transloco = inject(TranslocoService);

  readonly email = input<string>();
  readonly token = input<string>();

  protected readonly form = inject(FormBuilder).nonNullable.group({ newPassword: ['', passwordValidators] });
  protected readonly submitting = signal(false);
  protected readonly done = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.api.resetPassword({
        email: this.email()!,
        token: this.token()!,
        newPassword: this.form.getRawValue().newPassword,
      }));
      this.done.set(true);
    } catch (e) {
      const problem = toProblem(e);
      if (!applyServerErrors(this.form, problem)) {
        // e.g. "Invalid or expired link."
        this.error.set(problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
