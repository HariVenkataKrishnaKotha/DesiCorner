import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="star-rating" [class.readonly]="readonly" [class.small]="size === 'small'">
      @for (star of stars; track star) {
        <mat-icon 
          [class.filled]="star <= displayRating"
          [class.half]="star === Math.ceil(displayRating) && displayRating % 1 !== 0"
          (click)="!readonly && onStarClick(star)"
          (mouseenter)="!readonly && onStarHover(star)"
          (mouseleave)="!readonly && onStarLeave()">
          {{ getStarIcon(star) }}
        </mat-icon>
      }
      @if (showValue) {
        <span class="rating-value">{{ rating.toFixed(1) }}</span>
      }
      @if (reviewCount !== undefined) {
        <span class="review-count">({{ reviewCount }} {{ reviewCount === 1 ? 'review' : 'reviews' }})</span>
      }
    </div>
  `,
  styles: [`
    .star-rating {
      display: inline-flex;
      align-items: center;
      gap: 2px;
    }

    mat-icon {
      color: #ddd;
      font-size: 24px;
      width: 24px;
      height: 24px;
      cursor: pointer;
      transition: color 0.2s ease;
    }

    .star-rating.small mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }

    .star-rating.readonly mat-icon {
      cursor: default;
    }

    mat-icon.filled {
      color: #ffc107;
    }

    mat-icon:hover:not(.readonly) {
      transform: scale(1.1);
    }

    .rating-value {
      margin-left: 8px;
      font-weight: 600;
      color: #333;
    }

    .review-count {
      margin-left: 4px;
      color: #666;
      font-size: 0.9em;
    }

    .star-rating.small .rating-value,
    .star-rating.small .review-count {
      font-size: 0.85em;
    }
  `]
})
export class StarRatingComponent {
  @Input() rating: number = 0;
  @Input() readonly: boolean = true;
  @Input() showValue: boolean = false;
  @Input() reviewCount?: number;
  @Input() size: 'normal' | 'small' = 'normal';
  @Output() ratingChange = new EventEmitter<number>();

  stars = [1, 2, 3, 4, 5];
  hoverRating = 0;
  Math = Math;

  get displayRating(): number {
    return this.hoverRating || this.rating;
  }

  getStarIcon(star: number): string {
    if (star <= this.displayRating) {
      return 'star';
    } else if (star === Math.ceil(this.displayRating) && this.displayRating % 1 >= 0.5) {
      return 'star_half';
    }
    return 'star_border';
  }

  onStarClick(star: number): void {
    this.rating = star;
    this.ratingChange.emit(star);
  }

  onStarHover(star: number): void {
    this.hoverRating = star;
  }

  onStarLeave(): void {
    this.hoverRating = 0;
  }
}