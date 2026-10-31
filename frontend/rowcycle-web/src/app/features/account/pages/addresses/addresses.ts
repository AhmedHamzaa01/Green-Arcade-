import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Address } from '../../../../core/api/models';
import { ShopApi } from '../../../../core/api/shop-api';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { AddressDialog } from '../../components/address-dialog/address-dialog';

/** /account/addresses */
@Component({
  selector: 'app-addresses',
  imports: [MatButton, MatIconButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, PageHeader, Loading, EmptyState],
  templateUrl: './addresses.html',
  styleUrl: './addresses.scss',
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
