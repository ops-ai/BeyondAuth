import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { APP_INITIALIZER, NgModule, ErrorHandler } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { CoreModule } from './@core/core.module';
import { ThemeModule } from './@theme/theme.module';
import { AppRoutingModule } from './app-routing.module';
import { JL } from 'jsnlog';
import { NbPasswordAuthStrategy, NbAuthModule, NbOAuth2AuthStrategy, NbOAuth2ClientAuthMethod } from '@nebular/auth';
import { AuthModule, ConfigResult, OidcConfigService, OidcSecurityService, OpenIdConfiguration } from 'angular-auth-oidc-client';

import { AppComponent } from './app.component';
import {
  NbDatepickerModule,
  NbDialogModule,
  NbMenuModule,
  NbSidebarModule,
  NbToastrModule,
  NbWindowModule,
} from '@nebular/theme';

const oidc_configuration = 'assets/auth.clientConfiguration.json';

export function loadConfig(oidcConfigService: OidcConfigService) {
  return () => oidcConfigService.load(oidc_configuration);
}

export class UncaughtExceptionHandler implements ErrorHandler {
  handleError(error: any) {
    JL().fatalException('Uncaught Exception', error);
  }
}

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    AppRoutingModule,

    ThemeModule.forRoot(),

    NbSidebarModule.forRoot(),
    NbMenuModule.forRoot(),
    NbDatepickerModule.forRoot(),
    NbDialogModule.forRoot(),
    NbWindowModule.forRoot(),
    NbToastrModule.forRoot(),
    CoreModule.forRoot(),

    AuthModule.forRoot(),
  ],
  bootstrap: [AppComponent],
  providers: [
    { provide: ErrorHandler, useClass: UncaughtExceptionHandler },
    { provide: 'JSNLOG', useValue: JL },
    OidcConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: loadConfig,
      deps: [OidcConfigService],
      multi: true,
    },
  ]
})
export class AppModule {
    constructor(private oidcSecurityService: OidcSecurityService, private oidcConfigService: OidcConfigService) {
      this.oidcConfigService.onConfigurationLoaded.subscribe((configResult: ConfigResult) => {

        // Use the configResult to set the configurations

        const config: OpenIdConfiguration = {
          stsServer: configResult.customConfig.stsServer,
          redirect_url: 'https://localhost:5005',
          client_id: 'angularClient',
          scope: 'openid profile email',
          response_type: 'code',
          silent_renew: true,
          silent_renew_url: 'https://localhost:5005/silent-renew.html',
          log_console_debug_active: true,
          // all other properties you want to set
        };

        this.oidcSecurityService.setupModule(config, configResult.authWellknownEndpoints);
      });
  }
}
