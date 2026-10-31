// Points ledger (F2). TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.
import { PagedResult } from './common.models';
import { Order } from './shop.models';
import { Submission } from './submissions.models';

// Points (F2)
export type PointsEntryType = 'Earn' | 'Redeem' | 'Reverse' | 'Adjust';
export type PointsSourceType = 'Submission' | 'Order' | 'Manual';

export interface PointsEntry {
  id: string;
  amount: number;
  type: PointsEntryType;
  sourceType: PointsSourceType;
  sourceId: string | null;
  reason: string | null;
  createdAt: string;
}

export interface PointsHistoryResponse extends PagedResult<PointsEntry> {
  balance: number;
}
