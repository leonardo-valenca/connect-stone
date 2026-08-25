import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { CreateOrderRequest, OrderResponse } from './order.models';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);

  readonly orders = signal<OrderResponse[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      const orders = await firstValueFrom(this.http.get<OrderResponse[]>('/orders'));
      this.orders.set(orders);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  async create(request: CreateOrderRequest): Promise<boolean> {
    this.errorMessage.set(null);
    try {
      const order = await firstValueFrom(this.http.post<OrderResponse>('/orders', request));
      this.orders.update((list) => [order, ...list]);
      return true;
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
      return false;
    }
  }

  async simulatePayment(orderId: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await firstValueFrom(this.http.post(`/demo/simulate-payment/${orderId}`, null));
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    }
  }

  /** Called by OrderStatusHubService when a SignalR push arrives. */
  applyStatusUpdate(update: OrderResponse): void {
    this.orders.update((list) => {
      const index = list.findIndex((o) => o.id === update.id);
      if (index === -1) {
        return list;
      }
      const next = [...list];
      next[index] = update;
      return next;
    });
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const problem = error.error as { title?: string } | null;
      return problem?.title ?? `Request failed (${error.status}).`;
    }
    return 'Something went wrong.';
  }
}
