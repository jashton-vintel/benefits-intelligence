import { inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

export const APP_NAME = 'Benefits Intelligence';

/** Every page title ends with the app name, so open tabs stay recognisable. */
@Injectable({ providedIn: 'root' })
export class AppTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.setPageTitle(this.buildTitle(snapshot));
  }

  setPageTitle(page: string | undefined): void {
    this.title.setTitle(page ? `${page} · ${APP_NAME}` : APP_NAME);
  }
}
