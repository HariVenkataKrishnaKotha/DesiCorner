import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '@core/services/auth.service';
import { CartService } from '@core/services/cart.service';
import { Callback } from './callback';

describe('Callback (PKCE sign-in landing page)', () => {
  let auth: jasmine.SpyObj<AuthService>;
  let cart: jasmine.SpyObj<CartService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['handlePkceCallback']);
    cart = jasmine.createSpyObj<CartService>('CartService', ['loadCart']);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    sessionStorage.removeItem('returnUrl');

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: CartService, useValue: cart },
        { provide: Router, useValue: router },
        { provide: ToastrService, useValue: jasmine.createSpyObj('ToastrService', ['success', 'error']) },
      ],
    });
  });

  afterEach(() => sessionStorage.removeItem('returnUrl'));

  async function land(): Promise<void> {
    const component = TestBed.runInInjectionContext(() => new Callback());
    await component.ngOnInit();
  }

  it('after a successful sign-in, goes to the return URL without loading the cart itself', async () => {
    // The cart is loaded by CartEffects once the guest cart has been merged; loading it here
    // would race the merge and show the pre-merge cart.
    auth.handlePkceCallback.and.resolveTo(true);
    sessionStorage.setItem('returnUrl', '/checkout');

    await land();

    expect(cart.loadCart).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/checkout']);
  });
});
