import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { OrderResponse } from './order.models';
import { OrdersService } from './orders.service';

const sampleOrder: OrderResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  connectStoneOrderId: 'or_123',
  customerName: 'Jane Doe',
  description: 'Coffee',
  amountInCents: 1500,
  status: 'Open',
  createdAt: '2026-08-20T10:00:00Z',
  paidAt: null,
};

describe('OrdersService', () => {
  let service: OrdersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('load() populates orders from the API', async () => {
    const promise = service.load();
    httpMock.expectOne('/orders').flush([sampleOrder]);
    await promise;

    expect(service.orders()).toEqual([sampleOrder]);
    expect(service.loading()).toBe(false);
  });

  it('create() prepends the new order on success', async () => {
    service.orders.set([sampleOrder]);

    const promise = service.create({ customerName: 'John', description: 'Tea', amountInCents: 1000 });
    const created: OrderResponse = { ...sampleOrder, id: '22222222-2222-2222-2222-222222222222', customerName: 'John' };
    httpMock.expectOne('/orders').flush(created);
    const result = await promise;

    expect(result).toBe(true);
    expect(service.orders()[0]).toEqual(created);
    expect(service.orders().length).toBe(2);
  });

  it('applyStatusUpdate() replaces the matching order in place', () => {
    service.orders.set([sampleOrder]);

    service.applyStatusUpdate({ ...sampleOrder, status: 'Paid', paidAt: '2026-08-20T10:05:00Z' });

    expect(service.orders()[0].status).toBe('Paid');
  });

  it('applyStatusUpdate() ignores updates for unknown orders', () => {
    service.orders.set([sampleOrder]);

    service.applyStatusUpdate({ ...sampleOrder, id: 'unknown-id', status: 'Paid' });

    expect(service.orders()).toEqual([sampleOrder]);
  });
});
