import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

import { OrdersService } from '../api/orders.service';
import { OrderResponse } from '../api/order.models';

export type HubConnectionStatus = 'disconnected' | 'connecting' | 'connected';

@Injectable({ providedIn: 'root' })
export class OrderStatusHubService {
  private readonly ordersService = inject(OrdersService);
  private connection?: HubConnection;

  readonly status = signal<HubConnectionStatus>('disconnected');

  async start(): Promise<void> {
    if (this.connection) {
      return;
    }

    this.status.set('connecting');

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/order-status')
      .withAutomaticReconnect()
      .build();

    connection.on('OrderStatusChanged', (order: OrderResponse) => {
      this.ordersService.applyStatusUpdate(order);
    });

    connection.onreconnecting(() => this.status.set('connecting'));
    connection.onreconnected(() => this.status.set('connected'));
    connection.onclose(() => this.status.set('disconnected'));

    this.connection = connection;

    try {
      await connection.start();
      this.status.set(connection.state === HubConnectionState.Connected ? 'connected' : 'disconnected');
    } catch {
      this.status.set('disconnected');
    }
  }
}
