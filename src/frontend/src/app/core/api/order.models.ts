export type OrderStatus = 'Open' | 'Paid' | 'Canceled' | 'Failed' | 'Refunded';

export interface OrderResponse {
  id: string;
  connectStoneOrderId: string | null;
  customerName: string;
  description: string;
  amountInCents: number;
  status: OrderStatus;
  createdAt: string;
  paidAt: string | null;
}

export interface CreateOrderRequest {
  customerName: string;
  description: string;
  amountInCents: number;
}
