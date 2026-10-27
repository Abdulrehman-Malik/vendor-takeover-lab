import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, WritableSignal, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { Api, Customer, DocumentInfo, Invoice, NewOrder, Order, OrderDetails } from './api';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly api = inject(Api);

  protected readonly version = signal('…');
  protected readonly customers = signal<Customer[]>([]);
  protected readonly orders = signal<Order[]>([]);
  protected readonly invoices = signal<Invoice[]>([]);
  protected readonly documents = signal<DocumentInfo[]>([]);
  protected readonly selected = signal<OrderDetails | null>(null);
  protected readonly message = signal('');
  protected readonly error = signal('');

  protected newOrder: NewOrder = { customerId: 1, product: 'Widget', quantity: 1, unitPrice: 25 };
  protected uploadOrderId = 0;
  private file: File | null = null;

  ngOnInit() {
    this.refresh();
  }

  protected refresh() {
    this.error.set('');
    this.api.version().subscribe({
      next: (v) => this.version.set(v.version),
      error: () => this.version.set('unreachable'),
    });
    this.load(this.api.customers(), this.customers);
    this.load(this.api.orders(), this.orders);
    this.load(this.api.invoices(), this.invoices);
    this.load(this.api.documents(), this.documents);
  }

  protected createOrder() {
    this.api.createOrder(this.newOrder).subscribe({
      next: (r) => {
        this.message.set(`Order ${r.id} created (total ${r.total}), invoice ${r.invoiceId} raised.`);
        this.refresh();
      },
      error: (e) => this.fail(e),
    });
  }

  protected showDetails(orderId: number) {
    this.api.order(orderId).subscribe({ next: (d) => this.selected.set(d), error: (e) => this.fail(e) });
  }

  protected pay(invoiceId: number) {
    this.api.pay(invoiceId).subscribe({
      next: (r) => {
        this.message.set(`Payment approved, reference ${r.gatewayReference}.`);
        this.refresh();
      },
      error: (e) => this.fail(e),
    });
  }

  protected onFileChosen(event: Event) {
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  protected upload() {
    if (!this.file || !this.uploadOrderId) {
      this.error.set('Choose an order and a file first.');
      return;
    }
    this.api.upload(this.uploadOrderId, this.file).subscribe({
      next: (d) => {
        this.message.set(`Uploaded ${d.fileName} to MinIO as ${d.objectKey}.`);
        this.refresh();
      },
      error: (e) => this.fail(e),
    });
  }

  private load<T>(source: Observable<T>, target: WritableSignal<T>) {
    source.subscribe({ next: (value) => target.set(value), error: (e) => this.fail(e) });
  }

  private fail(e: HttpErrorResponse) {
    const detail = typeof e.error === 'string' ? e.error : (e.error?.detail ?? e.message);
    this.error.set(`${e.status} ${e.url ?? ''} - ${detail}`);
  }
}
