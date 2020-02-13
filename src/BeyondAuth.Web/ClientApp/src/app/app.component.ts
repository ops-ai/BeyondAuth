import { Component, OnDestroy, OnInit } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'ngx-app',
  template: '<router-outlet></router-outlet>',
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'BeyondAuth';

  isAuthenticated: boolean;
  userData: any;

  constructor(public oidcSecurityService: OidcSecurityService) {
    if (this.oidcSecurityService.moduleSetup) {
      this.doCallbackLogicIfRequired();
    } else {
      this.oidcSecurityService.onModuleSetup.subscribe(() => {
        this.doCallbackLogicIfRequired();
      });
    }
  }

  ngOnInit() {
    this.oidcSecurityService.getIsAuthorized().subscribe(auth => {
      this.isAuthenticated = auth;
    });

    this.oidcSecurityService.getUserData().subscribe(userData => {
      this.userData = userData;
    });
  }

  ngOnDestroy(): void { }

  login() {
    this.oidcSecurityService.authorize();
  }

  logout() {
    this.oidcSecurityService.logoff();
  }

  private doCallbackLogicIfRequired() {
    // Will do a callback, if the url has a code and state parameter.
    this.oidcSecurityService.authorizedCallbackWithCode(window.location.toString());
  }
}
