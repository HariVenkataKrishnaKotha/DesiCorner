import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ProductService } from '@core/services/product.service';
import { CartService } from '@core/services/cart.service';
import { Product, Category } from '@core/models/product.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule
  ],
  templateUrl: './home.html',
  styleUrls: ['./home.scss']
})
export class HomeComponent implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private toastr = inject(ToastrService);

  categories: Category[] = [];
  featuredProducts: Product[] = [];
  loading = true;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
  this.loading = true;

  // Load categories
  this.productService.loadCategories().subscribe({
    next: (response) => {
      if (response.isSuccess && response.result) {
        this.categories = response.result;
      }
    },
    error: (error) => {
      console.error('Failed to load categories', error);
      // Don't show toast - error interceptor handles it
    }
  });

  // Load products
  this.productService.loadProducts().subscribe({
    next: (response) => {
      if (response.isSuccess && response.result) {
        // Get first 6 products as featured
        this.featuredProducts = response.result.slice(0, 6);
      }
      this.loading = false;
    },
    error: (error) => {
      console.error('Failed to load products', error);
      this.loading = false;
      // Don't show toast - error interceptor handles it
    }
  });
}

  addToCart(product: Product): void {
    this.cartService.addItem(product, 1);
    this.toastr.success(`${product.name} added to cart!`, 'Success');
  }
}