// Addresses, cart and orders (F6, F7). TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.

// Addresses
export interface Address {
  id: string;
  fullName: string;
  phone: string;
  city: string;
  area: string;
  street: string;
  building: string;
  notes: string | null;
  isDefault: boolean;
}

export type SaveAddressRequest = Omit<Address, 'id'>;

// Cart (F6)
export interface CartItem {
  variantId: string;
  productId: string;
  productSlug: string;
  productName: string;
  variantName: string;
  imageUrl: string | null;
  unitPriceEgp: number;
  unitPricePoints: number | null;
  unitRewardPoints: number;
  quantity: number;
  lineTotalEgp: number;
  isAvailable: boolean;
  hasEnoughStock: boolean;
}

export interface Cart {
  items: CartItem[];
  itemCount: number;
  totalEgp: number;
  totalPoints: number | null;
  totalRewardPoints: number;
  canPayWithPoints: boolean;
  hasIssues: boolean;
}

// Orders (F6, F7)
export type PaymentMethod = 'Points' | 'Cod';
export type OrderStatus = 'Placed' | 'Confirmed' | 'Shipped' | 'Delivered' | 'Cancelled';

export interface OrderItem {
  id: string;
  variantId: string;
  productName: string;
  variantName: string;
  imageUrl: string | null;
  unitPriceEgp: number;
  unitPricePoints: number | null;
  unitRewardPoints: number;
  quantity: number;
  lineTotalEgp: number;
}

export interface OrderStatusChange {
  fromStatus: OrderStatus | null;
  toStatus: OrderStatus;
  changedBy: string | null;
  changedAt: string;
}

export interface ShippingAddress {
  fullName: string;
  phone: string | null;
  city: string;
  area: string;
  street: string;
  building: string;
  notes: string | null;
}

export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  totalEgp: number;
  totalPoints: number;
  totalRewardPoints: number;
  rewardPending: boolean;
  canCancel: boolean;
  nextStatuses: OrderStatus[];
  shippingAddress: ShippingAddress;
  createdAt: string;
  items: OrderItem[];
  statusHistory: OrderStatusChange[];
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  totalEgp: number;
  totalPoints: number;
  totalRewardPoints: number;
  itemCount: number;
  recipientName: string;
  city: string;
  createdAt: string;
}
