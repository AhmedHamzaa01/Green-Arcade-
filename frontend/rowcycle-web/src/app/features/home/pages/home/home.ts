import { Component, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { BadgesApi } from '../../../../core/api/badges-api';
import { CatalogApi } from '../../../../core/api/catalog-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { StatCard } from '../../../../shared/ui/stat-card/stat-card';
import { ProductCard } from '../../../store/components/product-card/product-card';

/** The landing page: what RowCycle is, how it works, what's new in the store, and (for members) where they stand. */
@Component({
  selector: 'app-home',
  imports: [DecimalPipe, RouterLink, MatButton, MatIcon, TranslocoPipe, StatCard, ProductCard],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly session = inject(AuthSession);
  private readonly catalog = inject(CatalogApi);
  private readonly badgesApi = inject(BadgesApi);

  protected readonly steps = [
    { key: 'act', icon: 'volunteer_activism' },
    { key: 'photo', icon: 'add_a_photo' },
    { key: 'earn', icon: 'verified' },
    { key: 'spend', icon: 'shopping_bag' },
  ];

  protected readonly products = rxResource({
    stream: () => this.catalog.products({ sort: 'Newest', page: 1, pageSize: 4 }),
  });

  /** Only for members; visitors don't call the API. */
  protected readonly badges = rxResource({
    params: () => this.session.isLoggedIn(),
    stream: ({ params: loggedIn }) => (loggedIn ? this.badgesApi.mine() : of([])),
  });
}
