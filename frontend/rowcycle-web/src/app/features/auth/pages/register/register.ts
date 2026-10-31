import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
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

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule, RouterLink, MatCard, MatCardContent, MatFormField, MatLabel, MatHint, MatInput, MatError,
    MatButton, TranslocoPipe, FieldErrorPipe, PageHeader,
  ],
  templateUrl: './register.html',
})
export class Register {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', passwordValidators],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    try {
      const value = this.form.getRawValue();
      await firstValueFrom(this.api.register(value));
      await this.router.navigate(['/check-email'], { queryParams: { email: value.email } });
    } catch (e) {
      const problem = toProblem(e);
      if (!applyServerErrors(this.form, problem)) {
        // e.g. 409 "Email already registered."
        this.error.set(problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
