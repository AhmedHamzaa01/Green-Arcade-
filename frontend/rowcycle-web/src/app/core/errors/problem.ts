import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormGroup } from '@angular/forms';
import { ProblemDetails } from '../api/api.models';

/** Reads the API's Problem Details from a failed call; falls back to a generic message. */
export function toProblem(error: unknown): ProblemDetails {
  if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
    return { status: error.status, ...(error.error as ProblemDetails) };
  }
  return { status: error instanceof HttpErrorResponse ? error.status : undefined };
}

/**
 * Puts the API's field errors (e.g. `errors.password`) on the matching form controls as `server` errors,
 * so they show under the right field. Returns true if any field got an error.
 */
export function applyServerErrors(form: FormGroup, problem: ProblemDetails): boolean {
  let applied = false;
  for (const [field, messages] of Object.entries(problem.errors ?? {})) {
    const control: AbstractControl | null = form.get(field);
    if (control && messages.length > 0) {
      control.setErrors({ ...control.errors, server: messages[0] });
      control.markAsTouched();
      applied = true;
    }
  }
  return applied;
}
