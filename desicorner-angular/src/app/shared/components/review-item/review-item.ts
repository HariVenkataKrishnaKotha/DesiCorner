import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { Review } from '@core/models/review.models';
import { StarRatingComponent } from '../star-rating/star-rating';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-review-item',
  standalone: true,
  imports: [
    CommonModule, 
    MatIconModule, 
    MatButtonModule, 
    MatMenuModule,
    StarRatingComponent
  ],
  template: `
    <div class="review-item">
      <div class="review-header">
        <div class="reviewer-info">
          <div class="avatar">{{ getInitials(review.userName) }}</div>
          <div class="reviewer-details">
            <span class="reviewer-name">{{ review.userName }}</span>
            @if (review.isVerifiedPurchase) {
              <span class="verified-badge">
                <mat-icon>verified</mat-icon>
                Verified Purchase
              </span>
            }
          </div>
        </div>

        @if (isOwnReview) {
          <button mat-icon-button [matMenuTriggerFor]="menu">
            <mat-icon>more_vert</mat-icon>
          </button>
          <mat-menu #menu="matMenu">
            <button mat-menu-item (click)="onEdit()">
              <mat-icon>edit</mat-icon>
              <span>Edit</span>
            </button>
            <button mat-menu-item (click)="onDelete()">
              <mat-icon>delete</mat-icon>
              <span>Delete</span>
            </button>
          </mat-menu>
        }
      </div>

      <div class="review-rating">
        <app-star-rating [rating]="review.rating" [readonly]="true" size="small"></app-star-rating>
        @if (review.title) {
          <span class="review-title">{{ review.title }}</span>
        }
      </div>

      <div class="review-date">
        Reviewed on {{ review.createdAt | date:'mediumDate' }}
        @if (review.updatedAt) {
          <span class="edited">(edited)</span>
        }
      </div>

      @if (review.comment) {
        <p class="review-comment">{{ review.comment }}</p>
      }

      <div class="review-actions">
        <span class="helpful-text">Was this review helpful?</span>
        <button 
          mat-button 
          (click)="onVote(true)"
          [disabled]="!canVote">
          <mat-icon>thumb_up</mat-icon>
          Yes ({{ review.helpfulCount }})
        </button>
        <button 
          mat-button 
          (click)="onVote(false)"
          [disabled]="!canVote">
          <mat-icon>thumb_down</mat-icon>
          No ({{ review.notHelpfulCount }})
        </button>
      </div>
    </div>
  `,
  styles: [`
    .review-item {
      padding: 16px;
      border-bottom: 1px solid #eee;
    }

    .review-item:last-child {
      border-bottom: none;
    }

    .review-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 8px;
    }

    .reviewer-info {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #3f51b5;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 14px;
    }

    .reviewer-details {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .reviewer-name {
      font-weight: 600;
      color: #333;
    }

    .verified-badge {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 0.75rem;
      color: #4caf50;
    }

    .verified-badge mat-icon {
      font-size: 14px;
      width: 14px;
      height: 14px;
    }

    .review-rating {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 4px;
    }

    .review-title {
      font-weight: 600;
      color: #333;
    }

    .review-date {
      font-size: 0.85rem;
      color: #888;
      margin-bottom: 12px;
    }

    .edited {
      font-style: italic;
    }

    .review-comment {
      color: #333;
      line-height: 1.6;
      margin-bottom: 12px;
    }

    .review-actions {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .helpful-text {
      font-size: 0.85rem;
      color: #666;
    }

    .review-actions button {
      font-size: 0.85rem;
    }

    .review-actions mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      margin-right: 4px;
    }
  `]
})
export class ReviewItemComponent {
  @Input() review!: Review;
  @Output() edit = new EventEmitter<Review>();
  @Output() delete = new EventEmitter<Review>();
  @Output() vote = new EventEmitter<{ reviewId: string; isHelpful: boolean }>();

  private authService = inject(AuthService);

  get isOwnReview(): boolean {
    const user = this.authService.currentUser;
    return user?.id === this.review.userId;
  }

  get canVote(): boolean {
    const user = this.authService.currentUser;
    return !!user && user.id !== this.review.userId;
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  onEdit(): void {
    this.edit.emit(this.review);
  }

  onDelete(): void {
    this.delete.emit(this.review);
  }

  onVote(isHelpful: boolean): void {
    this.vote.emit({ reviewId: this.review.id, isHelpful });
  }
}