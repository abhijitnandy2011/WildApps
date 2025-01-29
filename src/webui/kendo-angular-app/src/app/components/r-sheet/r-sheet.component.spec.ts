import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RSheetComponent } from './r-sheet.component';

describe('RSheetComponent', () => {
  let component: RSheetComponent;
  let fixture: ComponentFixture<RSheetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RSheetComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RSheetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
