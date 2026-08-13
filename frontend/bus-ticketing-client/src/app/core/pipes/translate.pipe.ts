import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '../services/translate.service';

@Pipe({ name: 'translate', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly translateService = inject(TranslateService);

  transform(key: string, interpolateParams?: Record<string, any>): string {
    if (!key) return '';
    return this.translateService.getSync(key, interpolateParams);
  }
}
