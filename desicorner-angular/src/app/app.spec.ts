import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { BehaviorSubject } from 'rxjs';
import { AppComponent } from './app';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';

/**
 * Smoke test for the application shell: the real AppComponent, HeaderComponent and
 * FooterComponent render together, with AuthService and CartService stubbed.
 */
describe('AppComponent (shell smoke test)', () => {
  const authState$ = new BehaviorSubject<{ isAuthenticated: boolean; loading: boolean; profile?: { email: string } }>({
    isAuthenticated: false,
    loading: false,
  });
  let isAdmin = false;
  let itemCount = 0;

  beforeEach(async () => {
    authState$.next({ isAuthenticated: false, loading: false });
    isAdmin = false;
    itemCount = 0;

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AuthService, useValue: { authState$, isAdmin: () => isAdmin, logout: jasmine.createSpy('logout') } },
        { provide: CartService, useValue: { get itemCount() { return itemCount; } } },
      ],
    }).compileComponents();
  });

  function render(): HTMLElement {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function navLinks(root: HTMLElement): string[] {
    return Array.from(root.querySelectorAll('.nav-links a')).map(a => a.textContent!.trim());
  }

  it('renders the header, a router outlet for pages, and the footer', () => {
    const root = render();

    expect(root.querySelector('app-header .logo-text')?.textContent).toContain('DesiCorner');
    expect(root.querySelector('main.main-content router-outlet')).not.toBeNull();
    expect(root.querySelector('app-footer')?.children.length).toBeGreaterThan(0);
  });

  it('shows anonymous visitors the public links and a login button', () => {
    const root = render();

    expect(navLinks(root)).toEqual(['Home', 'Menu']);
    expect(root.querySelector('[aria-label="Login to your account"]')).not.toBeNull();
  });

  it('shows signed-in admins their orders, the admin link and their email', () => {
    authState$.next({ isAuthenticated: true, loading: false, profile: { email: 'admin@example.test' } });
    isAdmin = true;

    const root = render();

    expect(navLinks(root)).toEqual(['Home', 'Menu', 'My Orders', 'Admin']);
    expect(root.querySelector('.user-name')?.textContent).toContain('admin@example.test');
    expect(root.querySelector('[aria-label="Login to your account"]')).toBeNull();
  });

  it('labels the cart button with the item count', () => {
    itemCount = 3;

    const root = render();

    expect(root.querySelector('[aria-label="Shopping cart with 3 items"]')).not.toBeNull();
  });
});
