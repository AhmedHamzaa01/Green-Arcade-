import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
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
import { SubmissionsApi } from '../../core/api/submissions-api';
import { AuthSession } from '../../core/auth/auth-session';
import { Notifier } from '../../core/errors/notifier';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** Same limit as the API (FR-08); checked here only to save a pointless upload. */
const MAX_PHOTO_BYTES = 10 * 1024 * 1024;
const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

/** /actions/new — submit a photo of an action for points (F3). */
@Component({
  selector: 'app-new-submission',
  imports: [
    ReactiveFormsModule, RouterLink, MatButton, MatCard, MatCardContent, MatFormField, MatLabel, MatHint, MatError, MatSuffix,
    MatIcon, MatInput, MatSelect, MatOption, TranslocoPipe, FieldErrorPipe, PageHeader, Loading,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'actions.new.title' | transloco" [subtitle]="'actions.new.subtitle' | transloco">
        <a mat-button routerLink="/actions/mine">{{ 'nav.myActions' | transloco }}</a>
      </app-page-header>

      @if (!session.user()?.emailVerified) {
        <mat-card><mat-card-content class="row negative"><mat-icon>warning</mat-icon>{{ 'actions.new.unverified' | transloco }}</mat-card-content></mat-card>
      } @else if (categories.value(); as list) {
        <mat-card>
          <mat-card-content>
            <form class="stack submission-form" [formGroup]="form" (ngSubmit)="submit()">
              <mat-form-field>
                <mat-label>{{ 'actions.new.category' | transloco }}</mat-label>
                <mat-select formControlName="categoryId">
                  @for (c of list; track c.id) {
                    <mat-option [value]="c.id">{{ c.name }} · {{ 'actions.points' | transloco: { points: c.pointsReward } }}</mat-option>
                  }
                </mat-select>
                <mat-error>{{ form.controls.categoryId | fieldError }}</mat-error>
              </mat-form-field>

              <div class="photo">
                @if (preview(); as url) {
                  <img [src]="url" [alt]="'actions.new.photo' | transloco" />
                }
                <div class="row">
                  <button mat-stroked-button type="button" (click)="fileInput.click()">
                    <mat-icon>photo_camera</mat-icon>{{ (photo() ? 'actions.new.changePhoto' : 'actions.new.choosePhoto') | transloco }}
                  </button>
                  <span class="muted small">{{ 'actions.new.photoHint' | transloco }}</span>
                </div>
                <input #fileInput hidden type="file" accept="image/jpeg,image/png,image/webp" (change)="pick(fileInput)" />
                @if (photoError()) {
                  <p class="form-error" role="alert">{{ photoError()! | transloco }}</p>
                }
              </div>

              @if (selectedCategory()?.tracksWeight) {
                <mat-form-field>
                  <mat-label>{{ 'actions.new.weight' | transloco }}</mat-label>
                  <input matInput type="number" min="0" step="0.1" formControlName="weightKg" />
                  <span matTextSuffix>kg</span>
                  <mat-hint>{{ 'actions.new.weightHint' | transloco }}</mat-hint>
                  <mat-error>{{ form.controls.weightKg | fieldError }}</mat-error>
                </mat-form-field>
              }

              <mat-form-field>
                <mat-label>{{ 'actions.new.note' | transloco }}</mat-label>
                <textarea matInput rows="3" formControlName="note"></textarea>
                <mat-error>{{ form.controls.note | fieldError }}</mat-error>
              </mat-form-field>

              @if (error()) {
                <p class="form-error" role="alert">{{ error() }}</p>
              }
              <div class="row">
                <button mat-flat-button type="submit" [disabled]="saving()">{{ 'actions.new.submit' | transloco }}</button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>
      } @else if (categories.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .submission-form { max-width: 520px; }
    .photo { display: flex; flex-direction: column; gap: 8px; margin-bottom: 16px; }
    .photo img { max-width: 100%; max-height: 320px; object-fit: contain; border-radius: 8px; align-self: flex-start; }
    .small { font-size: 12px; }
  `,
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

  protected readonly photo = signal<File | null>(null);
  protected readonly preview = signal<string | null>(null);
  protected readonly photoError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    inject(DestroyRef).onDestroy(() => this.setPreview(null));
  }

  protected pick(input: HTMLInputElement): void {
    const file = input.files?.[0] ?? null;
    input.value = '';   // picking the same file again still fires (change)
    if (!file) {
      return;
    }
    if (!PHOTO_TYPES.includes(file.type)) {
      this.photoError.set('actions.new.photoType');
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      this.photoError.set('actions.new.photoSize');
      return;
    }
    this.photoError.set(null);
    this.photo.set(file);
    this.setPreview(URL.createObjectURL(file));
  }

  protected async submit(): Promise<void> {
    const photo = this.photo();
    if (!photo) {
      this.photoError.set('actions.new.photoRequired');
    }
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

  private setPreview(url: string | null): void {
    const old = this.preview();
    if (old) {
      URL.revokeObjectURL(old);
    }
    this.preview.set(url);
  }
}
