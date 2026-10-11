import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Address } from '../../core/api/api.models';
import { ShopApi } from '../../core/api/shop-api';
import { Notifier } from '../../core/errors/notifier';
import { confirm } from '../../shared/confirm-dialog';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { AddressDialog } from './address-dialog';

/** /account/addresses */
@Component({
  selector: 'app-addresses',
  imports: [MatButton, MatIconButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, PageHeader, Loading, EmptyState],
  template: `
    <div class="page">
      <app-page-header [title]="'addresses.title' | transloco" [subtitle]="'addresses.subtitle' | transloco">
        <button mat-flat-button class="add" (click)="edit(null)"><mat-icon>add</mat-icon>{{ 'addresses.add' | transloco }}</button>
      </app-page-header>
      @if (addresses.value(); as list) {
        @if (list.length === 0) {
          <app-empty-state icon="home" [message]="'addresses.empty' | transloco" />
        } @else {
          <div class="grid">
            @for (a of list; track a.id) {
              <mat-card [class.default]="a.isDefault">
                <mat-card-content>
                  <div class="row between">
                    <strong>{{ a.fullName }}</strong>
                    @if (a.isDefault) { <span class="badge">{{ 'addresses.default' | transloco }}</span> }
                  </div>
                  <div>{{ a.phone }}</div>
                  <div class="muted">{{ a.street }}, {{ a.building }}</div>
                  <div class="muted">{{ a.area }}, {{ a.city }}</div>
                  @if (a.notes) { <div class="muted small">{{ a.notes }}</div> }
                  <div class="row end">
                    <button matIconButton (click)="edit(a)" [attr.aria-label]="'common.edit' | transloco"><mat-icon>edit</mat-icon></button>
                    <button matIconButton (click)="remove(a)" [attr.aria-label]="'common.delete' | transloco"><mat-icon>delete</mat-icon></button>
                  </div>
                </mat-card-content>
              </mat-card>
            }
          </div>
        }
      } @else {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .add { margin-top: 12px; }
    .grid { display: grid; gap: 16px; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); }
    mat-card.default { outline: 2px solid var(--mat-sys-primary); }
    .between { justify-content: space-between; }
    .end { justify-content: flex-end; }
    .small { font-size: 12px; }
    .badge { font: var(--mat-sys-label-small); padding: 2px 8px; border-radius: 10px; background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container); }
  `,
})
export class Addresses {
  private readonly api = inject(ShopApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  protected readonly addresses = rxResource({ stream: () => this.api.addresses() });

  protected async edit(address: Address | null): Promise<void> {
    const saved = await firstValueFrom(this.dialog.open(AddressDialog, { data: address, width: '560px' }).afterClosed());
    if (saved) {
      this.notifier.success('addresses.saved');
      this.addresses.reload();
    }
  }

  protected async remove(address: Address): Promise<void> {
    if (!(await confirm(this.dialog, { message: this.transloco.translate('addresses.deleteConfirm') }))) {
      return;
    }
    try {
      await firstValueFrom(this.api.deleteAddress(address.id));
      this.notifier.success('addresses.deleted');
      this.addresses.reload();
    } catch {
      // Shown by the error toast.
    }
  }
}
