import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { OrdersService } from '../../core/api/orders.service';
import { OrderStatusHubService } from '../../core/signalr/order-status-hub.service';

@Component({
  selector: 'app-orders-dashboard',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './orders-dashboard.component.html',
})
export class OrdersDashboardComponent implements OnInit {
  protected readonly ordersService = inject(OrdersService);
  protected readonly hub = inject(OrderStatusHubService);

  protected readonly customerName = signal('');
  protected readonly description = signal('');
  protected readonly amount = signal<number | null>(null);
  protected readonly submitting = signal(false);
  protected readonly simulatingOrderId = signal<string | null>(null);

  protected readonly canSubmit = computed(
    () => this.customerName().trim().length > 0 && this.description().trim().length > 0 && (this.amount() ?? 0) > 0,
  );

  async ngOnInit(): Promise<void> {
    await Promise.all([this.ordersService.load(), this.hub.start()]);
  }

  async createOrder(): Promise<void> {
    if (!this.canSubmit()) {
      return;
    }

    this.submitting.set(true);
    try {
      const amountInCents = Math.round((this.amount() ?? 0) * 100);
      const created = await this.ordersService.create({
        customerName: this.customerName().trim(),
        description: this.description().trim(),
        amountInCents,
      });

      if (created) {
        this.customerName.set('');
        this.description.set('');
        this.amount.set(null);
      }
    } finally {
      this.submitting.set(false);
    }
  }

  async simulatePayment(orderId: string): Promise<void> {
    this.simulatingOrderId.set(orderId);
    try {
      await this.ordersService.simulatePayment(orderId);
    } finally {
      this.simulatingOrderId.set(null);
    }
  }

  formatAmount(amountInCents: number): string {
    return (amountInCents / 100).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  statusClasses(status: string): string {
    switch (status) {
      case 'Open':
        return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300';
      case 'Paid':
        return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
      case 'Refunded':
        return 'bg-sky-100 text-sky-800 dark:bg-sky-900/40 dark:text-sky-300';
      default:
        return 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300';
    }
  }
}
