import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface Customer {
  id: number;
  name: string;
  email: string;
}

export interface Order {
  id: number;
  customerId: number;
  product: string;
  quantity: number;
  unitPrice: number;
  total: number;
  createdAt: string;
}

export interface OrderDetails extends Order {
  customerName: string;
  customerEmail: string;
  servedBy: string;
}

export interface Invoice {
  id: number;
  orderId: number;
  customerEmail: string;
  amount: number;
  createdAt: string;
  emailSent: boolean;
  paid: boolean;
}

export interface DocumentInfo {
  id: number;
  orderId: number;
  fileName: string;
  objectKey: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface NewOrder {
  customerId: number;
  product: string;
  quantity: number;
  unitPrice: number;
}

/**
 * All calls use relative /api/... URLs. In the lab, Traefik receives them and routes
 * each path prefix to a different backend service (see infra/stack.yml labels).
 */
@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  version() {
    return this.http.get<{ service: string; version: string }>('/api/orders/version');
  }

  customers() {
    return this.http.get<Customer[]>('/api/customers');
  }

  orders() {
    return this.http.get<Order[]>('/api/orders');
  }

  order(id: number) {
    return this.http.get<OrderDetails>(`/api/orders/${id}`);
  }

  createOrder(order: NewOrder) {
    return this.http.post<{ id: number; total: number; invoiceId: number }>('/api/orders', order);
  }

  invoices() {
    return this.http.get<Invoice[]>('/api/billing/invoices');
  }

  pay(invoiceId: number) {
    return this.http.post<{ approved: boolean; gatewayReference: string }>(`/api/billing/invoices/${invoiceId}/pay`, {});
  }

  documents() {
    return this.http.get<DocumentInfo[]>('/api/documents');
  }

  upload(orderId: number, file: File) {
    const form = new FormData();
    form.append('orderId', String(orderId));
    form.append('file', file);
    return this.http.post<DocumentInfo>('/api/documents', form);
  }
}
