import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NotePadComponent } from './note-pad.component';

describe('NotePadComponent', () => {
  let component: NotePadComponent;
  let fixture: ComponentFixture<NotePadComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotePadComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NotePadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
