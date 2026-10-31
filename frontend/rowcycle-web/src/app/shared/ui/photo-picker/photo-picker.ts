import { Component, DestroyRef, inject, input, model, signal } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';

/** Same rules as the API (FR-08); checked here only to save a pointless upload. */
const MAX_PHOTO_BYTES = 10 * 1024 * 1024;
const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

/**
 * A large tap target that opens the camera or gallery, shows a preview, and rejects files that aren't
 * JPG/PNG/WEBP or are over 10 MB. Two-way bind the chosen file: `<app-photo-picker [(file)]="photo" />`.
 */
@Component({
  selector: 'app-photo-picker',
  imports: [MatIcon, TranslocoPipe],
  templateUrl: './photo-picker.html',
  styleUrl: './photo-picker.scss',
})
export class PhotoPicker {
  /** The chosen photo, or null. */
  readonly file = model<File | null>(null);
  /** A translation key the parent wants shown (e.g. "a photo is required"). The picker's own errors win. */
  readonly error = input<string | null>(null);

  protected readonly preview = signal<string | null>(null);
  protected readonly ownError = signal<string | null>(null);

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
      this.ownError.set('common.photo.wrongType');
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      this.ownError.set('common.photo.tooLarge');
      return;
    }
    this.ownError.set(null);
    this.file.set(file);
    this.setPreview(URL.createObjectURL(file));
  }

  /** Frees the previous preview's memory before showing a new one. */
  private setPreview(url: string | null): void {
    const old = this.preview();
    if (old) {
      URL.revokeObjectURL(old);
    }
    this.preview.set(url);
  }
}
