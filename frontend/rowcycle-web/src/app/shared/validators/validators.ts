import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

/** FR-01: at least 8 characters with a letter and a digit (same rule as the API). */
export const passwordValidators: ValidatorFn[] = [
  Validators.required,
  Validators.minLength(8),
  (control: AbstractControl): ValidationErrors | null =>
    control.value && !/[A-Za-z]/.test(control.value) ? { passwordLetter: true } : null,
  (control: AbstractControl): ValidationErrors | null =>
    control.value && !/[0-9]/.test(control.value) ? { passwordDigit: true } : null,
];
