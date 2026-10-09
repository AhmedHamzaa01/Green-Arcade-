import { Pipe, PipeTransform, inject } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { TranslocoService } from '@jsverse/transloco';

/**
 * The message for a form control's first error: `{{ form.controls.email | fieldError }}`.
 * Client errors use `validation.*` texts; `server` errors are the API's own message.
 * Impure because a control's errors change without the control object changing.
 */
@Pipe({ name: 'fieldError', pure: false })
export class FieldErrorPipe implements PipeTransform {
  private readonly transloco = inject(TranslocoService);

  transform(control: AbstractControl | null): string {
    const errors = control?.errors;
    if (!errors) {
      return '';
    }

    const [key, value] = Object.entries(errors)[0];
    if (key === 'server') {
      return value as string;
    }
    return this.transloco.translate(`validation.${key}`, typeof value === 'object' ? value : {});
  }
}
